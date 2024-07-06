from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import FloatNodeProperty, ColorNodeProperty, DropdownNodeProperty, IntNodeProperty, BoolNodeProperty
from Motio.Meshing import MotioShapeGroup, StrokeTracerShape, MotioShape
from Motio.NodeCore import Node
from Motio.Graphics import Color
from Q_Tools.Q_Helper import colorShape

class Border(BaseClass):
    classNameStatic = "Border"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_properties(self):
        #thickness
        thicknessProp = FloatNodeProperty(self, "Thickness of the border", "Thickness")
        thicknessProp.RangeFrom = 0
        thicknessProp.RangeTo = 10
        self.Properties.Add("thickness", thicknessProp, 0.2)
        #color
        colorProp = ColorNodeProperty(self, "Color of the border", "Color")
        self.Properties.Add("color", colorProp, Color.Blue)
        #alignment
        # alignmentProp = DropdownNodeProperty(self, "How to place the line around the shape", "Alignment", ["Inside", "Middle", "Outside"])
        # self.Properties.Add("alignment", alignmentProp, "Middle")
        #zindex
        zIndexProp = IntNodeProperty(self, "Order in the stack of layer", "Z Index")
        self.Properties.Add("zIndex", zIndexProp, 0)
        #keepShape
        keepShapeProp = BoolNodeProperty(self, "Add the border to the original shape", "Keep original shape")
        self.Properties.Add("keepShape", keepShapeProp, True)

    def evaluate_frame(self, frame, dataFeed):
        # input check
        if not dataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            return
        shapesInput = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)
    
        # properties
        self.Properties.WaitForProperty("keepShape")
        thickness = self.Properties.GetValue("thickness", frame)
        borderColor = self.Properties.GetValue("color", frame).ToVector4()
        keepShape = self.Properties.GetValue("keepShape", frame)
        zIndex = self.Properties.GetValue("zIndex", frame)

        if keepShape:
            shapeGroup = shapesInput
        else:
            shapeGroup = MotioShapeGroup()

        # mesh gen
        newShapes = []
        for shape in shapesInput:
            self.createBorder(shape, thickness, newShapes)

        for strokeShape in newShapes:
            strokeShape.zIndex = zIndex
            colorShape(strokeShape, borderColor)
            shapeGroup.Add(strokeShape)

        # output
        dataFeed.SetChannelData(Node.POLYGON_CHANNEL, shapeGroup)

    def createBorder(self, shape, thickness, newShapeList):
        for hole in shape.holes:
            self.createBorder(hole, thickness, newShapeList)

        stroke = StrokeTracerShape()
        shapePos = [v.position for v in shape.vertices]
        shapeThickness = [thickness] * shape.vertices.Count
        stroke.Stroke(shapePos, shapeThickness, StrokeTracerShape.EndCap.Square, True)

        newShapeList.append(stroke.MotioShape)
