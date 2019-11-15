# Sensor Plugins

This repository contains the code and assets for a custom Sensor Plugin

To use this Sensor Plugin, clone the Sensor Plugin repo into Assets/External/Sensors as Assets/External/Sensors/{PluginName} inside of your Simulator Unity Project

To build the Sensor Plugin for use with the Simulator, navigate to the Simulator -> Build Sensors Unity Editor menu item. Clicking on it will build every custom sensor in the Assets/External/Sensors directory and will output built Sensor Plugin bundles to the AssetBundles/Sensors folder

On simulation startup, the Simulator will load all custom Sensor Plugin bundles in AssetBundles/Sensors directory and will be a valid sensor in a vehicle's configuration JSON

Each Sensor Plugin repo must contain a Unity prefab with the same name as the directory ({PluginName}.prefab) that must have a script that inherits from SensorBase on the root object of the prefab

Custom sensors must have SensorType attribute which specifies the kind of sensor being implemented as well as the type of data that the sensor sends over the bridge. In addition, it must have SensorBase as the base class and must implement the OnBridgeSetup, OnVisualize, and OnVisualizeToggle methods.

SensorBase in inherited from Unity's Monobehavior so any of the Messages can be used to control how and when the sensor collects data.

# Comfort Sensor

Comfort Sensor detects whether a vehicle's acceleration or rotation values are out of acceptable ranges

maxAccelAllowed - Maximum m/s^2 allowed

maxJerkAllowed - Maximum m/s^3 allowed

maxAngularVelocityAllowed - Maximum deg/s allowed

maxAngularAccelerationAllowed - Maximum deg/s^2 allowed

rollTolerance - Maximum deg rotation on the x axis

slipTolerance - Maximum deg difference between vehicle's velocity and vehicle's forward

Sample sensor config JSON:

```json
{
"type" : "Comfort",
"name" : "Comfort Sensor",    
"params": {
      "maxAccelAllowed": 8,
      "maxJerkAllowed": 4,
      "maxAngularVelocityAllowed": 200,
      "maxAngularAccelerationAllowed": 100,
      "rollTolerance": 10,
      "slipTolerance": 15
    }
}
```

## Copyright and License

Copyright (c) 2019 LG Electronics, Inc.

This software contains code licensed as described in LICENSE.