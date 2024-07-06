from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase
from Motio.NodeImpl.NodePropertyTypes import FloatNodeProperty, BoolNodeProperty
from Motio.NodeCore import Node

class Dilate(PyGraphicsAffectingNodeBase):
    classNameStatic = "Dilate deformer"
    def __new__(cls, *args):
        return PyGraphicsAffectingNodeBase.__new__(cls, *args)

    def setup_properties(self):
        dilateProp = FloatNodeProperty(self, "Expansion of the points", "Dilate")
        dilateProp.RangeFrom = -20
        dilateProp.RangeTo = 20
        self.Properties.Add("dilate", dilateProp, 0)
        self.Properties.Add("forceCalculate", BoolNodeProperty(self,"Force calculate normals", "Force calculate normals"), False)
        # mix
        mixProp = FloatNodeProperty(self, "How much the deformer affect the shape", "Mix")
        mixProp.SetRangeFrom(0, True)
        mixProp.SetRangeTo(100, True)
        self.Properties.Add("mix", mixProp, 100)

    def evaluate_frame(self, frame, dataFeed):
        #check input
        if not dataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            return
        shapeGroup = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)

        # properties
        self.Properties.WaitForProperty("mix")
        mix = self.Properties.GetValue("mix", frame) / 100
        dilate = self.Properties.GetValue("dilate", frame)
        forceCalculate = self.Properties.GetValue("forceCalculate", frame)

        for shape in shapeGroup:
            if shape.ShouldCalculateNormals() or forceCalculate:
                shape.CalculateNormals()
            self.dilateshape(shape, dilate, mix)


    def dilateshape(self, shape, dilate, mix):
        for i in range(len(shape.vertices)):
            p = shape.vertices[i].position
            normal = shape.vertices[i].normal
            if not self.isNaN(normal):
                p += normal*dilate
            
            v = shape.vertices[i]
            v.SetPos(p*mix + v.position*(1-mix))
            shape.vertices[i] = v

    def isNaN(self, num):
        return num != num