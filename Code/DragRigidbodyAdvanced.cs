using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DragRigidbodyAdvanced : MonoBehaviour
{
    public float force = 600;
    public float damping = 6;
    public float distance = 15;

    public LineRenderer lr;
    public Transform lineRenderLocation;

    Transform jointTrans;
    float dragDepth;
    Rigidbody targetRb;

    void OnMouseDown()
    {
        HandleInputBegin(Input.mousePosition);
    }

    void OnMouseUp()
    {
        HandleInputEnd(Input.mousePosition);
    }

    void OnMouseDrag()
    {
        HandleInput(Input.mousePosition);
    }

    public void HandleInputBegin(Vector3 screenPosition)
    {
        var ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, distance))
        {
            if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Interactive"))
            {
                dragDepth = CameraPlane.CameraToPointDepth(Camera.main, hit.point);
                targetRb = hit.rigidbody;
                if (targetRb != null)
                {
                    // If the collider is MeshCollider and not convex, warn the user
                    var meshCol = targetRb.GetComponent<MeshCollider>();
                    if (meshCol != null && !meshCol.convex)
                    {
                        Debug.LogWarning($"[DragRigidbodyAdvanced] MeshCollider on {targetRb.name} is not convex. Dragging may not work. Set it to convex or use primitive colliders.");
                    }
                    jointTrans = AttachJoint(targetRb, hit.point);
                }
            }
        }
        if (lr != null)
            lr.positionCount = 2;
    }

    public void HandleInput(Vector3 screenPosition)
    {
        if (jointTrans == null)
            return;
        jointTrans.position = CameraPlane.ScreenToWorldPlanePoint(Camera.main, dragDepth, screenPosition);
        DrawRope();
    }

    public void HandleInputEnd(Vector3 screenPosition)
    {
        DestroyRope();
        if (jointTrans != null)
        {
            Destroy(jointTrans.gameObject);
            jointTrans = null;
        }
        targetRb = null;
    }

    Transform AttachJoint(Rigidbody rb, Vector3 attachmentPosition)
    {
        GameObject go = new GameObject("Attachment Point");
        go.hideFlags = HideFlags.HideInHierarchy;
        go.transform.position = attachmentPosition;

        var newRb = go.AddComponent<Rigidbody>();
        newRb.isKinematic = true;

        var joint = go.AddComponent<ConfigurableJoint>();
        joint.connectedBody = rb;
        joint.configuredInWorldSpace = true;
        joint.xDrive = NewJointDrive(force, damping);
        joint.yDrive = NewJointDrive(force, damping);
        joint.zDrive = NewJointDrive(force, damping);
        joint.slerpDrive = NewJointDrive(force, damping);
        joint.rotationDriveMode = RotationDriveMode.Slerp;

        // Advanced: If the target is a MeshCollider and not convex, set Rigidbody to kinematic for drag duration
        var meshCol = rb.GetComponent<MeshCollider>();
        if (meshCol != null && !meshCol.convex)
        {
            Debug.LogWarning($"[DragRigidbodyAdvanced] Temporarily setting Rigidbody on {rb.name} to kinematic for drag (concave MeshCollider detected).");
            rb.isKinematic = true;
        }

        return go.transform;
    }

    private JointDrive NewJointDrive(float force, float damping)
    {
        JointDrive drive = new JointDrive();
        drive.mode = JointDriveMode.Position;
        drive.positionSpring = force;
        drive.positionDamper = damping;
        drive.maximumForce = Mathf.Infinity;
        return drive;
    }

    private void DrawRope()
    {
        if (jointTrans == null || lr == null || lineRenderLocation == null)
        {
            return;
        }
        lr.SetPosition(0, lineRenderLocation.position);
        lr.SetPosition(1, this.transform.position);
    }

    private void DestroyRope()
    {
        if (lr != null)
            lr.positionCount = 0;
    }
}

// This advanced version will warn you if your MeshCollider is not convex and will temporarily set the Rigidbody to kinematic for dragging if needed.
// For best results, use primitive or convex colliders for interactive objects.
