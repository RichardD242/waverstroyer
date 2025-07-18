using UnityEngine;

public class MoveCamera : MonoBehaviour {

    public Transform player;
    public Vector3 offset = new Vector3(0, 1.6f, 0); // Head level offset

    void LateUpdate() {
        if (player != null) {
            transform.position = player.position + offset;
        }
    }
}