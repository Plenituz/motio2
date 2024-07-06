from Motio.NodeImpl.PropertyAffectingNodes import PyPropertyAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import FloatNodeProperty, SeparatorNodeProperty
from System import Single as Single
from Motio.NodeCore import Node
from Motio.NodeCommon.ToolBox import ConvertToFloat
from Motio.Geometry import Vector2
from Q_Tools import Q_Helper

class FitRange(BaseClass):
    classNameStatic = "Fit range"
    acceptedPropertyTypes = [Single.__clrtype__(),Vector2.__clrtype__()]

    def setup_properties(self):
        inMinProp = FloatNodeProperty(self, "The minimum value the input can be", "Input min")
        self.Properties.Add("inMin", inMinProp, -1)
        inMaxProp = FloatNodeProperty(self, "The maximum value the input can be", "Input max")
        self.Properties.Add("inMax", inMaxProp, 1)
        self.Properties.AddManually("separator1", SeparatorNodeProperty(self))
        outMinProp = FloatNodeProperty(self, "The minimum value the fitted value can take", "Output min")
        self.Properties.Add("outMin", outMinProp, 0)
        outMaxProp = FloatNodeProperty(self, "The maximum value the fitted value can take", "Output max")
        self.Properties.Add("outMax", outMaxProp, 1)

    def evaluate_frame(self, frame, dataFeed):
        #current frame value
        inputValue = dataFeed.GetChannelData(Node.PROPERTY_OUT_CHANNEL)

        # properties
        self.Properties.WaitForProperty("outMax")
        inMin = self.Properties.GetValue("inMin", frame)
        inMax = self.Properties.GetValue("inMax", frame)
        outMin = self.Properties.GetValue("outMin", frame)
        outMax = self.Properties.GetValue("outMax", frame)
        
        if type(inputValue) == Single:
            outputValue = Q_Helper.fitRange(inMin, inMax, outMin, outMax, inputValue)
            outputValue = ConvertToFloat(outputValue)
        else:
            outputValueX = Q_Helper.fitRange(inMin, inMax, outMin, outMax, inputValue.X)
            outputValueY = Q_Helper.fitRange(inMin, inMax, outMin, outMax, inputValue.Y)
            outputValue = Vector2(outputValueX, outputValueY)
        dataFeed.SetChannelData(Node.PROPERTY_OUT_CHANNEL, outputValue)