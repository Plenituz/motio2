from Motio.NodeImpl.PropertyAffectingNodes import PyPropertyAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import FloatNodeProperty
from System import Single as Single
from Motio.NodeCore import Node
from Motio.Geometry import Vector2
from Motio.NodeCommon.Utils import FrameRange
from Motio.NodeCommon.ToolBox import ConvertToFloat
from time import sleep

class Overshoot(BaseClass):
    classNameStatic = "Overshoot"
    acceptedPropertyTypes = [Single.__clrtype__(),Vector2.__clrtype__()]

    def setup_properties(self):
        attractionProp = FloatNodeProperty(self, "Attraction of the input value on the output value", "Attraction")
        attractionProp.SetRangeFrom(0.001, True)
        attractionProp.SetRangeTo(15, False)
        self.Properties.Add("attraction", attractionProp, 1)
        inertiaProp = FloatNodeProperty(self, "How slowly the output value can change of value (0 means the attraction has full power on the output value / 1 means the value will go in the same direction indefinitely)", "Inertia")
        inertiaProp.SetRangeFrom(0, True)
        inertiaProp.SetRangeTo(1, True)
        self.Properties.Add("inertia", inertiaProp, 0.8)

    def get_IndividualCalculationRange(self):
        return FrameRange.Infinite

    def evaluate_frame(self, frame, dataFeed):
        self.Properties.WaitForProperty("inertia")
        attractionInput = self.Properties.GetValue("attraction", frame)
        inertia = self.Properties.GetValue("inertia", frame)

        #current frame value
        desiredCurrentVal = dataFeed.GetChannelData(Node.PROPERTY_OUT_CHANNEL)

        if frame<1:
            previousVal = desiredCurrentVal
        else:
            previousVal = self.getValueAt(self, frame-1)

        if frame<2:
            if type(desiredCurrentVal) == Single:
                previousVel = 0
            else:
                previousVel = Vector2.Zero
        else:
            previousVel = previousVal - self.getValueAt(self, frame-2)

        attraction = (desiredCurrentVal - previousVal)*attractionInput
        newVal = previousVal + (previousVel*inertia) + (attraction*(1-inertia))
        if type(desiredCurrentVal) == Single:
            dataFeed.SetChannelData(Node.PROPERTY_OUT_CHANNEL, ConvertToFloat(newVal))
        dataFeed.SetChannelData(Node.PROPERTY_OUT_CHANNEL, newVal)

    def getValueAt(self, node, frame):
        propertyNode = node.propertyHost
        dataFeed = propertyNode.GetCacheOrCalculateEndOfChain(frame)
        while not dataFeed:
            sleep(0.01)
            dataFeed = propertyNode.GetCacheOrCalculateEndOfChain(frame)
        return dataFeed.GetChannelData(Node.PROPERTY_OUT_CHANNEL)