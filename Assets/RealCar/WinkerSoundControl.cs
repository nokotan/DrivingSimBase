using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

public class WinkerSoundControl : MonoBehaviour
{
    [SerializeField]
    private AudioSource winkerSound;

    private CarController car;

    // Start is called before the first frame update
    void Start()
    {
        car = GetComponent<CarController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (car.LeftWinkerOn || car.RightWinkerOn)
        {
            if (!winkerSound.isPlaying)
                winkerSound.Play();
        }
        else
        {
            winkerSound.Stop();
        }
    }
}
