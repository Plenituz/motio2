from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import VectorNodeProperty, FloatNodeProperty, ColorNodeProperty, DropdownNodeProperty
from Motio.NodeImpl.NodePropertyTypes import IntNodeProperty, SeparatorNodeProperty
from Motio.Meshing import MotioShape, MotioShapeGroup
from Motio.Geometry import Vector2, Vertex
from Motio.NodeCore import Node
from Motio.Graphics import Color
from Motio.NodeImpl.NodeTools import MoveTool

class Line(BaseClass):
    classNameStatic = "Line"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_node(self):
        self.PassiveTools.Add(MoveTool(self,"pos1")) #add move gizmo
        self.PassiveTools.Add(MoveTool(self,"pos2")) #add move gizmo

    def setup_properties(self):
        #position point 1
        pos1Prop = VectorNodeProperty(self, "Start point position", "Start position")
        self.Properties.Add("pos1", pos1Prop, Vector2(0, 2))
        #position point 2
        pos2Prop = VectorNodeProperty(self, "End point position", "End position")
        self.Properties.Add("pos2", pos2Prop, Vector2(0, -2))
        #thickness
        thicknessProp = FloatNodeProperty(self, "Thickness of the line", "Thickness")
        thicknessProp.RangeFrom = 0
        thicknessProp.RangeTo = 15
        self.Properties.Add("thickness", thicknessProp, 0.35)
        self.Properties.AddManually('separator1', SeparatorNodeProperty(self))
        #color
        colorProp = ColorNodeProperty(self, "Color of the object", "Color")
        self.Properties.Add("color", colorProp, Color.DodgerBlue)
        #zindex
        zIndexProp = IntNodeProperty(self, "Order in the stack of layer", "Z Index")
        self.Properties.Add("zIndex", zIndexProp, 0)
        #action
        actionProp = DropdownNodeProperty(self, "Choose what to do with existing shapes", "Action", ["Replace", "Merge"])
        self.Properties.Add("action", actionProp, "Merge")

    def evaluate_frame(self, frame, dataFeed):
        # properties
        self.Properties.WaitForProperty("action")
        thickness = self.Properties.GetValue("thickness", frame)
        pos1 = self.Properties.GetValue("pos1", frame)
        pos2 = self.Properties.GetValue("pos2", frame)
        color = self.Properties.GetValue("color", frame).ToVector4()
        action = self.Properties.GetValue("action",frame)
        zIndex = self.Properties.GetValue("zIndex",frame)

        # mesh gen
        vertices = self.createLine(pos1, pos2, thickness, color)
        
        # output
        shape = MotioShape()
        shape.vertices = vertices
        shape.UpdateNormalsGeneration()
        shape.zIndex = zIndex

        shapeGroupInput = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)
        if action == "Merge" and shapeGroupInput:
            shapeGroupInput.Add(shape)
            shapeGroup = shapeGroupInput
        else:
            shapeGroup = MotioShapeGroup(shape)

        dataFeed.SetChannelData(Node.POLYGON_CHANNEL, shapeGroup)

    def createLine(self, pos1, pos2, thickness, color):
        #calculate normal vector in both ways
        normal1 = Vector2(pos2.Y-pos1.Y,pos1.X-pos2.X)
        normal1.Normalize()
        normal1 *= thickness/2
        normal2 = - normal1
        
        # points
        vertices = [
            Vertex(pos1 + normal2, color),
            Vertex(pos1 + normal1, color),
            Vertex(pos2 + normal1, color),
            Vertex(pos2 + normal2, color)
        ]

        #normals
        vertices[0].SetNormal(Vector2.Normalize((pos2-pos1) + normal1))
        vertices[0].SetNormal(Vector2.Normalize((pos2-pos1) + normal2))
        vertices[0].SetNormal(Vector2.Normalize((pos1-pos2) + normal1))
        vertices[0].SetNormal(Vector2.Normalize((pos1-pos2) + normal2))

        return vertices