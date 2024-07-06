# **Introduction**

In this article I will explain the basics of how to use Water Motion as a software. We will get to the developement aspect in the next articles.

## The interface

![screen1](/doc/images/screen1.png)

Here is what the interface looks like, it doesn't look good because we are focusing on functionnality first. 
Don't worry we will make it better before the alpha release.

The screen is divided in 4 sections described below:

![screen_distrib](/doc/images/screen_distrib.png)

__Render view:__ This is where the images are rendered. You can zoom in and out with the mouse wheel and pan around by click and dragging with the middle mouse button.
you can also click and drag with the left mouse button to select gizmos if there is some.

__Property panel:__ This is where you can edit the properties of all the nodes you have created. Double click on a node to push it to the top of the property panel.
More details on nodes below.

__Keyframe panel:__ This is where you will be able to edit keyframes just like you're used to. The keyframe panel is coupled with the curve view, if you select a keyframe on the keyframe panel it's curve is displayed in the curve view.

__Curve view:__ This is where you can modifie the interpolation curve of your keyframes.

## Getting started

In Water Motion you create animations by plugging nodes together. Each node has it's own job and doesn't know what node comes before or after it.
What it can see is the result of the preceding nodes. 

Water Motion has 3 types of nodes:
 - The *"Graphics Node"* is like a canvas, it's only job is to hold other nodes and connect them together.
 - The *"Graphics Affecting Node"* is the type of node you will use the most, as it's name implies it goes on a Graphics Node. 
It's this kind of node that will create the images and animations.
 - The *"Property Affecting Node"* is like a Graphics Affecting Node but it goes on a property.

Let's try to create a simple animation of a circle moving from left to right.

### Creating nodes

First we need a Graphics Node. Currently to create one you need to go to the "Edit" menu in the navigation bar and choose "New Node". 
This will change in the futur once the clip view is finished.

![screen2](/doc/images/screen2.png)

The newly created node is automatically displayed in the property panel to the right. We can then add a Graphics Affecting Node by clicking on the "+" button
next to the Graphics Node's name.

![screen3](/doc/images/screen3.png)

A list pops up listing all the nodes you can add. You can type in the search box at the top to find a node if you already know it's name. As you can see not a lot of nodes are available, that why this is a developer preview. 

For this example we will add a `Circle` node which job is to draw a simple circle. Click on the name `Circle` or select it using the arrows and press enter. 
The `Circle` node gets added to the list of nodes under the Graphics Node, and we can already see a circle drawn in the render view.

![screen4](/doc/images/screen4.png)

Let's change some parameters to adjust the look of our circle. Double click on the node to open it in the property panel.

![screen5](/doc/images/screen5.png)

We can see that this node has 2 parameters: `size` and `details`. <br/>
You can change the values for each parameter by either click and dragging the number to the right of the name or by clicking it and typing the desired value. 

>[!Note]
> When you click on the value to type a precise value you can also use mathematical expressions like `5+6*2`. 
You can also refer to the currently set value by typing `value`. So typing `value+1` will increase the value of the property by one. 

### Animating

Let's animate the size of the circle so it grows overtime. 

Properties that can be animated have a check box to the left of their name. This checkbox determines weither 
or not this property is displayed in the keyframe panel. This allows you to only see what you are currently working on, 
that way you can really focus and work fast. So let's check the box on the `size` property.

![screen6](/doc/images/screen6.png)

Nothing happened on the screen yet, we just told Water Motion "when you display this node in the keyframe panel, display this property". 
Now we need to tell it to actually display the node in the keyframe panel. To do so just click on the "+" button to the left of the 
Node's name, "Circle" here. 

![screen7](/doc/images/screen7.png)

A row appeared in the keyframe panel, and a keyframe has been created where the time cursor (the red vertical line) was placed in the timeline. 
To add another keyframe, move the cursor further in time by click and dragging in the plum colored area, then increase the value of 
the `size` property in the property panel. A new keyframe will be automatically added under the cursor. You can now move the cursor back to 
the first keyframe and press space to play your animation.

![screen8](/doc/images/screen8.png)

>**The interface for animating is a bit rough for now, a lot is missing, remember this release is meant for developers, not animators yet.**

Let's finish our animation by adding a `Transform` node to move our circle.

![screen9](/doc/images/screen9.png)

![screen10](/doc/images/screen10.png)

Place the time cursor at the first keyframe of the size animation and click on the checkbox to the left of the `offset` property 
and display it in the keyframe panel by clicking the "+" on the node. The keyframe panel gets populated with another node, but this time 
there is 2 keyframes above one another. That's because the `offset` property is a point, and a point has 2 components: x and y. 
You can edit keyframes on both components individually just like before.

![screen11](/doc/images/screen11.png)

Move the time cursor to the end of the animation and increase the `X` property. A new keyframe appears like before and now when you press space
the circle grows and moves to the right.

![render1](/doc/images/render1.gif)

### Exporting

Right now your only option to export is as a PNG image sequence. We plan on supporting export to various formats like SVG, mp4, mov and many others, 
but for now you will have to use an external tool to stitch the images into a video. We recommend ffmpeg or any video editing sofware.

To export simply go to File>Export to Image Sequence, choose the start and end frames, the path and click "Render"

### Behind the curtains

If you increase the size of the circle you will quickly see that the circle is not smooth.

![screen12](/doc/images/screen12.png)

That's because Water Motion actually uses 3D meshes behind the scene. This means a lot for developers but also for animators. 
Working with meshes opens a world a possibility for motion designers, like procedural animations for example. But it also closes a lot of doors compared to 
the pixel manipulations you might be used to, for example you can't blur a shape in Water Motio. We think that motion design is a lot faster and easier 
using meshes, that's why we built Water Motion. But we also know you sometimes need to use pixel manipulations, therefore we plan to plug directly into
Adobe After Effects, just like Cinema 4D does. Soon you will be able to animate in Water Motion and refine the look in Adobe After Effects seemlessly.

Water Motion and PleniCorp is not linked to Adobe or Maxon in any way.

### The end 

Thanks for reading this article! you should now have the basics to find your way around Water Motion. If you are a developer or a curious person, continue to 
[the next article](/doc/articles/introdev.html) to learn how to build your own node it's super easy!

plug directly into after effect
but also closes a buch of things no blur etc