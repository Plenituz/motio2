from Motio.NodeCore import GraphicsNode
from Motio.NodeImpl import NodeScanner

timeline = timeline.Original
graphics = GraphicsNode(timeline)


#for node in NodeScanner.UserAccesibleGraphicsNodes:
#    if node.ClassNameStatic not in ["Clone", "Duplicate", "Stroke"]:
#        node.CreateInstance(graphics)

#circle = NodeScanner.GetByName("Clone").CreateInstance(graphics)
#circle.Properties["radius"].StaticValue = 25;
#NodeScanner.GetByName("Voxelize").CreateInstance(graphics)
