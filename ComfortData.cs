/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
*/
using Simulator.Bridge;
using System;
using UnityEngine;

namespace Simulator.Sensors
{
    public class ComfortDataConverters : IDataConverter<ComfortData>
    {
        public Func<ComfortData, object> GetConverter(IBridge bridge)
        {
            if (bridge.GetType() == typeof(Bridge.Ros.Bridge))
            {
                return (c) =>
                {
                    return new ros_ComfortBridgeData
                    {
                        acceleration = c.acceleration.magnitude,
                        angularAcceleration = c.angularAcceleration,
                        angularVelocity = c.angularVelocity,
                        jerk = c.jerk.magnitude,
                        roll = c.roll,
                        slip = c.slip,
                        velocity = c.velocity.magnitude
                    };
                };
            }

            throw new System.Exception("ComfortSensor not implemented for this bridge type!");
        }

        public Type GetOutputType(IBridge bridge)
        {
            if (bridge.GetType() == typeof(Bridge.Ros.Bridge))
            {
                return typeof(ros_ComfortBridgeData);
            }

            throw new System.Exception("ComfortSensor not implemented for this bridge type!");
        }

    }

    [Bridge.Ros.MessageType("lgsvl_msgs/ComfortData")]
    public class ComfortData
    {
        public Vector3 velocity;
        public Vector3 acceleration;
        public Vector3 jerk;
        public float angularVelocity;
        public float angularAcceleration;
        public float roll;
        public float slip;
    }

    [Bridge.Ros.MessageType("lgsvl_msgs/ComfortData")]
    public class ros_ComfortBridgeData
    {
        public float velocity;
        public float acceleration;
        public float jerk;
        public float angularVelocity;
        public float angularAcceleration;
        public float roll;
        public float slip;
    }
}