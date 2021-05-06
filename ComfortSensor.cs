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
        float prevSpeed;
        float speed;
        float steerAngle;
        float prevSteerAngle;
        Vector3 position;
        Vector3 velocity;
        Vector3 accel;
        Vector3 jerk;
        [SensorParameter]
        public float MaxAccelAllowed = 10f;
        [SensorParameter]
        public float MaxJerkAllowed = 10f;
        [SensorParameter]
        public float MaxBrakeAllowed = 10f;
        [SensorParameter]
        public float MaxSuddenSteerAllowed = 10f;

        Quaternion rotation;
        float angularVelocity;
        float angularAcceleration;
        [SensorParameter]
        public float MaxAngularVelocityAllowed = 10f;
        [SensorParameter]
        public float MaxAngularAccelerationAllowed = 10f;
        [SensorParameter]
        public float RollTolerance = 10f;
        [SensorParameter]
        public float MinSlipVelocity = 10f;
        [SensorParameter]
        public float SlipTolerance = 10f;
        public float slip;
        public float Detected;

        private BridgeInstance Bridge;
        private Publisher<ComfortData> Publish;
        private IAgentController Controller;
        private IVehicleDynamics Dynamics;

        private void Start()
        {
            Controller = GetComponentInParent<IAgentController>();
            Dynamics = GetComponentInParent<IVehicleDynamics>();
            position = transform.position;
            rotation = transform.rotation;
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

            if (!IsWithinRange())
            {
                Debug.Log("ComfortSensor is Outside of Range");
                var jsonData = new JSONObject();
                jsonData.Add("velocity", velocity.magnitude);
                jsonData.Add("acceleration", accel.magnitude);
                jsonData.Add("jerk", jerk.magnitude);
                jsonData.Add("angularVelocity", angularVelocity);
                jsonData.Add("angularAcceleration", angularAcceleration);
                jsonData.Add("roll", transform.rotation.eulerAngles.z);
                jsonData.Add("slip", slip);

                if (ApiManager.Instance != null)
                {
                    ApiManager.Instance.AddCustom(transform.parent.gameObject, "comfort", jsonData);
                }

                if (Bridge != null && Bridge.Status == Status.Connected)
                {
                    Debug.Log("Writing to existing bridge");
                    Publish(new ComfortData()
                    {
                        velocity = velocity,
                        acceleration = accel,
                        jerk = jerk,
                        angularVelocity = angularVelocity,
                        angularAcceleration = angularAcceleration,
                        roll = transform.rotation.eulerAngles.z,
                        slip = slip
                    });
                }
            }
        }

        public void CalculateValues()
        {
            prevSpeed = speed;
            speed = Dynamics.Velocity.magnitude;
            if (MaxBrakeAllowed > 0 && prevSpeed > speed && Mathf.Abs(prevSpeed - speed) > MaxBrakeAllowed)
            {
                SuddenBrakeEvent(Controller.GTID);
            }

            prevSteerAngle = steerAngle;
            steerAngle = Dynamics.WheelAngle;
            if (MaxSuddenSteerAllowed > 0 && Mathf.Abs(prevSteerAngle - steerAngle) > MaxSuddenSteerAllowed)
            {
                SuddenSteerEvent(Controller.GTID);
            }

            Vector3 posDelta = Dynamics.Velocity;
            Vector3 velocityDelta = (posDelta - velocity) / Time.fixedDeltaTime;
            Vector3 accelDelta = (velocityDelta - accel) / Time.fixedDeltaTime;
            jerk = accelDelta;
            accel = velocityDelta;
            velocity = posDelta;
            position = transform.position;
            float angleDelta = Quaternion.Angle(rotation, transform.rotation) / Time.fixedDeltaTime;
            float angularVelocityDelta = (angleDelta - angularVelocity) / Time.fixedDeltaTime;
            angularAcceleration = angularVelocityDelta;
            angularVelocity = angleDelta;
            rotation = transform.rotation;
            slip = Vector3.Angle(Dynamics.Velocity, transform.forward);
        }

        public bool IsWithinRange()
        {
            if (MaxAccelAllowed > 0 && accel.magnitude > MaxAccelAllowed)
            {
                Debug.Log($"MaxAccel {MaxAccelAllowed} / value {accel.magnitude}");
                return false;
            }

            if (MaxJerkAllowed > 0 && jerk.magnitude > MaxJerkAllowed)
            {
                Debug.Log($"MaxJerk {MaxJerkAllowed} / value {jerk.magnitude}");
                return false;
            }

            if (MaxAngularVelocityAllowed > 0 && angularVelocity > MaxAngularVelocityAllowed)
            {
                Debug.Log($"MaxAngVel {MaxAngularVelocityAllowed} / value {angularVelocity}");
                return false;
            }

            if (MaxAngularAccelerationAllowed > 0 && angularAcceleration > MaxAngularAccelerationAllowed)
            {
                Debug.Log($"MaxAngAccel {MaxAngularAccelerationAllowed} / value {angularAcceleration}");
                return false;
            }

            float zAxis = transform.rotation.eulerAngles.z;
            float roll = zAxis < 180 ? zAxis : Mathf.Abs(zAxis - 360);
            if (RollTolerance > 0 && roll > RollTolerance)
            {
                Debug.Log($"Roll {RollTolerance} / value {RollTolerance}");
                return false;
            }

            if (MinSlipVelocity > 0 && SlipTolerance > 0 && velocity.magnitude > MinSlipVelocity && slip > SlipTolerance)
            {
                Debug.Log($"MinSlipVel {MinSlipVelocity} / value {velocity.magnitude}    SlipTolerance {SlipTolerance} / value {slip}");
                SuddenSteerEvent(Controller.GTID);
                return false;
            }

            return true;
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            var graphData = new Dictionary<string, object>()
            {
                { "velocity", velocity.magnitude },
                { "acceleration", accel.magnitude },
                { "jerk", jerk.magnitude },
                { "angularVelocity", angularVelocity },
                { "angularAcceleration", angularAcceleration },
                { "roll", transform.rotation.eulerAngles.z },
                { "slip", slip }
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

        public override void OnVisualizeToggle(bool state)
        {
        }
    }
}
