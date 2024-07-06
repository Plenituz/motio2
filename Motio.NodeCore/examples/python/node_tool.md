# Creating a NodeTool

In this tutorial we will learn how to create a NodeTool. A NodeTool is used to fill the hole between the viewport and the actual node. Here is what a NodeTool looks like:

![screen20](/doc/images/screen20.png)

## Why is this necessary ?

Water Motion is divided in 2 main layers: The node layer and the UI layer.<br/>
The node layer is what we worked with in the previous tutorials, it's responsible for generating the mesh that is then displayed by the UI layer in the viewport. This means the UI layer has access to the node layer but the inverse is not true. 

## View Models

Since the node is responsible for giving itself tools, a NodeTool is actually part of the node layer. So to fill the gap we use ViewModels. A ViewModel can be thought of as the mirror image of a node-layer-object but that lives in the UI layer. The ViewModel can have data specific to the node but that would make no sense in the node layer. For example the ViewModel of a node could store the color of a button on the node's representation in the property panel.

A NodeToolViewModel is the mirror image of a NodeTool that has access to the viewport events and it's underlying NodeTool in the node layer.

## Setting up

Create a new directory in the `Addons` folder named `viewModelExample`. In this directory create 3 python files: `MyNode.py`, `MyNodeTool.py` and `MyNodeToolViewModel.py`. For `MyNode.py` we will use similar code as in previous tutorials:

```python
#MyNode.py
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

Then we need to create the NodeTool that lives in the node layer. This class won't have any functionality in this tutorial, it just serves as a token class to tell the UI layer to create a ViewModel for it. Paste this in `MyNodeTool.py`.

```python
#MyNodeTool.py
import Motio.NodeCore as Core

class MyNodeTool(Core.NodeTool):
    pass
```

As you can see it's just an empty class that extends `Motio.NodeCore.NodeTool`. We then need to add the tool to the `Tools` of the node. To do so our node need to import the `MyNodeTool`, so we need to setup our project folder to be concidered a module by python.

Create `__init__.py` in `viewModelExample` and paste the following inside:

```python
__all__ = ["MyNodeTool"]
```

This is just to tell python what file are present in our module.<br/>
We can now import it in `MyNode.py`:

```python
from viewModelExample.MyNodeTool import MyNodeTool
```

And add it to the `Tools` list in `setup_node` of `MyNode.py`:

```python
def setup_node(self):
    print "Setting up"
    self.Tools.Add(MyNodeTool(self))
```

>[!warning]
>A NodeTools always take as argument a reference to the node hosting it in it's constructor

Now for the interesting part, paste this in `MyNodeToolViewModel.py`.

```python
import Motio.UI.ViewModels.NodeToolViewModel as BaseClass
from Motio.UI.Gizmos import Gizmo
import Motio.Geometry as Geo
from System.Windows.Media import Brushes

class MyNodeToolViewModel(BaseClass):

    def __new__(cls, *args):
        instance = BaseClass.__new__(cls, *args)
        return instance

    def OnHide(self):
        #always call super method in ViewModels
        super(MyNodeToolViewModel, self).OnHide()
        print "tool hidden, hidding gizmos..."

    def OnShow(self):
        #always call super method in ViewModels
        super(MyNodeToolViewModel, self).OnShow()
        print "tool shown, displaying gizmos..."

    def Delete(self):
        #always call super method in ViewModels
        super(MyNodeToolViewModel, self).Delete()
        print "tool deleted, unsubscribing from all events"

    def OnClickInViewport(self, ev, worldPos, canvasPos):
        #always call super method in ViewModels
        super(MyNodeToolViewModel, self).OnClickInViewport(ev, worldPos, canvasPos)
        print "user clicked on viewport while this tool was selected"
```

We created a `MyNodeToolViewModel` class that extends `Motio.UI.ViewModels.NodeToolViewModel`, inside of it we defined some method that will be called when certain events occur. To see all the callbacks you have access to, take a look at the `Addons\examples` folder.

>[!warning]
>To link the ViewModel to the actual node-layer-object they must have the exact same class name with the `*ViewModel` suffix for the ViewModel. So here since we create a ViewModel for the class `MyNodeTool` we have to name our class `MyNodeToolViewModel`

>[!warning]
>Make sure you call the super methods first thing in each method you override, otherwise some functionalities won't work properly

If you restart Water Motion now you will see your tool and events will be printed in the console when you interact with the viewport when the tool is enabled.

![screen20](/doc/images/screen20.png) ![screen21](/doc/images/screen21.png)

## Adding Gizmos

This is not much so let's add some functionality. As an example we will add a circle wherever the user clicks in the viewport. To do so we will use the `OnClickInViewport` method which provides us directly with the position on the viewport where we should add our circle in `canvasPos`. Change `OnClickInViewport` to the following:

```python
def OnClickInViewport(self, ev, worldPos, canvasPos):
    super(MyNodeToolViewModel, self).OnClickInViewport(ev, worldPos, canvasPos)
    print "user clicked on viewport while this tool was selected"
    #Gizmo.AddCircle add the gizmo to the canvas and returns it 
    radius = 50
    ellipse = Gizmo.AddCircle(canvasPos, radius, Brushes.Brown, False)
```

As you can see to display an item on the viewport is made very simple using the `Gizmo` class. Here we call the helper method `AddCircle` that creates, adds and returns a circle of radius `50` for us. You can also create any [WPF UIElement](https://msdn.microsoft.com/fr-fr/library/aa970268(v=vs.100).aspx) and pass it to `Gizmo.Add`. Check out [this page on drawing shapes](https://docs.microsoft.com/en-us/dotnet/framework/wpf/graphics-multimedia/shapes-and-basic-drawing-in-wpf-overview) with WPF from the official documentation. 

You can now select your tool and click in the viewport a circle should appear each time.

![screen22](/doc/images/screen22.png)

Currently our tool displays circles when we click but doesn't react to the user deleting the node, or hiding the node from the property panel. We need to remove our gizmos from the screen when thoses events occur. 

First we need to keep track of what we added otherwise we won't know what to remove. Let's add an array to our object in the constructor. And then each time we add a circle we will also add it to this array.

>[!warning]
>In IronPython you should use `__new__` instead of `__init__` as your constructor

Change `__new__` to:

```python
def __new__(cls, *args):
    instance = BaseClass.__new__(cls, *args)
    instance.gizmos = []
    return instance
```

And `OnClickInViewport`:

```python
def OnClickInViewport(self, ev, worldPos, canvasPos):
    #always call super method in ViewModels
    super(MyNodeToolViewModel, self).OnClickInViewport(ev, worldPos, canvasPos)
    print "user clicked on viewport while this tool was selected"
    radius = 50
    ellipse = Gizmo.AddCircle(canvasPos, radius, Brushes.Brown, False)
    #to remove the gizmos OnHide we need to keep track of them
    self.gizmos.append(ellipse)
```

Now that we know what we added to the screen we need to remove it when the tool is hidden or deleted. Change `OnHide` to:

```python
def OnHide(self):
    super(MyNodeToolViewModel, self).OnHide()
    print "tool hidden, hidding gizmos..."
    for gizmo in self.gizmos:
        Gizmo.Remove(gizmo)
```

And `OnDelete`:

```python
def Delete(self):
    #always call super method in ViewModels
    super(MyNodeToolViewModel, self).Delete()
    print "tool deleted, unsubscribing from all events"
    self.OnHide()
    self.gizmos = None
```

In `Delete` we have to make sure this instance of NodeToolViewModel is not held by any event or object. Here we don't have any but you need to keep this in mind if you subscribe to any event. 

You can test everything is working by selecting the tool, clicking in the viewport and then deleting the node. The gizmos should disapear. 

Right now if the tool is hidden (deleted from the property panel for example) and re-shown the gizmos stay hidden. Let's change the `OnShow` to:

```python
def OnShow(self):
    #always call super method in ViewModels
    super(MyNodeToolViewModel, self).OnShow()
    print "tool shown, displaying gizmos..."
    for gizmo in self.gizmos:
        Gizmo.Add(gizmo)
```

This works but now we have another problem: when you zoom in on the viewport, the circles get bigger too. This is not the behaviour users are used to from a gizmos. Usually a gizmo is the same size on screen, whatever the zoom on the viewport. Fortunately this is really easy to change.

We need to subscribe to the `ContentScaleChanged` event of the `RenderView`. To easily find components of the UI Water Motion has a `MainViewModel`, which holds a reference to the `RenderView`. You can get the `MainViewModel` using the extension method `FindMainViewModel()` for `NodeViewModel` located in `Motio.UI.Utils`. 

>What's an extension method ?<br/>
>An extension method is a feature from the C# language. It allows you to create a method that will be added to a class as if it was actually declared in the class body. Here when we add a method to the `NodeViewModel` class that will return the `MainViewModel`.

Well that's a lot of words but what does it mean in practice ?

First let's import the extension method from the namespace, this is IronPython specific syntax:

```python
import clr
import Motio.UI.Utils
clr.ImportExtensions(Motio.UI.Utils)
```

Now we have access to the `FindMainViewModel()` method directly on all instances of `NodeViewModel`. Luckly our `NodeToolViewModel` hold a reference to it's host `NodeViewModel` under the field `_host`. So doing:

```python
self._host.FindMainViewModel()
```

Will get the instance of `MainViewModel`. we can now get the RenderView and subscribe to the event by adding this line to `OnShow`:

```python
self._host.FindMainViewModel().RenderView.ContentScaleChanged += self.content_scale_changed
```

This will make sure that the `content_scale_changed` function is called every time the zoom level changes in the viewport. But this method is not defined so let's add it:

```python
def content_scale_changed(self, scale):
    scale = 1/scale
    for gizmo in self.gizmos:
        gizmo.Width = scale*8
        gizmo.Height = scale*8
```

The callback takes a single argument `scale` that will be the scale of the viewport. Therefore for our circles to always stay the same size we need to multiply their size by the inverse of this scale. Here we reduced the size from 50 to 8 to compensate for the scale of the viewport. Since we subscribed to the event in `OnShow` we also need to unsubscribe in `OnHide`, we don't want our circle to change size while they are not visible. 

Add this in `OnHide`:

```python
self._host.FindMainViewModel().RenderView.ContentScaleChanged -= self.content_scale_changed
```

Since we changed the size of a circle from 50 to `scale*8` we need to make sure the circles we create are the right size. In `OnClickInViewport` change the line radius line:

```python
radius = 8*self._host.FindMainViewModel().RenderView.InverseContentScale
```

## Interacting with the node layer

Up to now our node only does UI specific things, but what if we want to control some property on the underlying node from the NodeToolViewModel? Any ViewModel in Water Motion also implement the interface `IProxy`. This means all the ViewModels have a member `Original` which is a reference to the node-layer-object this ViewModel is mirroring. In other words, to access the node under a `NodeViewModel` you can simply do `myNodeViewModel.Original`.

Let's make it so the Position property of our node changes to where we click in the viewport. In `OnClickInViewport` add:

```python
self._host.Original.Properties["pos"].StaticValue = Geo.Vector2(worldPos.X, worldPos.Y)
```

Let's deconstruct this line:
- `self._host` is a reference to the `NodeViewModel` holding this tool
- `self._host.Original` is a reference to the actual node. Here we know it's our `HelloCircle` node
- `self._host.Original.Properties["pos"].StaticValue` all the property types have a `StaticValue` member that represent the value before any node. We can set it directly to a `Vector2` here. 

As you can see here we use the `worldPos` argument instead of `canvasPos`. This is the position of the click transformed back to "mesh space". Indeed, `canvasPos` depends the render resolution, so by default it goes from 0 to 1080 on the Y axis for example. Whereas the meshes don't take in account the render resolution so the positions might just go from 0 to 100 for example in `worldSpace`.

Now when you click in the viewport a gizmo circle is added and the actual circle moves!

## Source code

\_\_init__.py
```python
__all__ = ["MyNodeTool"]
```

MyNode.py
```python
from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
import Motio.Meshing as Meshing
import Motio.Geometry as Geo
from Motio.NodeCore import Node
import Motio.NodeImpl.NodePropertyTypes as Props
from viewModelExample.MyNodeTool import MyNodeTool

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
        self.Tools.Add(MyNodeTool(self))

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

MyNodeTool.py
```python
#MyNodeTool.py
import Motio.NodeCore as Core

class MyNodeTool(Core.NodeTool):
    pass
```

MyNodeToolViewModel.py
```python
#MyNodeToolViewModel.py
import Motio.UI.ViewModels.NodeToolViewModel as BaseClass
from Motio.UI.Gizmos import Gizmo
import Motio.Geometry as Geo
from System.Windows.Media import Brushes

import clr
import Motio.UI.Utils
clr.ImportExtensions(Motio.UI.Utils)

class MyNodeToolViewModel(BaseClass):

    def __new__(cls, *args):
        instance = BaseClass.__new__(cls, *args)
        instance.gizmos = []
        return instance

    def OnHide(self):
        super(MyNodeToolViewModel, self).OnHide()
        self._host.FindMainViewModel().RenderView.ContentScaleChanged -= self.content_scale_changed
        print "tool hidden, hidding gizmos..."
        for gizmo in self.gizmos:
            Gizmo.Remove(gizmo)

    def OnShow(self):
        #always call super method in ViewModels
        super(MyNodeToolViewModel, self).OnShow()
        self._host.FindMainViewModel().RenderView.ContentScaleChanged += self.content_scale_changed
        print "tool shown, displaying gizmos..."
        for gizmo in self.gizmos:
            Gizmo.Add(gizmo)

    def Delete(self):
        #always call super method in ViewModels
        super(MyNodeToolViewModel, self).Delete()
        print "tool deleted, unsubscribing from all events"
        self.OnHide()
        self.gizmos = None

    def OnClickInViewport(self, ev, worldPos, canvasPos):
        #always call super method in ViewModels
        super(MyNodeToolViewModel, self).OnClickInViewport(ev, worldPos, canvasPos)
        print "user clicked on viewport while this tool was selected"
        radius = 8*self._host.FindMainViewModel().RenderView.InverseContentScale
        ellipse = Gizmo.AddCircle(canvasPos, radius, Brushes.Brown, False)
        #to remove the gizmos OnHide we need to keep track of them
        self.gizmos.append(ellipse)
        self._host.Original.Properties["pos"].StaticValue = Geo.Vector2(worldPos.X, worldPos.Y)

    def content_scale_changed(self, scale):
        scale = 1/scale
        for gizmo in self.gizmos:
            gizmo.Width = scale*8
            gizmo.Height = scale*8
```