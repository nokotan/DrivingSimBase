using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof (CarController))]
    public class CarUserControl : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use
        [SerializeField] private float m_CreapFactor = 0.01f;
        [SerializeField] private float m_CreapSpeedMax = 8.0f;

        private void Awake()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
        }


        private void FixedUpdate()
        {
            // pass the input to the car!
            float h = CrossPlatformInputManager.GetAxis("Horizontal");
            float v = CrossPlatformInputManager.GetAxis("Vertical");

            float creapAmount = Mathf.Max(m_CreapSpeedMax - m_Car.CurrentSpeed, 0.0f) * 0.2f * m_CreapFactor;

            if (!m_Car.LeftWinkerOn)
                m_Car.LeftWinkerOn |= CrossPlatformInputManager.GetButtonDown("Fire1");
            else
                m_Car.LeftWinkerOn &= !CrossPlatformInputManager.GetButtonDown("Fire1");

            if (!m_Car.RightWinkerOn)
                m_Car.RightWinkerOn |= CrossPlatformInputManager.GetButtonDown("Fire2");
            else
                m_Car.RightWinkerOn &= !CrossPlatformInputManager.GetButtonDown("Fire2");

#if !MOBILE_INPUT
            float handbrake = CrossPlatformInputManager.GetAxis("Jump");
            m_Car.Move(h, v + creapAmount, v, handbrake);
#else
            m_Car.Move(h, v + creapAmount, v, 0f);
#endif
        }
    }
}
