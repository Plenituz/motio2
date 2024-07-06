# Creating Property Affecting Nodes

The last 2 tutorials were centered around the creation of Graphics Affecting Nodes. Let's now take a look at how you can create a Property Affecting Node. The process is very similar to before so this tutorial is fairly short.

## Setting up

As before we need to create a python file in the `Addons` folder located where you installed Water Motion. To stay organised we will put it in it's own folder, so create a `mypropertynode` folder with `MyPropertyNode.py` inside it. 

Paste this code in `MyPropertyNode.py`:

```python
from Motio.NodeImpl.PropertyAffectingNodes import PyPropertyAffectingNodeBase as BaseClass
from System import Type
from Motio.NodeCore import Node
import System.Single as Single

class MyPropertyNode(BaseClass):
    classNameStatic = "My Property Node"
    def get_class_name(self):
        return MyPropertyNode.classNameStatic

    def get_accepted_property_types(self):
        #this nodes only goes on FloatNodeProperties
        return [Single.__clrtype__()]

    def setup_properties(self):
        print "adding all kind of properties to this node"

    def setup_node(self):
        print "node constructor"

    def evaluate_frame(self, frame, dataFeed):
        previousVal = 0
        #get the previous val if there is one
        if dataFeed.ChannelExists(Node.PROPERTY_OUT_CHANNEL):
            previousVal = dataFeed.GetChannelData(Node.PROPERTY_OUT_CHANNEL)

        dataFeed.SetChannelData(Node.PROPERTY_OUT_CHANNEL, previousVal * 2)
```

As before, this is the least amount of code necessary for your node to work. Most of it should look familiar so we will only look at what's different from a GraphicsAffectingNode. 

## Run Down

```python
from Motio.NodeImpl.PropertyAffectingNodes import PyPropertyAffectingNodeBase as BaseClass
```

Very important here we change the `BaseClass` to be `PyPropertyAffectingNodeBase`, this is the base class for all Property Affecting Nodes written in python. 

```python
def get_accepted_property_types(self):
    #this nodes only goes on FloatNodeProperties
    return [Single.__clrtype__()]
```

Has it's name implies this function should return an array of `Type` that this node can handle. Any type you list here may appear in the `Node.PROPERTY_OUT_CHANNEL`. Here we only accept floating point numbers.

>[!warning]
> You should return a C# type here, not a python type. As you can see here we use the special method `__clrtype__()` that returns the C# equivalent of a python type. 

>[!tip]
> Polymorphism is taken in account, so if you return `System.Object.__clrtype__()` you will accept any type of data. Obviously you should not accept any type but you can use this fact to your advantage.

```python
def evaluate_frame(self, frame, dataFeed):
    previousVal = 0
    #get the previous val if there is one
    if dataFeed.ChannelExists(Node.PROPERTY_OUT_CHANNEL):
        previousVal = dataFeed.GetChannelData(Node.PROPERTY_OUT_CHANNEL)

    dataFeed.SetChannelData(Node.PROPERTY_OUT_CHANNEL, previousVal * 2)
```

This method has the same signature as before but the content is very different. Since this node will be on a property, the `DataFeed` only has 1 standard channel, `Node.PROPERTY_OUT_CHANNEL`. In this channel you will find either the value a previous node created, or the static value of the property. Here since we blindly mulptiply by 2 the input value we have to make sure there is one using `dataFeed.ChannelExists`.

To test the node:
 - Restart Water Motion
 - Create a GraphicsNode
 - Add a Graphics Affecting Node that has a FloatProperty on it (Circle for example)
 - Add "My Property Node" to the FloatProperty

![screen19](/doc/images/screen19.png)

As you can see the node does it's job of multiplying by 2 the value it receives.

Property Affecting Nodes can themself have properties, to learn how to add properties to your node [see the previous tutorial](/doc/examples/python/adding_properties.html)

## Source Code

```python
from Motio.NodeImpl.PropertyAffectingNodes import PyPropertyAffectingNodeBase as BaseClass
from System import Type
from Motio.NodeCore import Node
import System.Single as Single

class MyPropertyNode(BaseClass):
    classNameStatic = "My Property Node"
    def get_class_name(self):
        return MyPropertyNode.classNameStatic

    def get_accepted_property_types(self):
        #this nodes only goes on FloatNodeProperties
        return [Single.__clrtype__()]

    def setup_properties(self):
        print "adding all kind of properties to this node"

    def setup_node(self):
        print "node constructor"

    def evaluate_frame(self, frame, dataFeed):
        previousVal = 0
        #get the previous val if there is one
        if dataFeed.ChannelExists(Node.PROPERTY_OUT_CHANNEL):
            previousVal = dataFeed.GetChannelData(Node.PROPERTY_OUT_CHANNEL)

        dataFeed.SetChannelData(Node.PROPERTY_OUT_CHANNEL, previousVal * 2)
```