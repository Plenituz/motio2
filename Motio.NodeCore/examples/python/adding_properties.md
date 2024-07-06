# Adding Properties

Last tutorial we created a node that can draw a circle. Now let's give some properties to this node so the user can animate them. Go to the bottom to find the source for this example.

We will add 3 properties: Size, Position and Color. 

## Our First Property

Let's start by adding the Size then we'll do the rest. Add the following import at the top of the file:

```python
import Motio.NodeImpl.NodePropertyTypes as Props
```

There are several property types you can add, and they are all stored in `Motio.NodeImpl.NodePropertyTypes`. Replace the `setup_properties` function by this:

```python
def setup_properties(self):
    sizeProp = Props.FloatNodeProperty(self, "size of the circle", "Size")
    self.Properties.Add("size", sizeProp, 5)
```

Here we use the `FloatNodeProperty` which as it's name implies is a property of type `float`. This is important for 2 reasons: it determines what
kind of data we will get when getting the value of this property, and it changes the display in the property panel. Here is what a `FloatNodeProperty`
looks like in the property panel

![screen15](/doc/images/screen15.png)

Let's take a closer look at the code.

```python
sizeProp = Props.FloatNodeProperty(self, "size of the circle", "Size")
```

Here we create the NodeProperty object by giving it a reference to the node it will on, a description that will be shown to the user, and a name that will
be displayed to the user as well. Some NodeProperty types require more arguments, like the `DropDownNodeProperty` where you have to supply the list 
of items in the dropdown. 

```python
self.Properties.Add("size", sizeProp, 5)
```

Next we add the property to our node. The first argument is the unique name of this property amongst other properties on this node, here we chose `"size"`. The second argument is the NodeProperty object and the last argument is the default value for this property. 

>[!Note]
>The unique name of a property on a node can be completly different from the displayed name of the property, but more advanced users may be exposed
to the unique name as well. It is therefore better to choose a unique name similar to the displayed name. By convention we use the property name, all lower case and replace spaces by '_'.

>[!warning]
>The default value's type must be the same as the NodeProperty's type. For example if you add a VectorNodeProperty, the default value must be a Vector2.

Re-compile your node and open it in the property panel, you should see a `Size` property. You can hover over the name to see the description. 

![screen16](/doc/images/screen16.png)

We have a property but if you change it, nothing happens! That's because our algorithm in `evaluate_frame` doesn't use the value of this property. 
Change this line in `evaluate_frame`

```python
radius = 5
```

To this one

```python
radius = self.Properties.GetValue("size", frame)
```

We ask the PropertyGroup to give us the value of the property with the unique name `"size"` for the frame
number `frame`. Now re-compile again, you can animate the value of the Size property, the circle should react to it.

>[!tip]
>Calling `Properties.GetProperties` will evaluate all the PropertyAffectingNodes on the property. This means if you don't use a certain property
the PropertyAffectingNodes on it will not be evaluated, don't be surprised!

## The Vector NodeProperty

Let's add the Position property. Add this to the `setup_properties` method:

```python
posProp = Props.VectorNodeProperty(self, "position of the circle", "Position")
self.Properties.Add("pos", posProp, Geo.Vector2(0, 0))
```

This time we create a `VectorNodeProperty` so we have to give it a `Vector2` for the default value. We can then get the property value 
in `evaluate_frame` just like before, except this type the value we get is of type `Vector2`.

```python
center = self.Properties.GetValue("pos", frame)
```

Try changing the value in the property panel, the circle should move.

![screen17](/doc/images/screen17.png)

## Adding color

Let's add a color to our circle. The property creation is the same but the usage in `evaluate_node` is a little different. First we need to import
the `Motio.Graphics` namespace, it holds the `Color` class.

```python
import Motio.Graphics as Graphics
```

Then create the property in `setup_properties`

```python
colorProp = Props.ColorNodeProperty(self, "color of the circle", "Color")
self.Properties.Add("color", colorProp, Graphics.Color.Brown)
```

The Color class has a lot of predefined colors, here we choose brown as our default color. For the color to be displayed we need to modify the 
mesh's material. In `evaluate_frame` just below the `mesh = builder.Mesh` change the code to:

```python
mesh = builder.Mesh

if mesh.material is None:
    mesh.material = Graphics.MeshMaterial()
mesh.material.color = self.Properties.GetValue("color", frame)

meshGroup = Meshing.MeshGroup(mesh)
```

First we make sure the mesh has a material, you never know, previous nodes might have deleted the default material. Then simply assign the `color` field
of the material to the value of our property. Now you can re-compile and you should see your circle turning brown, what an achievement!

You can play around with the color, even add more another HelloCircle node on another GraphicsNode to create several circles.

![screen18](/doc/images/screen18.png)

There is a lot more property types you can use, [see the documentation](/doc/api/Motio.NodeImpl.NodePropertyTypes.html) to learn

## Source Code 

```python
from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
import Motio.Meshing as Meshing
import Motio.Geometry as Geo
from Motio.NodeCore import Node
import Motio.NodeImpl.NodePropertyTypes as Props
import Motio.Graphics as Graphics

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
        colorProp = Props.ColorNodeProperty(self, "color of the circle", "Color")
        self.Properties.Add("color", colorProp, Graphics.Color.Brown)

    def evaluate_frame(self, frame, dataFeed):
        builder = Meshing.MeshBuilder()
        
        center = self.Properties.GetValue("pos", frame)
        radius = self.Properties.GetValue("size", frame)
        divisions = 12
        builder.AddCircle(center, radius, divisions)
        
        mesh = builder.Mesh

        if mesh.material is None:
            mesh.material = Graphics.MeshMaterial()
        mesh.material.color = self.Properties.GetValue("color", frame)

        meshGroup = Meshing.MeshGroup(mesh)

        dataFeed.SetChannelData(Node.MESH_CHANNEL, meshGroup)
```