# Other View Models

In the last tutorial we saw how to create a ViewModel for our `NodeTool`. But sometimes it can be useful to have custom View Models for a node itself. That's what we will be doing in this tutorial.

The main usage of making a custom ViewModel is to be able to interact with UI specific properties of the node which are on other view models. For example you could listen to the `PropertyChanged` event on a property and change other property accordingly.

>[!note]
>Almost every object in Water Motion has a `PropertyChanged` event that get triggered automatically when any property changes.

## Setting up 

Create a folder for your project add `MyNode.py` and `MyNodeViewModel.py`:

MyNode.py
```python
from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
import Motio.Meshing as Meshing
import Motio.Geometry as Geo
from Motio.NodeCore import Node
import Motio.NodeImpl.NodePropertyTypes as Props

class HelloCircle(BaseClass):
    classNameStatic = "Hello Circle"
    def get_class_name(self):
        return HelloCircle.classNameStatic

    #this is necessary to call the BaseClass's constructor,
    #removing this will break all the things
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_node(self):
        print "Setting up"

    def setup_properties(self):
        sizeProp = Props.FloatNodeProperty(self, "size of the circle", "Size")
        self.Properties.Add("size", sizeProp, 5)
        posProp = Props.VectorNodeProperty(self, "position of the circle", "Position")
        self.Properties.Add("pos", posProp, Geo.Vector2(0, 0))

    def evaluate_frame(self, frame, dataFeed):
        builder = Meshing.MeshBuilder()
        
        center = self.Properties.GetValue("pos", frame)
        radius = self.Properties.GetValue("size", frame)
        divisions = 12
        builder.AddCircle(center, radius, divisions)
        
        mesh = builder.Mesh
        meshGroup = Meshing.MeshGroup(mesh)

        dataFeed.SetChannelData(Node.MESH_CHANNEL, meshGroup)
```

MyNodeViewModel.py
```python
import Motio.UI.ViewModels.GraphicsAffectingNodeViewModel as BaseClass

class HelloCircleViewModel(BaseClass):

    def get_UserGivenName(self):
        return "custom name in the ui layer"
```

2 things to note: 
 - The view model has to have the same name as his corresponding node-layer-object plus the *ViewModel suffix.
 - There is base classes for all the node-layer-objects and you need to extends from the right one depending on the node-layer-object. Here since `HelloCircle` is a `GraphicsAffectingNode`, we extend `GraphicsAffectingNodeViewModel`. For a `PropertyAffectingNode` it would be `PropertyAffectingNodeViewModel`

The `get_UserGivenName` method is not necessary here but we override it just to demonstrate what you could do with a view model. If you create a `HelloCircle` node and open it in the property panel you should see it's name say "custom name in the ui layer"

![screen23](/doc/images/screen23.png)