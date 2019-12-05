# Comfort Sensor

This repository contains the code and assets for a custom Sensor Plugin - Comfort Sensor.

To use this Sensor Plugin:

1) clone the Sensor Plugin repo into Assets/External/Sensors as Assets/External/Sensors/ComfortSensor inside of your Simulator Unity Project

2) build the Sensor Plugin for use with the Simulator, navigate to the `Simulator -> Build Sensors` Unity Editor menu item. Clicking on it will build every custom sensor in the Assets/External/Sensors directory and will output built Sensor Plugin bundles to the AssetBundles/Sensors folder

3) on simulation startup, the Simulator will load all custom Sensor Plugin bundles in AssetBundles/Sensors directory and will be a valid sensor in a vehicle's configuration JSON

4) add json configuration (see below) to vehicle of your choosing and launch simulation

Comfort Sensor will detect whether a vehicle's acceleration, rotation or other values are out of acceptable ranges.

# Parameters

maxAccelAllowed - Maximum m/s^2 allowed

maxJerkAllowed - Maximum m/s^3 allowed

maxAngularVelocityAllowed - Maximum deg/s allowed

maxAngularAccelerationAllowed - Maximum deg/s^2 allowed

rollTolerance - Maximum deg rotation on the x axis

slipTolerance - Maximum deg difference between vehicle's velocity and vehicle's forward

Example sensor config JSON:

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
