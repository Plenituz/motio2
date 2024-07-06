from Motio.Geometry import Vector2
from Motio.NodeImpl.NodePropertyTypes import StringNodeProperty, ButtonNodeProperty, FloatNodeProperty, \
    SeparatorNodeProperty, IntNodeProperty, VectorNodeProperty
from System import Single, Int32
from Motio.NodeCore import Node
from Motio.NodeImpl.PropertyAffectingNodes import PyPropertyAffectingNodeBase as BaseClass
from Motio.NodeCommon.Utils import FrameRange
from Motio.NodeCommon.ToolBox import ConvertToFloat, ConvertToInt

class Expression(BaseClass):
    classNameStatic = "Expression"
    acceptedPropertyTypes = [Single.__clrtype__(), Vector2.__clrtype__(), Int32.__clrtype__()]

    def setup_properties(self):
        # expression
        self.Properties.Add("expression", StringNodeProperty(self, "Type an expression to get result like input + frame * 2", "Expression"), "output = input")
        self.Properties.AddManually('separator1', SeparatorNodeProperty(self))
        # add prop button
        addFloatProp = ButtonNodeProperty(self, "Add a new float property to use in expression", "Add float property",
                                          "addFloatProperty")
        self.Properties.AddManually("addFloat", addFloatProp)
        addIntProp = ButtonNodeProperty(self, "Add a new integer property to use in expression", "Add int property",
                                          "addIntProperty")
        self.Properties.AddManually("addInt", addIntProp)
        addVectorProp = ButtonNodeProperty(self, "Add a new Vector2 property to use in expression", "Add Vector2 property",
                                          "addVectorProperty")
        self.Properties.AddManually("addVector", addVectorProp)
        self.Properties.AddManually('separator2', SeparatorNodeProperty(self))

    def addFloatProperty(self):
        number = len([i for i in range (self.Properties.Count) if type(self.Properties[i]) == FloatNodeProperty])
        number = str(number + 1)
        self.Properties.Add("floatProperty" + number, FloatNodeProperty(self,
                                                                    "Float property manually added to this node, can be use in expression using variable floatProperty"+number,
                                                                    "Float Property "+number), 0)

    def addIntProperty(self):
        number = len([i for i in range (self.Properties.Count) if type(self.Properties[i]) == IntNodeProperty])
        number = str(number + 1)
        self.Properties.Add("intProperty" + number, IntNodeProperty(self,
                                                                    "Interger property manually added to this node, can be use in expression using variable intProperty"+number,
                                                                    "Int Property "+number), 0)

    def addVectorProperty(self):
        number = len([i for i in range (self.Properties.Count) if type(self.Properties[i]) == VectorNodeProperty])
        number = str(number + 1)
        self.Properties.Add("vectorProperty" + number, VectorNodeProperty(self,
                                                                    "Vector2 property manually added to this node, can be use in expression using variable vectorProperty"+number,
                                                                    "Vector Property "+number), Vector2.Zero)

    def get_IndividualCalculationRange(self):
        expression = self.Properties.GetValue("expression", 0)
        if "frame" in expression:
            return FrameRange.Infinite
        else:
            return super(Expression, self).IndividualCalculationRange

    def evaluate_frame(self, frame, dataFeed):
        # check expression string
        self.Properties.WaitForProperty("expression")
        expressionProp = self.Properties["expression"]
        expression = self.Properties.GetValue("expression", 0)
        #cleanup windows line endings
        expression = expression.replace('\r', '')
        if not expression:
            return

        #get input from previous node
        input = dataFeed.GetChannelData(Node.PROPERTY_OUT_CHANNEL)

        #get manually added properties
        for prop in self.Properties:
            if type(prop) in [FloatNodeProperty, VectorNodeProperty, IntNodeProperty]:
                name = self.Properties.GetUniqueName(prop)
                exec("{} = self.Properties.GetValue('{}',{})".format(name, name, frame))

        #exec
        try:
            exec(expression)
            expressionProp.ClearError(3)
        except Exception, err:
            expressionProp.SetError(3, str(err))
            return

        try:
          output
        except NameError:
          expressionProp.SetError(1, "'output' variable not detected, please put the result in the output variable")
          return
        else:
          expressionProp.ClearError(1)

        # cast to C# type
        inputType = type(input)
        if inputType == Single:
            output = ConvertToFloat(output)
        elif inputType == Int32:
            output = ConvertToInt(output)
        elif inputType == Vector2:
            pass
        else:
            expressionProp.SetError(2, "output is not a Vector2")
            return
        expressionProp.ClearError(2)

        if inputType != type(output):
            expressionProp.SetError(4, "output type ({}) doesn't match input type ({})".format(type(output), inputType))
            return
        expressionProp.ClearError(4)
        

        dataFeed.SetChannelData(Node.PROPERTY_OUT_CHANNEL, output)