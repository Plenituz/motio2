from Motio.NodeImpl.PropertyAffectingNodes import PyPropertyAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import FloatNodeProperty, BoolNodeProperty, DropdownNodeProperty
from System import Single
from Motio.NodeCore import Node
from Motio.NodeCommon.ToolBox import ConvertToFloat

class LoopRange(BaseClass):
    classNameStatic = "Loop range"
    acceptedPropertyTypes = [Single.__clrtype__()]

    def setup_properties(self):
        rangeMinProp = FloatNodeProperty(self, "The minimum value of the looping range", "Range minimum")
        self.Properties.Add("rangeMin", rangeMinProp, 0)
        rangeMaxProp = FloatNodeProperty(self, "The maximum value of the looping range", "Range maximum")
        self.Properties.Add("rangeMax", rangeMaxProp, 100)
        loopTypeProp = DropdownNodeProperty(self, "Bounce at the range start/end or teleport to the other side", "Loop type", ["Continuous loop", "Oscillating loop"])
        self.Properties.Add("loopType", loopTypeProp, 'Continuous loop')
        valueProp = FloatNodeProperty(self, "If this value exceed the maximum, it loops", "Looping value")
        self.Properties.Add("value", valueProp, 0)
        replaceProp = BoolNodeProperty(self, "Replace the incoming value", "Replace")
        self.Properties.Add("replace", replaceProp, True)

    def evaluate_frame(self, frame, dataFeed):
        # properties
        self.Properties.WaitForProperty("replace")
        rangeMin = self.Properties.GetValue("rangeMin", frame)
        rangeMax = self.Properties.GetValue("rangeMax", frame)
        value = self.Properties.GetValue("value", frame)
        replace = self.Properties.GetValue("replace", frame)
        loopType = self.Properties.GetValue("loopType", frame)

        if loopType == "Continuous loop":
            newVal = (float(value) % (rangeMax - rangeMin)) + rangeMin
        else:
            if (float(value) / (rangeMax - rangeMin))%2 < 1:
                newVal = (float(value) % (rangeMax - rangeMin)) + rangeMin
            else:
                newVal = rangeMax - (float(value) % (rangeMax - rangeMin))

        newVal = ConvertToFloat(newVal)

        if replace:
            dataFeed.SetChannelData(Node.PROPERTY_OUT_CHANNEL, newVal)
        else:
            previousVal = dataFeed.GetChannelData(Node.PROPERTY_OUT_CHANNEL)
            dataFeed.SetChannelData(Node.PROPERTY_OUT_CHANNEL, newVal + previousVal)