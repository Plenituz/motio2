from Motio.NodeCore import Node
from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import FloatNodeProperty, VectorNodeProperty, BoolNodeProperty
from Motio.Geometry import Vector2, Vertex
from Q_Tools.Volume import Volume
from Motio.Meshing import MotioShapeGroup
import time


class Metaball(BaseClass):
    classNameStatic = "Metaball"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_properties(self):
        influenceProp = FloatNodeProperty(self, "How far two shapes will influence each other", "Influence")
        self.Properties.Add("influence", influenceProp, 1)
        extendProp = FloatNodeProperty(self, "", "Extend")
        self.Properties.Add("extend", extendProp, 0.5)
        resolutionProp = VectorNodeProperty(self, "How thin the computation will go", "Resolution")
        resolutionProp.uniform = True
        self.Properties.Add("resolution", resolutionProp, Vector2(1, 1))
        self.Properties.Add("debug", BoolNodeProperty(self, "Show debug volume", "Debug"), False)

    def evaluate_frame(self, frame, dataFeed):
        # check input
        if not dataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            return
        shapeGroup = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)

        # properties
        self.Properties.WaitForProperty("debug")
        influence = self.Properties.GetValue("influence", frame)
        resolution = self.Properties.GetValue("resolution", frame)
        extend = self.Properties.GetValue("extend", frame)
        debug = self.Properties.GetValue("debug", frame)

        resProp = self.Properties["resolution"]
        if resolution.X <= 0 or resolution.Y <= 0:
            resProp.SetError(1, "Size must be greater than 0")
            return
        else:
            resProp.ClearError(1)

        newShapeGroup = MotioShapeGroup()
        globalVolume = None
        shapes = []

        for shape in shapeGroup:
            shape.BakeTransform()
            volume = Volume(resolution, shape, True)
            # bT = time.time()
            volume.blur(influence)
            # print("blur", time.time() - bT)
            if globalVolume:
                # bT = time.time()
                globalVolume = volume.merge(globalVolume, volume)
                # print("merge", time.time() - bT)
            else:
                globalVolume = volume

        # globalVolume.blur(influence)
        # bT = time.time()
        if debug:
            shapes = globalVolume.drawDebug()
        else:
            shapes = globalVolume.toShape(extend)
        # print("toShape", time.time() - bT)
        for shape in shapes:
            newShapeGroup.Add(shape)

        dataFeed.SetChannelData(Node.POLYGON_CHANNEL, newShapeGroup)