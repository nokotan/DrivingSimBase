using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityStandardAssets.Utility;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof (CarController))]
    public class CarAIControl : MonoBehaviour
    {
        public enum BrakeCondition
        {
            NeverBrake,                 // the car simply accelerates at full throttle all the time.
            TargetDirectionDifference,  // the car will brake according to the upcoming change in direction of the target. Useful for route-based AI, slowing for corners.
            TargetDistance,             // the car will brake as it approaches its target, regardless of the target's direction. Useful if you want the car to
                                        // head for a stationary target and come to rest when it arrives there.
        }

        // This script provides input to the car controller in the same way that the user control script does.
        // As such, it is really 'driving' the car, with no special physics or animation tricks to make the car behave properly.

        // "wandering" is used to give the cars a more human, less robotic feel. They can waver slightly
        // in speed and direction while driving towards their target.

        [Header("Cautionsness")]
        [SerializeField] [Range(0, 1)] private float m_CautiousSpeedFactor = 0.05f;               // percentage of max speed to use when being maximally cautious
        [SerializeField] [Range(0, 180)] private float m_CautiousMaxAngle = 50f;                  // angle of approaching corner to treat as warranting maximum caution
        [SerializeField] private float m_CautiousMaxDistance = 100f;                              // distance at which distance-based cautiousness begins
        [SerializeField] private float m_CautiousAngularVelocityFactor = 30f;                     // how cautious the AI should be when considering its own current angular velocity (i.e. easing off acceleration if spinning!)
        [Header("Steering")]
        [SerializeField] private float m_SteerSensitivity = 0.05f;                                // how sensitively the AI uses steering input to turn to the desired direction
        [SerializeField] private float m_SteerAngleThreshold = 1.0f;                              // ステアリング操作の閾値
        [Header("Brake/AccelSensitivity")]
        [SerializeField] private float m_AccelSensitivity = 0.04f;                                // How sensitively the AI uses the accelerator to reach the current desired speed
        [SerializeField] private float m_BrakeSensitivity = 1f;                                   // How sensitively the AI uses the brake to reach the current desired speed
        [Header("DesiredSpeedBasedAccel")]
        [SerializeField] private float m_DesiredSpeed = 60.0f;                                    // 希望速度
        [SerializeField] private float m_DesiredSpeedSensitivity = 0.01f;
        [Header("GapBasedAccel")]
        [SerializeField] private float m_RequiredHeadGap = 8.0f;
        [SerializeField] private float m_HeadGapSensitivity = 1.0f;
        [SerializeField] private float m_YieldingRequiredHeadGap = 12.0f;
        [SerializeField] private float m_YieldingSensitivity = 0.6f;

        [Header("MergingAccel")] 
        [SerializeField] private float m_MergingWaitingDesiredSpeedFactor = 0.6f;
        [SerializeField] private float m_MergingDesiredSpeedFactor = 0.6f;
        [SerializeField] private float m_MergingRequiredHeadGapFactor = 0.6f;

        [Header("MergingRequirements")] [SerializeField]
        private float m_MergingRequiredAheadGap = 6.0f;
        [SerializeField] float m_MergingRequiredBackGap = 8.0f;
        [SerializeField] float m_MergingRequiredSpace = 14.0f;
        [Header("Wandering")]
        [SerializeField] private float m_LateralWanderDistance = 3f;                              // how far the car will wander laterally towards its target
        [SerializeField] private float m_LateralWanderSpeed = 0.1f;                               // how fast the lateral wandering will fluctuate
        [SerializeField] [Range(0, 1)] private float m_AccelWanderAmount = 0.1f;                  // how much the cars acceleration will wander
        [SerializeField] private float m_AccelWanderSpeed = 0.1f;                                 // how fast the cars acceleration wandering will fluctuate
        [Header("Setting")]
        [SerializeField] private BrakeCondition m_BrakeCondition = BrakeCondition.TargetDistance; // what should the AI consider when accelerating/braking?
        [SerializeField] private bool m_Driving;                                                  // whether the AI is currently actively driving or stopped.
        [SerializeField] private bool m_AcceptsMergingCar = true;
        [Header("Target")]
        [SerializeField] private Transform m_Target;                                              // 'target' the target object to aim for.
        [SerializeField] private bool m_StopWhenTargetReached;                                    // should we stop driving when we reach the target?
        [SerializeField] private float m_ReachTargetThreshold = 2;                                // proximity to target to consider we 'reached' it, and stop driving.
     

        private float m_RandomPerlin;             // A random value for the car to base its wander on (so that AI cars don't all wander in the same pattern)
        private CarController m_CarController;    // Reference to actual car controller we are controlling
        private float m_AvoidOtherCarTime;        // time until which to avoid the car we recently collided with
        private float m_AvoidOtherCarSlowdown;    // how much to slow down due to colliding with another car, whilst avoiding
        private float m_AvoidPathOffset;          // direction (-1 or 1) in which to offset path to avoid other car, whilst avoiding
        private Rigidbody m_Rigidbody;
        private WaypointProgressTracker m_Tracker;

        private StrategyBase DrivingStrategy;
        [Header("Debug")]
        [SerializeField] string StrategyString;
        
        private void Awake()
        {
            // get the car controller reference
            m_CarController = GetComponent<CarController>();
            m_Tracker = GetComponent<WaypointProgressTracker>();
            
            // give the random perlin a random value
            m_RandomPerlin = Random.value*100;

            m_Rigidbody = GetComponent<Rigidbody>();
            DrivingStrategy = new NormalStrategy(this);
        }


        private void FixedUpdate()
        {
            if (m_Target == null || !m_Driving)
            {
                // Car should not be moving,
                // use handbrake to stop
                m_CarController.Move(0, 0, -1f, 1f);
            }
            else
            {
                Vector3 fwd = transform.forward;
                if (m_Rigidbody.velocity.magnitude > m_CarController.MaxSpeed*0.1f)
                {
                    fwd = m_Rigidbody.velocity;
                }

                float desiredSpeed = m_DesiredSpeed;

                // now it's time to decide if we should be slowing down...
                switch (m_BrakeCondition)
                {
                    case BrakeCondition.TargetDirectionDifference:
                        {
                            // the car will brake according to the upcoming change in direction of the target. Useful for route-based AI, slowing for corners.

                            // check out the angle of our target compared to the current direction of the car
                            float approachingCornerAngle = Vector3.Angle(m_Target.forward, fwd);

                            // also consider the current amount we're turning, multiplied up and then compared in the same way as an upcoming corner angle
                            float spinningAngle = m_Rigidbody.angularVelocity.magnitude*m_CautiousAngularVelocityFactor;

                            // if it's different to our current angle, we need to be cautious (i.e. slow down) a certain amount
                            float cautiousnessRequired = Mathf.InverseLerp(0, m_CautiousMaxAngle,
                                                                           Mathf.Max(spinningAngle,
                                                                                     approachingCornerAngle));
                            desiredSpeed = Mathf.Lerp(m_DesiredSpeed, m_DesiredSpeed * m_CautiousSpeedFactor,
                                                      cautiousnessRequired);
                            break;
                        }

                    case BrakeCondition.TargetDistance:
                        {
                            // the car will brake as it approaches its target, regardless of the target's direction. Useful if you want the car to
                            // head for a stationary target and come to rest when it arrives there.

                            // check out the distance to target
                            Vector3 delta = m_Target.position - transform.position;
                            float distanceCautiousFactor = Mathf.InverseLerp(m_CautiousMaxDistance, 0, delta.magnitude);

                            // also consider the current amount we're turning, multiplied up and then compared in the same way as an upcoming corner angle
                            float spinningAngle = m_Rigidbody.angularVelocity.magnitude*m_CautiousAngularVelocityFactor;

                            // if it's different to our current angle, we need to be cautious (i.e. slow down) a certain amount
                            float cautiousnessRequired = Mathf.Max(
                                Mathf.InverseLerp(0, m_CautiousMaxAngle, spinningAngle), distanceCautiousFactor);
                            desiredSpeed = Mathf.Lerp(m_DesiredSpeed, m_DesiredSpeed * m_CautiousSpeedFactor,
                                                      cautiousnessRequired);
                            break;
                        }

                    case BrakeCondition.NeverBrake:
                        break;
                }

                // Evasive action due to collision with other cars:

                // our target position starts off as the 'real' target position
                Vector3 offsetTargetPos = m_Target.position;

                // if are we currently taking evasive action to prevent being stuck against another car:
                if (Time.time < m_AvoidOtherCarTime)
                {
                    // slow down if necessary (if we were behind the other car when collision occured)
                    desiredSpeed *= m_AvoidOtherCarSlowdown;

                    // and veer towards the side of our path-to-target that is away from the other car
                    offsetTargetPos += m_Target.right*m_AvoidPathOffset;
                }
                else
                {
                    // no need for evasive action, we can just wander across the path-to-target in a random way,
                    // which can help prevent AI from seeming too uniform and robotic in their driving
                    offsetTargetPos += m_Target.right*
                                       (Mathf.PerlinNoise(Time.time*m_LateralWanderSpeed, m_RandomPerlin)*2 - 1)*
                                       m_LateralWanderDistance;
                }

                // use different sensitivity depending on whether accelerating or braking:
                // float accelBrakeSensitivity = (desiredSpeed < m_CarController.CurrentSpeed)
                //                                  ? m_BrakeSensitivity
                //                                  : m_AccelSensitivity;

                // decide the actual amount of accel/brake input to achieve desired speed.
               
                var accel = DrivingStrategy.Tick(desiredSpeed);
                DrivingStrategy = DrivingStrategy.nextStrategy;
                StrategyString = DrivingStrategy.GetType().Name;
                
                float accelBrakeSensitivity = (accel < 0.0f)
                                                 ? m_BrakeSensitivity
                                                 : m_AccelSensitivity;
                
                accel *= accelBrakeSensitivity;
                accel = Mathf.Clamp(accel, -1, 1);
                
                // add acceleration 'wander', which also prevents AI from seeming too uniform and robotic in their driving
                // i.e. increasing the accel wander amount can introduce jostling and bumps between AI cars in a race
                accel *= (1 - m_AccelWanderAmount) +
                         (Mathf.PerlinNoise(Time.time*m_AccelWanderSpeed, m_RandomPerlin)*m_AccelWanderAmount);

                // calculate the local-relative position of the target, to steer towards
                Vector3 localTarget = transform.InverseTransformPoint(offsetTargetPos);

                // work out the local angle towards the target
                float targetAngle = Mathf.Atan2(localTarget.x, localTarget.z)*Mathf.Rad2Deg;

                if (Mathf.Abs(targetAngle) < m_SteerAngleThreshold)
                {
                    targetAngle = 0.0f;
                }

                // get the amount of steering needed to aim the car towards the target
                float steer = Mathf.Clamp(targetAngle*m_SteerSensitivity, -1, 1)*Mathf.Sign(m_CarController.CurrentSpeed);

                // feed input to the car controller.
                m_CarController.Move(steer, accel, accel, 0f);                

                // if appropriate, stop driving when we're close enough to the target.
                if (m_StopWhenTargetReached && localTarget.magnitude < m_ReachTargetThreshold)
                {
                    m_Driving = false;
                }
            }
        }


        // 合流可能かどうか
        private bool HasMergingHeads()
        {
            return CarList.Instance.GetAheadGap(m_Tracker, CarList.FindCarOption.InDifferentLane) >= m_MergingRequiredAheadGap
                && CarList.Instance.GetBehindGap(m_Tracker, CarList.FindCarOption.InDifferentLane) >= m_MergingRequiredBackGap;
        }

        private float GetMergingSpace()
        {
            var aheadGap = Mathf.Min(CarList.Instance.GetAheadGap(m_Tracker, CarList.FindCarOption.InDifferentLane), CarList.Instance.GetAheadGap(m_Tracker));
            var behindGap = CarList.Instance.GetBehindGap(m_Tracker, CarList.FindCarOption.InDifferentLane);

            return aheadGap + behindGap;
        }

        WaypointProgressTracker FindMergeAttemptingCar()
        {
            var aheadCar = CarList.Instance.FindAheadCar(m_Tracker, CarList.FindCarOption.InSameLane);
            var siblingLaneAheadCar = CarList.Instance.FindAheadCar(m_Tracker, CarList.FindCarOption.InDifferentLane);
                                
            if (aheadCar != null)
            {
                siblingLaneAheadCar = CarList.Instance.FindBehindCar(aheadCar, CarList.FindCarOption.InDifferentLane);
            }

            if (siblingLaneAheadCar == null)
            {
                siblingLaneAheadCar = CarList.Instance.FindAheadCar(m_Tracker, CarList.FindCarOption.InDifferentLane);
            }

            if (siblingLaneAheadCar != null 
                && ((m_Tracker.carLane == WaypointProgressTracker.CarLane.ThroughLane && siblingLaneAheadCar.GetComponent<CarController>().LeftWinkerOn)
                    || (m_Tracker.carLane == WaypointProgressTracker.CarLane.MergingLane && siblingLaneAheadCar.GetComponent<CarController>().RightWinkerOn)))
            {
                return siblingLaneAheadCar;
            }

            return null;
        }

        private void OnCollisionStay(Collision col)
        {
            // detect collision against other cars, so that we can take evasive action
            if (col.rigidbody != null)
            {
                var otherAI = col.rigidbody.GetComponent<CarAIControl>();
                if (otherAI != null)
                {
                    // we'll take evasive action for 1 second
                    // m_AvoidOtherCarTime = Time.time + 1;

                    // but who's in front?...
                    if (Vector3.Angle(transform.forward, otherAI.transform.position - transform.position) < 90)
                    {
                        // the other ai is in front, so it is only good manners that we ought to brake...
                        // m_AvoidOtherCarSlowdown = 0.5f;
                    }
                    else
                    {
                        // we're in front! ain't slowing down for anybody...
                        // m_AvoidOtherCarSlowdown = 1;
                    }

                    // both cars should take evasive action by driving along an offset from the path centre,
                    // away from the other car
                    var otherCarLocalDelta = transform.InverseTransformPoint(otherAI.transform.position);
                    float otherCarAngle = Mathf.Atan2(otherCarLocalDelta.x, otherCarLocalDelta.z);
                    m_AvoidPathOffset = m_LateralWanderDistance*-Mathf.Sign(otherCarAngle);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var tracker = GetComponent<WaypointProgressTracker>();
            
            if (other.CompareTag("MergeTrigger") && tracker.carLane == WaypointProgressTracker.CarLane.MergingLane)
            {
                GetComponent<CarController>().LeftWinkerOn = true;
                DrivingStrategy = new MergingActivatedStrategy(this);
            }
        }


        public void SetTarget(Transform target)
        {
            m_Target = target;
            m_Driving = true;
        }

        abstract class StrategyBase
        {
            protected StrategyBase()
            {
                nextStrategy = this;
            }
            
            public StrategyBase nextStrategy { get; protected set; } 
            public abstract float Tick(float desiredSpeed);
        }

        class NormalStrategy : StrategyBase
        {
            private readonly CarAIControl car;
            
            public NormalStrategy(CarAIControl controlledCar)
            {
                car = controlledCar;
            }

            public override float Tick(float desiredSpeed)
            {
                var accel = (desiredSpeed - car.m_CarController.CurrentSpeed) * car.m_DesiredSpeedSensitivity;
                var aheadCar = CarList.Instance.FindAheadCar(car.m_Tracker, CarList.FindCarOption.InSameLane);

                if (aheadCar != null)
                {
                    float gap = CarList.Instance.GetAheadGap(car.m_Tracker);
                    accel += (1.0f - car.m_RequiredHeadGap / gap) * car.m_HeadGapSensitivity;
                }
                
                if (car.m_AcceptsMergingCar)
                {
                    var siblingTracker = car.FindMergeAttemptingCar();
                    
                    if (siblingTracker != null)
                    {
                        nextStrategy = new YieldingStrategy(car, siblingTracker);
                    }
                }

                return accel;
            }
        }

        class YieldingStrategy : StrategyBase
        {
            private readonly CarAIControl car;
            private readonly WaypointProgressTracker triggerTracker;
            
            public YieldingStrategy(CarAIControl controlledCar, WaypointProgressTracker tracker)
            {
                car = controlledCar;
                triggerTracker = tracker;
            }
            
            public override float Tick(float desiredSpeed)
            {
                var accel = (desiredSpeed - car.m_CarController.CurrentSpeed) * car.m_DesiredSpeedSensitivity;

                var minGap = Mathf.Min(CarList.Instance.GetAheadGap(car.m_Tracker), CarList.Instance.GetGap(car.m_Tracker, triggerTracker));
                accel += (1.0f - car.m_YieldingRequiredHeadGap / minGap) * car.m_YieldingSensitivity;

                if (triggerTracker.carLane == WaypointProgressTracker.CarLane.InterLane)
                {
                    car.m_AcceptsMergingCar = false;
                    nextStrategy = new NormalStrategy(car);
                } 
                else if (car.FindMergeAttemptingCar() != triggerTracker)
                {
                    nextStrategy = new NormalStrategy(car);
                }
                
                return accel;
            }
        }

        class MergingActivatedStrategy : StrategyBase
        {
            private readonly CarAIControl car;
            private float previousMergingSpace;

            public MergingActivatedStrategy(CarAIControl controlledCar)
            {
                car = controlledCar;
                previousMergingSpace = car.GetMergingSpace();
            }

            public override float Tick(float desiredSpeed)
            {
                var mergingSpace = car.GetMergingSpace();
                var widingSpace = mergingSpace - previousMergingSpace > 0.1f;
                
                var accel = (desiredSpeed * (widingSpace ? 1.0f : car.m_MergingWaitingDesiredSpeedFactor) - car.m_CarController.CurrentSpeed) * car.m_DesiredSpeedSensitivity;
                var aheadCar = CarList.Instance.FindAheadCar(car.m_Tracker, CarList.FindCarOption.InSameLane);
                
                if (aheadCar != null)
                {
                    float gap = CarList.Instance.GetAheadGap(car.m_Tracker);
                    accel += (1.0f - car.m_RequiredHeadGap / gap) * car.m_HeadGapSensitivity;
                }

                if (car.GetMergingSpace() > car.m_MergingRequiredSpace)
                {
                    nextStrategy = new MergeTuningStrategy(car);
                }

                previousMergingSpace = mergingSpace;
                
                return accel;
            }
        }

        class MergeTuningStrategy : StrategyBase
        {
            private readonly CarAIControl car;
            
            public MergeTuningStrategy(CarAIControl controlledCar)
            {
                car = controlledCar;
            }
            
            public override float Tick(float desiredSpeed)
            {
                var accel = (desiredSpeed - car.m_CarController.CurrentSpeed) * car.m_DesiredSpeedSensitivity * car.m_MergingDesiredSpeedFactor;

                var aheadAccel = 1.0f - car.m_RequiredHeadGap / CarList.Instance.GetAheadGap(car.m_Tracker);
                var siblingAccel = 1.0f - car.m_MergingRequiredAheadGap * car.m_MergingRequiredHeadGapFactor / CarList.Instance.GetAheadGap(car.m_Tracker, CarList.FindCarOption.InDifferentLane);

                accel += Mathf.Min(aheadAccel, siblingAccel) * car.m_HeadGapSensitivity;

                if (car.HasMergingHeads())
                {
                    car.m_Tracker.ChangeLane(WaypointProgressTracker.CarLane.ThroughLane);
                    nextStrategy = new MergingStrategy(car, WaypointProgressTracker.CarLane.ThroughLane);
                }
                else if (car.GetMergingSpace() < car.m_MergingRequiredSpace)
                {
                    nextStrategy = new MergingActivatedStrategy(car);
                }
                
                return accel;
            }
        }

        class MergingStrategy : StrategyBase
        {
            private readonly CarAIControl car;
            private readonly WaypointProgressTracker.CarLane targetLane;
            private readonly WaypointProgressTracker.CarLane originalLane;
            
            public MergingStrategy(CarAIControl controlledCar, WaypointProgressTracker.CarLane target)
            {
                car = controlledCar;
                targetLane = target;
                
                originalLane = car.m_Tracker.carLane;
            }
            
            public override float Tick(float desiredSpeed)
            {
                var accel = (desiredSpeed - car.m_CarController.CurrentSpeed) * car.m_DesiredSpeedSensitivity * car.m_MergingDesiredSpeedFactor;

                var aheadAccel = 1.0f - car.m_RequiredHeadGap / CarList.Instance.GetAheadGap(car.m_Tracker);
                var siblingAccel = 1.0f - Mathf.Max(car.m_RequiredHeadGap * car.m_MergingRequiredHeadGapFactor, car.m_MergingRequiredAheadGap) / CarList.Instance.GetAheadGap(car.m_Tracker, CarList.FindCarOption.InDifferentLane);

                accel += Mathf.Min(aheadAccel, siblingAccel) * car.m_HeadGapSensitivity;

                if (car.m_Tracker.carLane == targetLane)
                {
                    nextStrategy = new NormalStrategy(car);
                } 
                else if (!car.HasMergingHeads())
                {
                    //car.m_Tracker.ChangeLane(originalLane);
                    //car.GetComponent<CarController>().LeftWinkerOn = true;
                    //nextStrategy = new MergeTuningStrategy(car);
                }
                
                return accel;
            }
        }
    }
}
