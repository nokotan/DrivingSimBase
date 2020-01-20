using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraDebug : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var camera = GetComponent<Camera>();
        Debug.Log($"FOV:{camera.fieldOfView}");
        Debug.Log($"Width:{camera.pixelWidth}, Height:{camera.pixelHeight}");

        var display = Display.displays[camera.targetDisplay];
        Debug.Log($"Rendering Width:{display.renderingWidth}, Rendering Height:{display.renderingHeight}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
