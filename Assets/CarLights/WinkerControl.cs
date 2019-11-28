using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

public class WinkerControl : MonoBehaviour
{
    public CarController car; // reference to the car controller, must be dragged in inspector

    [SerializeField] bool IsLeft;
    private Renderer m_Renderer;

    private void Start()
    {
        m_Renderer = GetComponent<Renderer>();
    }

    private void Update()
    {
        // enable the Renderer when the car is braking, disable it otherwise.
        m_Renderer.enabled = (IsLeft ? car.LeftWinkerOn : car.RightWinkerOn) && Time.time % 0.8f < 0.4f;
    }
}
