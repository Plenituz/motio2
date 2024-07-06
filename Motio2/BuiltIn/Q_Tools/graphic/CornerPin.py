from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import VectorNodeProperty, FloatNodeProperty, ButtonNodeProperty
from Motio.NodeCore import GroupNodeProperty
from Motio.Geometry import Vector2
import Motio.NodeCommon.StandardInterfaces.IDeformable as IDeformable
from Q_Tools.graphic import CornerTool

import clr
import Motio.NodeCore.Utils
clr.ImportExtensions(Motio.NodeCore.Utils)

class CornerPinDeformer(BaseClass):
    classNameStatic = "Corner pin deformer"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_node(self):
        # add gizmos
        self.PassiveTools.Add(CornerTool.CornerTool(self,"cornerGroup"))

    def setup_properties(self):
        self.getScreenCorners()
        # 4 corner point position
        cornerGroup = GroupNodeProperty(self, "Corner's position", "Corners")
        pos1Prop = VectorNodeProperty(self, "Up left corner position", "Up Left")
        cornerGroup.Properties.Add("pos1", pos1Prop, self.LeftUp)
        pos2Prop = VectorNodeProperty(self, "Up right corner position", "Up Right")
        cornerGroup.Properties.Add("pos2", pos2Prop, self.RightUp)
        pos3Prop = VectorNodeProperty(self, "Down left corner position", "Down Left")
        cornerGroup.Properties.Add("pos3", pos3Prop, self.LeftDown)
        pos4Prop = VectorNodeProperty(self, "Down right corner position", "Down Right")
        cornerGroup.Properties.Add("pos4", pos4Prop, self.RightDown)
        self.Properties.AddManually("cornerGroup", cornerGroup)

        # reset pos
        resetProp = ButtonNodeProperty(self, "Reset corner position to default", "Reset corners", "resetCorners")
        self.Properties.AddManually("reset", resetProp)

        # mix
        mixProp = FloatNodeProperty(self, "How much the deformer affect the shape", "Mix")
        mixProp.SetRangeFrom(0, True)
        mixProp.SetRangeTo(100, True)
        self.Properties.Add("mix", mixProp, 100)

    def evaluate_frame(self, frame, dataFeed):
        deformables = dataFeed.GetDataOfType[IDeformable]()
        if not deformables:
            return

        self.Properties.WaitForProperty("mix")
        mix = self.Properties.GetValue("mix", frame)/100
        cornerGroup = self.Properties["cornerGroup"]
        pos1 = cornerGroup.Properties.GetValue("pos1", frame)
        pos2 = cornerGroup.Properties.GetValue("pos2", frame)
        pos3 = cornerGroup.Properties.GetValue("pos3", frame)
        pos4 = cornerGroup.Properties.GetValue("pos4", frame)

        self.getScreenCorners()

        for deformable in deformables:
            vertices = deformable.OrderedPoints

            deformedVertices = []
            for v in vertices:
                #weighting corner impact by point position
                LeftUpDir = self.LeftUp - v.position
                RightUpDir = self.RightUp - v.position
                LeftDownDir = self.LeftDown - v.position
                RightDownDir = self.RightDown - v.position
                airLeftUp = abs(LeftUpDir.X * LeftUpDir.Y)
                airRightUp = abs(RightUpDir.X * RightUpDir.Y)
                airLeftDown = abs(LeftDownDir.X * LeftDownDir.Y)
                airRightDown = abs(RightDownDir.X * RightDownDir.Y)

                sumAir = airLeftDown + airLeftUp + airRightDown + airRightUp
                airLeftUp /= sumAir
                airRightUp /= sumAir
                airLeftDown /= sumAir
                airRightDown /= sumAir

                weightedposition = pos4*airLeftUp + pos3*airRightUp + pos2*airLeftDown + pos1*airRightDown
                v.SetPos(weightedposition*mix + v.position*(1-mix))
                deformedVertices.append(v)

            deformable.OrderedPoints = deformedVertices

    def resetCorners(self):
        self.Properties["pos1"].StaticValue = self.LeftUp
        self.Properties["pos2"].StaticValue = self.RightUp
        self.Properties["pos3"].StaticValue = self.LeftDown
        self.Properties["pos4"].StaticValue = self.RightDown

    def getScreenCorners(self):
        timeline = self.GetTimeline()
        height = timeline.CameraHeight
        width = timeline.CameraWidth
        left = -width / 2
        right = width / 2
        top = height / 2
        bottom = -height / 2
        self.LeftUp = Vector2(left, top)
        self.RightUp = Vector2(right, top)
        self.LeftDown = Vector2(left, bottom)
        self.RightDown = Vector2(right, bottom)