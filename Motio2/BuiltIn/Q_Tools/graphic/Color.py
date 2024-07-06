from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import DropdownNodeProperty, ColorNodeProperty, FloatNodeProperty
from Motio.NodeCore import Node
from Motio.Graphics import Color as MotioColor
from Q_Tools import Q_Helper
from System import Random

class Color(BaseClass):
    classNameStatic = "Color"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_properties(self):
        #type
        typeProp = DropdownNodeProperty(self, "Choose how to apply the color", "Type", ["Constant", "Random (uniform)", "Random (per shape)", "Random (per vertex)"])
        self.Properties.Add("type", typeProp, "Constant")
        #color
        colorProp = ColorNodeProperty(self, "Color of the object", "Color")
        self.Properties.Add("color", colorProp, MotioColor.Red)
        #seed
        seedProp = FloatNodeProperty(self, "Seed for random", "Seed")
        seedProp.RangeFrom = 0
        seedProp.RangeTo = 20
        self.Properties.Add("seed", seedProp, 1)

    def evaluate_frame(self, frame, dataFeed):
        # check input
        if not dataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            return
        shapeGroup = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)

        #properties
        self.Properties.WaitForProperty("seed")
        colorType = self.Properties.GetValue("type", frame)
        seed = self.Properties.GetValue("seed", frame)

        # color gen
        if colorType == "Constant":
            inputColor = self.Properties.GetValue("color", frame).ToVector4()
            for shape in shapeGroup:
                self.applyConstantColor(shape, inputColor)

        elif colorType == "Random (uniform)":
            randObject = Random(seed)
            constantRandomColor = Q_Helper.random_color(seed, randObject)
            for shape in shapeGroup:
                self.applyConstantColor(shape, constantRandomColor)

        elif colorType == "Random (per shape)":
            for i in range(shapeGroup.Count):
                randomColor = Q_Helper.random_color((seed + 1) * float(i) + (seed + 2))
                self.applyConstantColor(shapeGroup[i], randomColor)

        else:
            for i in range(shapeGroup.Count):
                self.applyRandomVertexColor(shapeGroup[i], seed * float(i+1))


    def applyConstantColor(self, shape, inputColor):
        for hole in shape.holes:
            self.applyConstantColor(hole, inputColor)

        for i in range(len(shape.vertices)):
            self.setVertexColor(shape.vertices, i, inputColor)

    def applyRandomVertexColor(self, shape, seed):
        for i in range(shape.holes.Count):
            self.applyRandomVertexColor(shape.holes[i], seed * float(i+1))

        for i in range(len(shape.vertices)):
            self.setVertexColor(shape.vertices, i, Q_Helper.random_color((seed+1)*float(i+2)))

    def setVertexColor(self, vertices, vId, color):
        v = vertices[vId]
        v.SetColor(color)
        vertices[vId] = v