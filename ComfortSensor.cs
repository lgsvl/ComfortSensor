/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using Simulator.Api;
using Simulator.Bridge;
using Simulator.Sensors.UI;
using Simulator.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Sensors
{
    [SensorType("Comfort", new System.Type[] { typeof(ComfortData) })]
    public class ComfortSensor : SensorBase
    {
        new Rigidbody rigidbody;

        Vector3 position;
        Vector3 velocity;
        Vector3 accel;
        Vector3 jerk;
        [SensorParameter]
        public float maxAccelAllowed;
        [SensorParameter]
        public float maxJerkAllowed;

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

        private IBridge Bridge;
        private IWriter<ComfortData> Writer;

        private void Start()
        {
            rigidbody = transform.parent.GetComponentInChildren<Rigidbody>();
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            Debug.Log("Bridge Setup");
            Bridge = bridge;
            if (Bridge.GetType() == typeof(Bridge.Ros.Bridge))
            {
                AddRosWriter();
            }else if (Bridge.GetType() == typeof(Bridge.Cyber.Bridge))
            {
                AddCyberWriter();
            }
            else
            {
                Debug.LogError("Bridge not recognized");
                throw new System.Exception("Writer for ComfortSensor not implemented");
            }
        }

        private void AddCyberWriter()
        {
            Writer = Bridge.AddCustomWriter<ComfortData, cyber_ComfortBridgeData>(Topic, (c) =>
            {
                return new cyber_ComfortBridgeData
                {
                    converted_acceleration = c.acceleration,
                    converted_angularAcceleration = c.angularAcceleration,
                    converted_angularVelocity = c.angularVelocity,
                    converted_jerk = c.jerk,
                    converted_roll = c.roll,
                    converted_slip = c.slip,
                    converted_velocity = c.velocity
                };
            });
        }

        private void AddRosWriter()
        {
            Debug.Log("Adding writer to Ros Bridge");
            Writer = Bridge.AddCustomWriter<ComfortData, ros_ComfortBridgeData>(Topic, (c) =>
            {
                Debug.Log("Writing to Ros Bridge");
                return new ros_ComfortBridgeData
                {
                    converted_acceleration = c.acceleration,
                    converted_angularAcceleration = c.angularAcceleration,
                    converted_angularVelocity = c.angularVelocity,
                    converted_jerk = c.jerk,
                    converted_roll = c.roll,
                    converted_slip = c.slip,
                    converted_velocity = c.velocity
                };
            });
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
                    Writer.Write(new ComfortData()
                    {
                        velocity = velocity.magnitude,
                        acceleration = accel.magnitude,
                        jerk = jerk.magnitude,
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

        public override void OnVisualizeToggle(bool state)
        {
        }
    }

    [Bridge.Ros.MessageType("lgsvl_msgs/ComfortData")]
    public class ComfortData
    {
        public float velocity;
        public float acceleration;
        public float jerk;
        public float angularVelocity;
        public float angularAcceleration;
        public float roll;
        public float slip;
    }

    [Bridge.Ros.MessageType("lgsvl_msgs/ros_ComfortBridgeData")]
    public class ros_ComfortBridgeData
    {
        public float converted_velocity;
        public float converted_acceleration;
        public float converted_jerk;
        public float converted_angularVelocity;
        public float converted_angularAcceleration;
        public float converted_roll;
        public float converted_slip;
    }

    [Bridge.Ros.MessageType("lgsvl_msgs/cyber_ComfortBridgeData")]
    public class cyber_ComfortBridgeData
    {
        public float converted_velocity;
        public float converted_acceleration;
        public float converted_jerk;
        public float converted_angularVelocity;
        public float converted_angularAcceleration;
        public float converted_roll;
        public float converted_slip;
    }
}
