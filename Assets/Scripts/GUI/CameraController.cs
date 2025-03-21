using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
    // Start is called before the first frame update
    void Start() {

    }

    public float panSpeed = 100f;
    public float zoomSpeed = 500f;
    public float minZoom = 10f;
    public float maxZoom = 100f;

    void Update() {
        // Pan with Arrow Keys or WASD
        float moveX = Input.GetAxis("Horizontal") * panSpeed * Time.deltaTime;
        float moveZ = Input.GetAxis("Vertical") * panSpeed * Time.deltaTime;
        transform.position += new Vector3(moveX, 0, moveZ);

        // Zoom with Mouse Scroll
        float scroll = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed * Time.deltaTime;
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - scroll, minZoom, maxZoom);
    }
}