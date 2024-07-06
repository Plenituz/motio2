from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import FloatNodeProperty
from Motio.Geometry import Vector2
import Motio.NodeCommon.StandardInterfaces.IDeformable as IDeformable
from System import Random

class RandomDeformer(BaseClass):
    classNameStatic = "Random deformer"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_properties(self):
        #deform
        deformProp = FloatNodeProperty(self, "How much to deform the shape", "Deform")
        deformProp.RangeFrom = -10
        deformProp.RangeTo = 10
        self.Properties.Add("deform", deformProp, 1)
        #seed
        seedProp = FloatNodeProperty(self, "How much to deform the shape", "Seed")
        seedProp.RangeFrom = 0
        seedProp.RangeTo = 10
        self.Properties.Add("seed", seedProp, 1)

        # mix
        mixProp = FloatNodeProperty(self, "How much the deformer affect the shape", "Mix")
        mixProp.SetRangeFrom(0, True)
        mixProp.SetRangeTo(100, True)
        self.Properties.Add("mix", mixProp, 100)

    def evaluate_frame(self, frame, dataFeed):
        # input
        deformables = dataFeed.GetDataOfType[IDeformable]()
        if not deformables:
            return

        # properties
        self.Properties.WaitForProperty("mix")
        mix = self.Properties.GetValue("mix", frame) / 100
        deform = self.Properties.GetValue("deform",frame)
        seed = self.Properties.GetValue("seed",frame)


        for deformable in deformables:
            vertices = deformable.OrderedPoints
            randObject = Random(seed)
            deformedVertices = []
            for v in vertices:
                xOffset = (randObject.NextDouble()*2-1)*deform
                yOffset = (randObject.NextDouble()*2-1)*deform
                v.SetPos(v.position + Vector2(xOffset, yOffset)*mix)
                deformedVertices.append(v)

            deformable.OrderedPoints = deformedVertices