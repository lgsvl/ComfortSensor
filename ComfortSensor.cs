/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using Simulator.Analysis;
using Simulator.Api;
using Simulator.Bridge;
using Simulator.Sensors.UI;
using Simulator.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Sensors
{
    [SensorType("Comfort", new System.Type[] { typeof(ComfortData) })]
    public class ComfortSensor : SensorBase
    {
        [AnalysisMeasurement(MeasurementType.Velocity)]
        public float SpeedMin = float.MaxValue;
        [AnalysisMeasurement(MeasurementType.Velocity)]
        public float SpeedMax = 0f;
        [AnalysisMeasurement(MeasurementType.Velocity)]
        public float SpeedAvg = 0f;
        private float SpeedTotal = 0f;
        private int SpeedCount = 0;
        private float PrevSpeed = 0f;
        private float Speed= 0f;

        private float SteerAngle = 0f;
        private float PrevSteerAngle = 0f;

        [AnalysisMeasurement(MeasurementType.Angle)]
        public float SteerAngleMax = 0f;
        private Vector3 Position = new Vector3();
        private Vector3 Velocity = new Vector3();
        private Vector3 Accel = new Vector3();
        private Vector3 Jerk = new Vector3();

        [AnalysisMeasurement(MeasurementType.Jerk)]
        public float JerkMin = float.MaxValue;
        [AnalysisMeasurement(MeasurementType.Jerk)]
        public float JerkMax = 0f;
        [AnalysisMeasurement(MeasurementType.Jerk)]
        public float JerkAvg = 0f;
        private float JerkTotal = 0f;
        private int JerkCount = 0;

        [SensorParameter]
        public float MaxAccelAllowed = 900f;
        [SensorParameter]
        public float MaxJerkAllowed = 600f;
        [SensorParameter]
        public float MaxBrakeAllowed = 0f;
        [SensorParameter]
        public float MaxSuddenSteerAllowed = 0f;

        private Quaternion Rotation;
        private float AngularVelocity = 0f;
        private float AngularAcceleration = 0f;
        [SensorParameter]
        public float MaxAngularVelocityAllowed = 0f;
        [SensorParameter]
        public float MaxAngularAccelerationAllowed = 0f;
        [SensorParameter]
        public float RollTolerance = 0f;
        [SensorParameter]
        public float MinSlipVelocity = 0f;
        [SensorParameter]
        public float SlipTolerance = 0f;
        public float Slip;
        public float Detected;

        private BridgeInstance Bridge;
        private Publisher<ComfortData> Publish;
        private IAgentController Controller;
        private IVehicleDynamics Dynamics;

        [SensorParameter]
        public float EventRate = 3f;
        private float EventTimer = 0f;

        [AnalysisMeasurement(MeasurementType.Misc)]
        public bool IsFellOff = false;
        private RaycastHit FellOffRayHit = new RaycastHit();
        private LayerMask FellOffMask => LayerMask.GetMask("Default");

        protected override void Initialize()
        {
            Controller = GetComponentInParent<IAgentController>();
            Dynamics = GetComponentInParent<IVehicleDynamics>();
            Position = transform.position;
            Rotation = transform.rotation;
        }

        protected override void Deinitialize()
        {
            //
        }

        public override void OnBridgeSetup(BridgeInstance bridge)
        {
            if (bridge.Plugin.Factory is Bridge.Cyber.CyberBridgeFactory)
            {
                return;
            }

            Bridge = bridge;
            Publish = Bridge.AddPublisher<ComfortData>(Topic);
            Bridge.AddSubscriber<ComfortData>(Topic, data => Detected = data.acceleration.magnitude);
        }

        public override Type GetDataBridgePlugin()
        {
            return typeof(ComfortDataBridgePlugin);
        }

        public void FixedUpdate()
        {
            if (Time.timeScale == 0f)
                return;

            CalculateValues();

            if (!IsWithinRange() && EventTimer > EventRate)
            {
                EventTimer = 0f;
                var jsonData = new JSONObject();
                jsonData.Add("velocity", Velocity.magnitude);
                jsonData.Add("acceleration", Accel.magnitude);
                jsonData.Add("jerk", Jerk.magnitude);
                jsonData.Add("angularVelocity", AngularVelocity);
                jsonData.Add("angularAcceleration", AngularAcceleration);
                jsonData.Add("roll", transform.rotation.eulerAngles.z);
                jsonData.Add("slip", Slip);

                if (ApiManager.Instance != null)
                {
                    ApiManager.Instance.AddCustom(transform.parent.gameObject, "comfort", jsonData);
                }

                if (Bridge != null && Bridge.Status == Status.Connected)
                {
                    Publish(new ComfortData()
                    {
                        velocity = Velocity,
                        acceleration = Accel,
                        jerk = Jerk,
                        angularVelocity = AngularVelocity,
                        angularAcceleration = AngularAcceleration,
                        roll = transform.rotation.eulerAngles.z,
                        slip = Slip
                    });
                }
            }
        }

        public void CalculateValues()
        {
            PrevSpeed = Speed;
            Speed = Dynamics.Velocity.magnitude;
            SpeedTotal += Speed;
            SpeedCount++;
            SpeedAvg = SpeedTotal / SpeedCount;
            UpdateMinMax(Speed, ref SpeedMin, ref SpeedMax);
            if (MaxBrakeAllowed > 0 && PrevSpeed > Speed && Mathf.Abs(PrevSpeed - Speed) > MaxBrakeAllowed)
            {
                SuddenBrakeEvent(Controller.GTID);
            }

            PrevSteerAngle = SteerAngle;
            SteerAngle = Dynamics.WheelAngle;
            SteerAngleMax = SteerAngle > SteerAngleMax ? SteerAngle : SteerAngleMax;
            if (MaxSuddenSteerAllowed > 0 && Mathf.Abs(PrevSteerAngle - SteerAngle) > MaxSuddenSteerAllowed)
            {
                SuddenSteerEvent(Controller.GTID);
            }

            Vector3 posDelta = Dynamics.Velocity;
            Vector3 velocityDelta = (posDelta - Velocity) / Time.fixedDeltaTime;
            Vector3 accelDelta = (velocityDelta - Accel) / Time.fixedDeltaTime;
            
            Jerk = accelDelta;
            JerkTotal += Jerk.magnitude;
            JerkCount++;
            JerkAvg = JerkTotal / JerkCount;
            UpdateMinMax(Jerk.magnitude, ref JerkMin, ref JerkMax);
            JerkMin *= 0.001f;
            JerkMax *= 0.001f;
            JerkAvg *= 0.001f;

            Accel = velocityDelta;
            Velocity = posDelta;
            Position = transform.position;
            float angleDelta = Quaternion.Angle(Rotation, transform.rotation) / Time.fixedDeltaTime;
            float angularVelocityDelta = (angleDelta - AngularVelocity) / Time.fixedDeltaTime;
            AngularAcceleration = angularVelocityDelta;
            AngularVelocity = angleDelta;
            Rotation = transform.rotation;
            Slip = Vector3.Angle(Dynamics.Velocity, transform.forward);

            if (!IsFellOff)
            {
                if (!Physics.Raycast(Position + Vector3.up * 5f, Vector3.down, out FellOffRayHit, 10f, FellOffMask) && Controller.Velocity.y < -1.0f)
                {
                    FellOffEvent(Controller.GTID);
                    IsFellOff = true;
                }
            }

            EventTimer += Time.fixedDeltaTime;
        }

        public bool IsWithinRange()
        {
            if (MaxAccelAllowed > 0 && Accel.magnitude > MaxAccelAllowed)
            {
                return false;
            }

            if (MaxJerkAllowed > 0 && Jerk.magnitude > MaxJerkAllowed)
            {
                return false;
            }

            if (MaxAngularVelocityAllowed > 0 && AngularVelocity > MaxAngularVelocityAllowed)
            {
                return false;
            }

            if (MaxAngularAccelerationAllowed > 0 && AngularAcceleration > MaxAngularAccelerationAllowed)
            {
                return false;
            }

            float zAxis = transform.rotation.eulerAngles.z;
            float roll = zAxis < 180 ? zAxis : Mathf.Abs(zAxis - 360);
            if (RollTolerance > 0 && roll > RollTolerance)
            {
                return false;
            }

            if (MinSlipVelocity > 0 && SlipTolerance > 0 && Velocity.magnitude > MinSlipVelocity && Slip > SlipTolerance)
            {
                SuddenSteerEvent(Controller.GTID);
                return false;
            }

            return true;
        }

        private void UpdateMinMax(float value, ref float min, ref float max)
        {
            if (value < min)
            {
                min = value;
            }

            if (value > max)
            {
                max = value;
            }
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            var graphData = new Dictionary<string, object>()
            {
                { "velocity", Velocity.magnitude },
                { "acceleration", Accel.magnitude },
                { "jerk", Jerk.magnitude },
                { "angularVelocity", AngularVelocity },
                { "angularAcceleration", AngularAcceleration },
                { "roll", transform.rotation.eulerAngles.z },
                { "slip", Slip }
            };

            visualizer.UpdateGraphValues(graphData);
        }

        private void SuddenBrakeEvent(uint id)
        {
            Hashtable data = new Hashtable
            {
                { "Id", id },
                { "Type", "SuddenBrake" },
                { "Time", SimulatorManager.Instance.GetSessionElapsedTimeSpan().ToString() },
                { "Status", AnalysisManager.AnalysisStatusType.Failed },
            };
            SimulatorManager.Instance.AnalysisManager.AddEvent(data);
        }

        private void SuddenSteerEvent(uint id)
        {
            Hashtable data = new Hashtable
            {
                { "Id", id },
                { "Type", "SuddenSteer" },
                { "Time", SimulatorManager.Instance.GetSessionElapsedTimeSpan().ToString() },
                { "Status", AnalysisManager.AnalysisStatusType.Failed },
            };
            SimulatorManager.Instance.AnalysisManager.AddEvent(data);
        }

        private void FellOffEvent(uint id)
        {
            Hashtable data = new Hashtable
            {
                { "Id", id },
                { "Type", "FellOff" },
                { "Time", SimulatorManager.Instance.GetSessionElapsedTimeSpan().ToString() },
                { "Status", AnalysisManager.AnalysisStatusType.Failed },
            };
            SimulatorManager.Instance.AnalysisManager.AddEvent(data);
        }

        public override void OnVisualizeToggle(bool state)
        {
        }
    }
}
