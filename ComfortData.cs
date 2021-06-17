/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
*/

using UnityEngine;
using Simulator.Bridge;

namespace Simulator.Sensors
{
    // generic non bridge specific data type that is produced by sensor
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

    // actual ROS type
    // can use anything from Simulator.Bridge.Ros.Ros namespace
    [Bridge.MessageType("lgsvl_msgs/ComfortData")]
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

    public class ComfortDataBridgePlugin : ISensorBridgePlugin
    {
        public void Register(IBridgePlugin plugin)
        {
            if (plugin.GetBridgeNameAttribute().Name == "ROS")
            {
                // ROS factory default RegPublisher method performs two actions:
                // 1) registers ComfortData type as supported data type for sensors
                // 2) registers converter (ComfortData => ros_ComoftBridgeData) for creating publishers
                plugin.Factory.RegPublisher(plugin,
                    (ComfortData data) => new ros_ComfortBridgeData()
                    {
                        acceleration = data.acceleration.magnitude,
                        angularAcceleration = data.angularAcceleration,
                        angularVelocity = data.angularVelocity,
                        jerk = data.jerk.magnitude,
                        roll = data.roll,
                        slip = data.slip,
                        velocity = data.velocity.magnitude,
                    }
                );
            }
        }
    }
}
