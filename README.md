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

# Custom Message

To implement custom messages, contained in a given Sensor Plugin project there must be an IDataConverter<Type> implementation. In this example project this is contained in ComfortData.cs

The interface requires the following functions to be implemented:

```public Func<ComfortData, object> GetConverter(IBridge bridge);```

Which provides the type conversion functions from the Sensor Plugin into the appropriate types for writing to the bridge

```public Type GetOutputType(IBridge bridge)```

Which provides the type expected for a reader implemented with the Sensor Plugin to convert into its implemented type

The IDataConverter is automatically loaded by the bridge on startup and will allow the Sensor Plugin to read/write custom messages to/from the bridge as long as in the sensor's OnBridgeSetup callback AddWriter() and AddReader() are called respectively 

# Python API example

Sensor will be calling `custom` callback in Python API with `kind` set to `comfort`.
Here is an example of Python API using this callback:

```python
#!/usr/bin/env python3

import os
import lgsvl

# load map
sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "BorregasAve":
  sim.reset()
else:
  sim.load("BorregasAve")

spawns = sim.get_spawn()

# create vehicle (make sure you add Comfort Sensor to its sensors in WebUI)
state = lgsvl.AgentState()
state.transform = spawns[0]
a = sim.add_agent("Lincoln2017MKZ (Apollo 5.0)", lgsvl.AgentType.EGO, state)

# custom callback that will be assigned to agent
def onCustom(agent, kind, context):
  if kind == "comfort":
    print("Comfort sensor callback!", context)
  else:
    # ignore other custom callbacks
    pass

# set callback & run simulation
a.on_custom(onCustom)
sim.run()
```

## Copyright and License

Copyright (c) 2019 LG Electronics, Inc.

This software contains code licensed as described in LICENSE.
