from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import VectorNodeProperty, FloatNodeProperty, IntNodeProperty, ColorNodeProperty
from Motio.NodeImpl.NodePropertyTypes import DropdownNodeProperty, SeparatorNodeProperty
from Motio.Meshing import MotioShape, MotioShapeGroup
from Motio.Geometry import Vector2, Vertex
from Motio.Graphics import Color
from Motio.NodeCore import Node
from Motio.NodeImpl.NodeTools import MoveTool
from math import sin, cos, pi, atan2

class Capsule(BaseClass):
    classNameStatic = "Capsule"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_node(self):
        self.PassiveTools.Add(MoveTool(self,"pos1")) #add move gizmo
        self.PassiveTools.Add(MoveTool(self,"pos2")) #add move gizmo

    def setup_properties(self):
        #position point 1
        pos1Prop = VectorNodeProperty(self, "First center position", "Position 1")
        self.Properties.Add("pos1", pos1Prop, Vector2(0, 2))
        #position point 2
        pos2Prop = VectorNodeProperty(self, "Second center position", "Position 2")
        self.Properties.Add("pos2", pos2Prop, Vector2(0, -2))
        #thickness
        thicknessProp = FloatNodeProperty(self, "Thickness of the capsule", "Thickness")
        thicknessProp.RangeFrom = 0
        thicknessProp.RangeTo = 50
        self.Properties.Add("thickness", thicknessProp, 2)
        #detail
        detailProp = IntNodeProperty(self, "Definition of the rounded edges", "Detail")
        detailProp.SetRangeFrom(1, True)
        detailProp.SetRangeTo(32, False)
        self.Properties.Add("detail", detailProp, 6)
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
        pos1 = self.Properties.GetValue("pos1", frame)
        pos2 = self.Properties.GetValue("pos2", frame)
        thickness = self.Properties.GetValue("thickness", frame)
        detail = self.Properties.GetValue("detail", frame)
        color = self.Properties.GetValue("color", frame).ToVector4()
        action = self.Properties.GetValue("action",frame)
        zIndex = self.Properties.GetValue("zIndex",frame)

        # mesh gen
        vertices = self.createCapsule(pos1, pos2, detail, thickness, color)
        
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

    def createCapsule(self, pos1, pos2, detail, thickness, color):
        normal = self.calculateNormal(pos1, pos2)
        normal *= thickness/2

        #angle between circle division
        divisionAngle = pi/detail

        #first point
        vertices = []
        firstV = Vertex(pos1 - normal, color)
        firstV.SetNormal(-normal)
        vertices.append(firstV)
        #first half circles
        for i in range(detail):
            positionOnCircle = Vector2(
                thickness/2*cos(divisionAngle*(i+1)+atan2(normal.Y, normal.X)+pi),
                thickness/2*sin(divisionAngle*(i+1)+atan2(normal.Y, normal.X)+pi)
            )
            vertices.append(Vertex(positionOnCircle+pos1, color))
            vertices[i+1].SetNormal(Vector2.Normalize(vertices[i+1].position - pos1))

        #second points
        secondV = Vertex(pos2 + normal, color)
        secondV.SetNormal(normal)
        vertices.append(secondV)
        #second half circle
        for i in range(detail):
            positionOnCircle = Vector2(
                thickness/2*cos(divisionAngle*(i+1)+atan2(normal.Y, normal.X)),
                thickness/2*sin(divisionAngle*(i+1)+atan2(normal.Y, normal.X))
            )
            vertices.append(Vertex(positionOnCircle+pos2, color))
            vertices[i + 2 + detail].SetNormal(Vector2.Normalize(vertices[i + 2 + detail].position - pos2))

        return vertices

    def calculateNormal(self, pos1, pos2):
        #calculate normal vector in both ways
        normal = Vector2(pos2.Y-pos1.Y,pos1.X-pos2.X)
        normal.Normalize()
        return normal