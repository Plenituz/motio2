# **Developer Introduction**

In this article I will explain the basics of the inner workings of Water Motion so you can get started and build some nodes. Right now you can only
develop GraphicsAffectingNodes and PropertyAffectingNodes but in the futur you will be able to script the entire software: write export plugins, interface plugins,
node tools etc...

## The events

A node has 3 main methods you can override: 

```c#
void SetupNode()
```

You can think of `SetupNode()` as the constructor of the node. You can't use the real constructor because when loading from a file the constructore might not be called. So we created the `SetupNode()` to remove that uncertainty. In this method you can do anything you would do in a constructor, but should NEVER create 
properties. Creating properties here may result in duplicate properties when loading from a file.

```c#
void SetupProperties()
```

`SetupProperties()` is dedicated to the creation of properties. It is guaranteed to be called only when the node needs it's properties initialized. 
This means it will not be called when creating the node from a file. Since the file already contains the data for the properties they will be recreated 
directly. Basically this is called once per node, ever. Even if the user turns off Water Motion.

```c#
void EvaluateFrame(int frame, DataFeed dataFeed)
```

This is where the magic happens. `EvaluateFrame(int, DataFeed)` will be called in a background thread once per frame. In this method you should
use the data from `dataFeed` and the properties of the node to either create new data or modify the existing one. The result of your operation 
should be stored back into the right channel of `dataFeed`. More on the DataFeed and channels below.

>[!Warning]
>_This method might be called from several threads at the same time, so anything you do in it should be thread safe._

## `DataFeed` and it's channels

The [DataFeed](/doc/api/Motio.NodeCore.Utils.DataFeed.html) is the class that is used to communicate data between nodes. 
Since nodes can generate data of different shapes and sizes the DataFeed has a system of channels to identify what data is where.
You can think of a channel as a pipe inbetween two nodes. Each channel is intended to have only one type of data going through it so other nodes
know what to expect at the end of each pipe. You can create a channel arbitrarily by simply calling [`SetChannelData(String, Object)`](/doc/api/Motio.NodeCore.Utils.DataFeed.html#Motio_NodeCore_Utils_DataFeed_SetChannelData_System_String_System_Object_), but some channels
are already created by default. 

The main channel that interrests you is the [`Node.MESH_CHANNEL`](/doc/api/Motio.NodeCore.Node.html#Motio_NodeCore_Node_MESH_CHANNEL).
As it's name implies it is used to transfert the mesh from node to node, and the data from this channel will be used be the render engine
to create the shape the user sees.

I invite you to go to the [Examples](/doc/examples) section of your language of choice to get started. 