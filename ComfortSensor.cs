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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Sensors
{
    [SensorType("Comfort", new System.Type[] { typeof(ComfortData) })]
    public class ComfortSensor : SensorBase
    {
        new Rigidbody rigidbody;

        float prevSpeed;
        float speed;
        float steerAngle;
        float prevSteerAngle;
        Vector3 position;
        Vector3 velocity;
        Vector3 accel;
        Vector3 jerk;
        [SensorParameter]
        public float maxAccelAllowed;
        [SensorParameter]
        public float maxJerkAllowed;
        [SensorParameter]
        public float maxBrakeAllowed;
        [SensorParameter]
        public float maxSuddenSteerAllowed = 10f;

        Quaternion rotation;
        float angularVelocity;
        float angularAcceleration;
        [SensorParameter]
        public float maxAngularVelocityAllowed;
        [SensorParameter]
        public float maxAngularAccelerationAllowed;
        [SensorParameter]
        public float rollTolerance;
        [SensorParameter]
        public float minSlipVelocity;
        [SensorParameter]
        public float slipTolerance;
        public float slip;
        public float Detected;

        private BridgeInstance Bridge;
        private Publisher<ComfortData> Publish;
        private AgentController AgentController;
        private IVehicleDynamics Dynamics;

        private void Start()
        {
            rigidbody = transform.parent.GetComponentInChildren<Rigidbody>();
            AgentController = GetComponentInParent<AgentController>();
            Dynamics = GetComponentInParent<IVehicleDynamics>();
        }

        public override void OnBridgeSetup(BridgeInstance bridge)
        {
            Bridge = bridge;
            Publish = Bridge.AddPublisher<ComfortData>(Topic);
            Bridge.AddSubscriber<ComfortData>(Topic, data => Detected = data.acceleration.magnitude);
        }

        public void FixedUpdate()
        {
            CalculateValues();

            if (!IsWithinRange())
            {
                Debug.Log("Outside of Range");
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
            speed = rigidbody.velocity.magnitude;
            if (Mathf.Abs(prevSpeed - speed) > maxBrakeAllowed)
            {
                SuddenBrakeEvent(AgentController.GTID);
            }

            prevSteerAngle = steerAngle;
            steerAngle = Dynamics.WheelAngle;
            if (Mathf.Abs(prevSteerAngle - steerAngle) > maxSuddenSteerAllowed)
            {
                SuddenSteerEvent(AgentController.GTID);
            }

            Vector3 posDelta = rigidbody.velocity;
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
            slip = Vector3.Angle(rigidbody.velocity, transform.forward);
        }

        public bool IsWithinRange()
        {
            if (maxAccelAllowed > 0 && accel.magnitude > maxAccelAllowed)
            {
                return false;
            }

            if (maxJerkAllowed > 0 && jerk.magnitude > maxJerkAllowed)
            {
                return false;
            }

            if (maxAngularVelocityAllowed > 0 && angularVelocity > maxAngularVelocityAllowed)
            {
                return false;
            }

            if (maxAngularAccelerationAllowed > 0 && angularAcceleration > maxAngularAccelerationAllowed)
            {
                return false;
            }

            float zAxis = transform.rotation.eulerAngles.z;
            float roll = zAxis < 180 ? zAxis : Mathf.Abs(zAxis - 360);
            if (rollTolerance > 0 && roll > rollTolerance)
            {
                return false;
            }

            if (minSlipVelocity > 0 && slipTolerance > 0 && velocity.magnitude > minSlipVelocity && slip > slipTolerance)
            {
                SuddenSteerEvent(AgentController.GTID);
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
