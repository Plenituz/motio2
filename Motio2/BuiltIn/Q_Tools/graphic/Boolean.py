from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import DropdownNodeProperty, BoolNodeProperty
from Motio.Boolean import BooleanOperation, PolyFillType, ClipType
from Motio.NodeCore.Utils import NodeUUIDGroup, EventHall
from Motio.NodeCore import Node
from Q_Tools.NodeDependency import NodeDependency

import clr
import Motio.NodeCore.Utils
clr.ImportExtensions(Motio.NodeCore.Utils)


class Boolean(BaseClass):
    classNameStatic = "Boolean"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def get_IndividualCalculationRange(self):
        self.Properties.WaitForProperty("clipShape")
        otherShape = self.Properties.GetValue("clipShape", 0)
        otherGNode = self.getGNodeFromUUID(otherShape[:NodeUUIDGroup.UUID_SIZE])
        if otherGNode:
            otherLastGA = otherGNode.attachedNodes[otherGNode.attachedNodes.Count - 1]
            return otherLastGA.CalculationRange
        else:
            return super(Boolean, self).IndividualCalculationRange

    def setup_properties(self):
        clipProp = DropdownNodeProperty(self, "Choose wich graphics node to compute clip with", "Cliping", [])
        self.Properties.Add("clipShape", clipProp, '')
        invertProp = BoolNodeProperty(self, "Invert the two shape groups", "Invert")
        self.Properties.Add("invert", invertProp, False)
        opProp = DropdownNodeProperty(self,
                                  "What operation to do with shapes (Difference : A - B, Intersection : A and B, Union : A or B, Xor : A or B but not A and B)",
                                  "Operation", ["Difference", "Intersection", "Union", "Xor"])
        self.Properties.Add("operation", opProp, "Intersection")
        fillProp = DropdownNodeProperty(self,
                                  "How to fill the space",
                                  "Fill type", ["EvenOdd", "Negative", "NonZero", "Positive"])
        self.Properties.Add("fill", fillProp, "EvenOdd")

    def setup_node(self):
        self.nodeDependency = NodeDependency(self)

    def evaluate_frame(self, frame, dataFeed):
        #input check
        if not dataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            return
        shapeGroup = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)

        # properties
        self.Properties.WaitForProperty("fill")
        clipShape = self.Properties.GetValue("clipShape", frame)
        invert = self.Properties.GetValue("invert", frame)
        operation = self.Properties.GetValue("operation", frame)
        fill = self.Properties.GetValue("fill", frame)

        # operation and fill type to boolean object type
        exec("fillType = PolyFillType.{}".format(fill))
        exec("clipType = ClipType.{}".format(operation))

        #get other shape group
        cachedDataFeed = self.nodeDependency.updateGDependencyAndGetDatafeed(frame, clipShape[:NodeUUIDGroup.UUID_SIZE])
        if not cachedDataFeed:
            return
        if not cachedDataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            return
        clipShapeGroup = cachedDataFeed.GetChannelData(Node.POLYGON_CHANNEL)

        if invert:
            shapeGroup, clipShapeGroup = clipShapeGroup, shapeGroup

        # boolean
        success, newShapeGroup = BooleanOperation.Execute(shapeGroup, clipShapeGroup, fillType, clipType)
        if success:
            dataFeed.SetChannelData(Node.POLYGON_CHANNEL, newShapeGroup)

    def getGNodeFromUUID(self, UUIDstring):
        timeline = self.GetTimeline()
        # get reference to graphics node from uuid
        try:
            graphicsNode = timeline.uuidGroup.LookupNode(UUIDstring)
        except:
            return False

        if not graphicsNode:
            return False
        return graphicsNode