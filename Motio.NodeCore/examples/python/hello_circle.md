# Hello Circle!

In this example we will see the basics of the creating a GraphicsAffectingNode in Python by displaying a simple circle.

You will need: 
 - Your text editor of choice, you can use the built in text editor but it kind of sucks.
 - Basic knowledge of OOP and Python
 - About 10 min or less 

## Setting up

First we need to create the file that where we will place our code, and we need to place it where Water Motion can find it.
Any python file found in `<root>\Addons` will be seen by Water Motion (where `<root>` is the directory containing your Water Motion installation).
But to stay organised we will create a directory for our project. 

So first, create a directory `hellocircle` in `<root>\Addons`. Then create a file named `HelloCircle.py` in `<root>\Addons\hellocircle`.
If the `Addons` directorie doesn't exist you can create it yourself.

Paste the following code in `HelloCircle.py`:

```python
from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass

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
        print "Creating properties"

    def evaluate_frame(self, frame, dataFeed):
        print "Evaluating frame %s" % frame
```

This is the minimal amount of code you need to get your node working. Make sure the file is in the right directory and restart Water Motion. 
If everything goes as planned nothing more than usual should happen. However if you have a compile error in your code Water Motio will tell you 
on startup.

>[!tip]
>The name of the file don't have to be the same as the name of the class

## Testing it

Create a GraphicsNode and click the "+" button to show the list of available GraphicsAffectingNodes, you should see "Hello Circle".

![screen13](/doc/images/screen13.png)

You should also be able to see the print messages in the console. To open the console press `CTRL`+`SHIFT`+`C`. In the image below the
first, third and last messages are from our HelloCircle node!

![screen14](/doc/images/screen14.png)

Let's go through the code and see what it does.

```python
from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass

class HelloCircle(BaseClass):
```

Here we import the `PyGraphicsAffectingNodeBase` class and rename it `BaseClass` to use it as a base class for our node. 
If you are making a GraphicsAffectingNode in Python you **MUST extend `PyGraphicsAffectingNodeBase`**. You **should NOT extend directly the `GraphicsAffectingNode` class**, that would confuse Water Motion when loading the node from a file and result in the save file being corrupted.

```python
classNameStatic = "Hello Circle"
def get_class_name(self):
    return HelloCircle.classNameStatic
```

Next we define a static variable named `classNameStatic`. The name is important as this is the variable Water Motion will look for when displaying the 
node name in the "+" dropdown menu. Just below we define a getter for the `ClassName` property, similarly to the `classNameStatic` this will be displayed 
to the user and it is recommended to return `classNameStatic` so the user doesn't see 2 different name for the name node in different places.

```python
#this is necessary to call the BaseClass's constructor,
#removing this will break all the things
def __new__(cls, *args):
    return BaseClass.__new__(cls, *args)
```

As the comment explains, this calls the base class's constructor and is absolutely necessary.

```python
def setup_node(self):
    print "Setting up"

def setup_properties(self):
    print "Creating properties"

def evaluate_frame(self, frame, dataFeed):
    print "Evaluating frame %s" % frame
```

Finally we see some familiar faces! Thoses are the 3 basic functions called by Water Motion as explained in the [introduction](/doc/articles/introdev.html).
As we saw in the console earlier this just prints when the events occur.

## The Circle

Let's modify this code to do something more interesting, like creating circle!
Add the following imports at the top of the file:

```python
import Motio.Meshing as Meshing
import Motio.Geometry as Geo
from Motio.NodeCore import Node
```

Next replace the old `EvaluateFrame` function by this one:

```python
def evaluate_frame(self, frame, dataFeed):
    builder = Meshing.MeshBuilder()
    
    center = Geo.Vector2(0, 0)
    radius = 5
    divisions = 12
    builder.AddCircle(center, radius, divisions)
    
    mesh = builder.Mesh
    meshGroup = Meshing.MeshGroup(mesh)

    dataFeed.SetChannelData(Node.MESH_CHANNEL, meshGroup)
```

>[!Note]
>We removed the `print` statement in `EvaluateFrame` because it would slow down the application drastically

Let's see how to compile our changes first, then we will see what each line does.

In the navigation bar, go to Edit>Text Editor. A window with a text editor appears, in this window's navigation bar go to File>Open and open your `HelloCircle.py`.
The content of your file appears in the text editor. To compile it click the "+" button in the navigation bar. It will re-compile the python file and update the node accordingly. You should now see the a familiar circle in the viewport.

![screen12](/doc/images/screen12.png)

>[!note]
>You can continue using your favorite text editor and just come back to Water Motion's editor just to click the "+" button to re-compile.
Don't worry if the content displayed in the built in editor is not up to date with your last changes, Water Motion re-reads the file from
scratch every time you press the "+"

>[!note]
>If you have any runtime error they will show up in the console (`CTRL`+`SHIFT`+`C`)

Let's take a look at the code.

```python
builder = Meshing.MeshBuilder()
```

`MeshBuilder` is a class we created to help you in the process of generating meshes. It does it's best to abstract the data structure of a mesh.

```python
center = Geo.Vector2(0, 0)
radius = 5
divisions = 12
builder.AddCircle(center, radius, divisions)
```

We create 3 variables and give them to the `AddCircle` method of the `MeshBuilder`. The variables are declared above for clarity but you can declare
them directly in the function call once you understand what each argument is. The `AddCircle` method will add a circle to the mesh, since this is a 3D
mesh we have to provide a number of division to approximate the circle.

```python
mesh = builder.Mesh
meshGroup = Meshing.MeshGroup(mesh)
```

Here we get the actual mesh from the `MeshBuilder` and put it into a `MeshGroup`. A `MeshGroup` is simply a collection of `Mesh`, and it's the only type
accepted in the `Node.MESH_CHANNEL`. Using `MeshGroup` instead of `Mesh` allows you to create completely different meshes but have the system treat them as a single mesh.

>[!warning]
>Calling `builder.Mesh` can be a computer intensive process, depending on the mesh you are trying to create. Make sure to call it only once and store the result in a variable

```python
dataFeed.SetChannelData(Node.MESH_CHANNEL, meshGroup)
```

Last but not least we put the `MeshGroup` in the appropriate channel of the `dataFeed`, so other nodes can do their job. You can add a `Transform` node after your HelloCircle node and it will behave as if your node is one of the native ones.

You now know the basics of how to create a node, head to the [next example](/doc/examples/python/adding_properties.html) to learn how to add properties to your node

## Source Code

```python
from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
import Motio.Meshing as Meshing
import Motio.Geometry as Geo
from Motio.NodeCore import Node

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
        print "Creating properties"

    def evaluate_frame(self, frame, dataFeed):
        builder = Meshing.MeshBuilder()
        
        center = Geo.Vector2(5, 0)
        radius = 5
        divisions = 12
        builder.AddCircle(center, radius, divisions)
        
        mesh = builder.Mesh
        meshGroup = Meshing.MeshGroup(mesh)

        dataFeed.SetChannelData(Node.MESH_CHANNEL, meshGroup)
```