using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;
using UnityStandardAssets.Utility;

public class CarList : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] int ConsistencyCheckIntervalFrames = 15;

    [Header("Debug")]
    [SerializeField] List<WaypointProgressTracker> m_AllCars;

    public List<WaypointProgressTracker> AllCars => m_AllCars;

    // Singleton Support
    public static CarList Instance { get; private set; }

    void Start()
    {
        Instance = this;

        m_AllCars.AddRange(FindObjectsOfType<WaypointProgressTracker>());
        BuildAllCarsConsistency();
    }

    // 並べなおします
    void BuildAllCarsConsistency()
    {
        m_AllCars.Sort((a, b) => (int)Mathf.Sign(a.progressDistance - b.progressDistance));
    }

    void Update()
    {
        if (Time.frameCount % ConsistencyCheckIntervalFrames == 0)
        {
            BuildAllCarsConsistency();
        }
    }

    public enum FindCarOption
    {
        InSameLane,
        InDifferentLane
    }
    
    public WaypointProgressTracker FindAheadCar(WaypointProgressTracker thisCar, FindCarOption option = FindCarOption.InSameLane)
    {
        var listIndex = m_AllCars.IndexOf(thisCar);

        for (int i = listIndex + 1; i < m_AllCars.Count; i++)
        {
            if (m_AllCars[i].Circuit == thisCar.Circuit ^ option != FindCarOption.InSameLane)
            {
                return m_AllCars[i];
            }
        }

        return null;
    }

    public WaypointProgressTracker FindBehindCar(WaypointProgressTracker thisCar, FindCarOption option = FindCarOption.InSameLane)
    {
        var listIndex = m_AllCars.IndexOf(thisCar);

        for (int i = listIndex - 1; i >= 0; i--)
        {
            if (m_AllCars[i].Circuit == thisCar.Circuit ^ option != FindCarOption.InSameLane)
            {
                return m_AllCars[i];
            }
        }

        return null;
    }

    public float GetBehindGap(WaypointProgressTracker thisCar, FindCarOption option = FindCarOption.InSameLane)
    {
        var behindCar = FindBehindCar(thisCar, option);
        return behindCar != null ? Vector3.Magnitude(thisCar.transform.position - behindCar.transform.position) : float.PositiveInfinity;
    }

    public float GetAheadGap(WaypointProgressTracker thisCar, FindCarOption option = FindCarOption.InSameLane)
    {
        var aheadCar = FindAheadCar(thisCar, option);
        return aheadCar != null ? Vector3.Magnitude(thisCar.transform.position - aheadCar.transform.position) : float.PositiveInfinity;
    }
}
