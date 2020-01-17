/**
* Copyright (c) 2020 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/
using Simulator.Bridge;
using System;

namespace Simulator.Sensors
{
    public class ComfortDataConverters : IBridgeConverter<ComfortData>
    {
        public Func<ComfortData, object> GetConverter(IBridge bridge)
        {
            if (bridge.GetType() == typeof(Bridge.Ros.Bridge))
            {
                return (c) =>
                {
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
                };
            }
            else if (bridge.GetType() == typeof(Bridge.Cyber.Bridge))
            {
                return (c) =>
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
                };
            }

            throw new System.Exception("ComfortSensor not implemented for this bridge type!");

            return null;
        }

        public Type GetOutputType(IBridge bridge)
        {
            if (bridge.GetType() == typeof(Bridge.Ros.Bridge))
            {
                return typeof(ros_ComfortBridgeData);
            }
            else if (bridge.GetType() == typeof(Bridge.Cyber.Bridge))
            {
                return typeof(cyber_ComfortBridgeData);
            }

            throw new System.Exception("ComfortSensor not implemented for this bridge type!");

            return null;
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


    [Bridge.Ros.MessageType("lgsvl_msgs/ComfortData")]
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