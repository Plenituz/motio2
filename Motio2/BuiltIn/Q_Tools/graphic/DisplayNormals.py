from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase
from Motio.NodeImpl.NodePropertyTypes import FloatNodeProperty, ColorNodeProperty
from Motio.Meshing import StrokeTracerShape, MotioShape
from Motio.Geometry import Vector2
from Motio.NodeCore import Node
from Motio.Graphics import Color
from Q_Tools.Q_Helper import colorShape

class DisplayNormals(PyGraphicsAffectingNodeBase):
    classNameStatic = "Display normals"
    def __new__(cls, *args):
        return PyGraphicsAffectingNodeBase.__new__(cls, *args)

    def setup_properties(self):
        #thickness
        thicknessProp = FloatNodeProperty(self, "Thickness of the normals", "Thickness")
        thicknessProp.SetRangeFrom(0.01, True)
        thicknessProp.SetRangeTo(1, False)
        self.Properties.Add("thickness", thicknessProp, 0.07)
        #length
        lengthProp = FloatNodeProperty(self, "Length of the normals", "Length")
        lengthProp.RangeFrom = 0
        lengthProp.RangeTo = 25
        self.Properties.Add("length", lengthProp, 2)
        #color
        colorProp = ColorNodeProperty(self, "Color of the normals", "Color")
        self.Properties.Add("color", colorProp, Color.Fuchsia)

    def evaluate_frame(self, frame, dataFeed):
        # input check
        if not dataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            return
        shapeGroup = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)

        # properties
        self.Properties.WaitForProperty("color")
        thickness = self.Properties.GetValue("thickness", frame)
        length = self.Properties.GetValue("length", frame)
        color = self.Properties.GetValue("color", frame).ToVector4()

        # mesh gen
        originalShapes = shapeGroup.Clone()
        for shape in originalShapes:
            self.addNormalMeshToMeshgroup(shape, shapeGroup, thickness, length, color)


    def addNormalMeshToMeshgroup(self, shape, shapeGroup, thickness, length, color):
        for hole in shape.holes:
            self.addNormalMeshToMeshgroup(hole, shapeGroup, thickness, length, color)

        for i in range(len(shape.vertices)):
            n = shape.vertices[i].normal
            if n == Vector2.Zero:
                continue
            p = shape.vertices[i].position

            shape1, shape2 = self.normalToShape(p, n*length, thickness, length)
            colorShape(shape1, color)
            colorShape(shape2, color)
            shapeGroup.Add(shape1)
            shapeGroup.Add(shape2)

    def normalToShape(self, point, normal, thickness, length):
        stroke = StrokeTracerShape()
        stroke.Stroke([point, normal + point], [thickness]*2, StrokeTracerShape.EndCap.Round, False)
        shape1 = stroke.MotioShape
        perpendicular = Vector2(normal.X, normal.Y)
        perpendicular.Rotate90Deg()
        arrow1 = perpendicular - normal
        arrow1.Normalize()
        arrow2 = - perpendicular - normal
        arrow2.Normalize()
        stroke = StrokeTracerShape()
        stroke.Stroke([arrow1*(length/4) + point + normal, point + normal, arrow2*(length/4) + point + normal], [thickness]*3, StrokeTracerShape.EndCap.Square, False)
        shape2 = stroke.MotioShape
        return shape1, shape2