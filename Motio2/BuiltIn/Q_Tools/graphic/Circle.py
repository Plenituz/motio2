from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import VectorNodeProperty, FloatNodeProperty, IntNodeProperty, ColorNodeProperty
from Motio.NodeImpl.NodePropertyTypes import DropdownNodeProperty, SeparatorNodeProperty
from Motio.Meshing import MotioShape, MotioShapeGroup
from Motio.Geometry import Vector2, Vertex
from Motio.NodeCore import Node
from Motio.Graphics import Color
from Motio.NodeImpl.NodeTools import MoveTool
from math import pi, cos, sin

class Circle(BaseClass):
    classNameStatic = "Circle"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_node(self):
        self.PassiveTools.Add(MoveTool(self,"pos")) #add move gizmo

    def setup_properties(self):
        #position
        posProp = VectorNodeProperty(self, "Position of the circle's center", "Position")
        self.Properties.Add("pos", posProp, Vector2(0, 0))
        #radius
        radiusProp = VectorNodeProperty(self, "Circle's radius (non uniform will result in ellipse)", "Radius")
        radiusProp.uniform = True
        self.Properties.Add("radius", radiusProp, Vector2(5, 5))
        #detail
        detailProp = IntNodeProperty(self, "Smoothness of the circle", "Detail")
        detailProp.SetRangeFrom(3,True)
        detailProp.SetRangeTo(128,False)
        self.Properties.Add("detail", detailProp, 12)
        #crop
        cropStartProp = FloatNodeProperty(self, "Crop circle from this angle", "Crop start (degree)")
        cropStartProp.SetRangeFrom(0,True)
        cropStartProp.SetRangeTo(360,True)
        self.Properties.Add("cropStart", cropStartProp, 0)
        cropEndProp = FloatNodeProperty(self, "Crop circle to this angle", "Crop end (degree)")
        cropEndProp.SetRangeFrom(0,True)
        cropEndProp.SetRangeTo(360,True)
        self.Properties.Add("cropEnd", cropEndProp, 360)
        #cropOffset
        cropOffsetProp = FloatNodeProperty(self, "Offset of the crop angles", "Crop offset")
        cropOffsetProp.SetRangeFrom(0, False)
        cropOffsetProp.SetRangeTo(360, False)
        self.Properties.Add("cropOffset", cropOffsetProp, 0)

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
        pos = self.Properties.GetValue("pos", frame)
        radius = self.Properties.GetValue("radius", frame)
        detail = self.Properties.GetValue("detail", frame)
        color = self.Properties.GetValue("color", frame).ToVector4()
        action = self.Properties.GetValue("action",frame)
        zIndex = self.Properties.GetValue("zIndex",frame)
        cropOffset = self.Properties.GetValue("cropOffset", frame) * (pi/180)

        # check and conversion for cropping
        cropStart = self.Properties.GetValue("cropStart",frame)
        cropEnd = self.Properties.GetValue("cropEnd",frame)
        if cropStart == 0 and cropEnd == 360 :
            cropping = False
        else:
            cropping = True
            if cropStart == cropEnd:
                return
            if cropStart > cropEnd:
                cropStart, cropEnd = cropEnd, cropStart
            cropStart *= pi/180
            cropStart += cropOffset
            cropEnd *= pi/180
            cropEnd += cropOffset

        # mesh gen
        vertices = self.createCircle(pos, radius, detail, color, cropOffset, cropStart, cropEnd, cropping)

        # output
        shape = MotioShape()
        shape.vertices = vertices
        shape.UpdateNormalsGeneration()
        shape.zIndex = zIndex


        motioShapeInput = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)
        if action == "Merge" and motioShapeInput:
            motioShapeInput.Add(shape)
            shapeGroup = motioShapeInput
        else:
            shapeGroup = MotioShapeGroup(shape)

        dataFeed.SetChannelData(Node.POLYGON_CHANNEL, shapeGroup)

    def createCircle(self, pos, radius, detail, color, cropOffset, cropStart=None, cropEnd=None, cropping=False):
        # POINTS
        vertices = []
        circleDivision = 2*pi/detail
        for i in range(detail):
            pointAngle = (circleDivision * i) + cropOffset
            # don't add vertex if outside cropping zone
            if cropping and not cropStart<pointAngle<cropEnd:
                continue
            vertices.append(self.vertexOnCircle(pos, radius, pointAngle, color))

        # real croping position vertex
        if cropping:
            vertices.insert(0, Vertex(pos, color))
            vertices.insert(1, self.vertexOnCircle(pos, radius, cropStart, color))
            vertices.append(self.vertexOnCircle(pos, radius, cropEnd, color))

        # NORMALS
        for i in range(len(vertices)):
            vertices[i].SetNormal(Vector2.Normalize(vertices[i].position-pos))


        return vertices

    def vertexOnCircle(self, center, radius, angle, color=None):
        pos = Vector2(
            center.X + radius.X * cos(angle),
            center.Y + radius.Y * sin(angle)
        )
        if color:
            return Vertex(pos, color)
        else:
            return Vertex(pos)