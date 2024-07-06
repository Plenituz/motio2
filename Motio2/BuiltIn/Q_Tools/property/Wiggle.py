from Motio.NodeImpl.PropertyAffectingNodes import PyPropertyAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import DropdownNodeProperty, FloatNodeProperty
from System import Single as Single
from Motio.NodeCore import Node
from Motio.Geometry import Vector2
from math import cos, sin
from System import Random
from Motio.NodeCommon.Utils import FrameRange
from Motio.NodeCommon.ToolBox import ConvertToFloat
from Q_Tools import Q_Helper

class Wiggle(BaseClass):
    classNameStatic = "Wiggle"
    acceptedPropertyTypes = [Single.__clrtype__(), Vector2.__clrtype__()]

    def setup_properties(self):
        self.Properties.Add("noiseType", DropdownNodeProperty(self, "Different type of movement", "Type", ["Periodic", "Perlin noise", "Random"]), "Perlin noise")
        self.Properties.Add("speed", FloatNodeProperty(self, "Speed of vibration", "Speed"), 1)
        self.Properties.Add("power", FloatNodeProperty(self, "Power of vibration (Useless above 4 in perlin noise mode)", "Power"), 1)
        self.Properties.Add("offset", FloatNodeProperty(self, "Vibration's offset in time", "Offset"), 0)

    def get_IndividualCalculationRange(self):
        return FrameRange.Infinite

    def evaluate_frame(self, frame, dataFeed):
        previousVal = dataFeed.GetChannelData(Node.PROPERTY_OUT_CHANNEL)

        self.Properties.WaitForProperty("offset")
        noiseType = self.Properties.GetValue("noiseType", frame)
        speed = self.Properties.GetValue("speed", frame)
        power = self.Properties.GetValue("power", frame)
        offset = self.Properties.GetValue("offset", frame)

        isSingle = True if type(previousVal) == Single else False
        
        if noiseType == "Periodic":
            if isSingle:
                noiseVal = cos((frame+offset)*speed)
            else:
                noiseVal = Vector2(
                    cos((frame+offset)*speed),
                    sin((frame+offset)*speed)
                )

        elif noiseType == "Perlin noise":
            noisePosition = abs(frame + offset)
            noiseScale = 1/float(speed)*5.0 if speed != 0 else 1000.0
            if isSingle:
                noiseVal = Q_Helper.Get2DPerlinNoiseValue(float(noisePosition),float(noisePosition),noiseScale)*2
            else:
                noiseVal = Vector2(
                    Q_Helper.Get2DPerlinNoiseValue(float(noisePosition),0,noiseScale)*2,
                    Q_Helper.Get2DPerlinNoiseValue(0,float(noisePosition),noiseScale)*2
                )

        else:
            randObject = Random(frame+offset)
            if isSingle:
                noiseVal = randObject.NextDouble()*2-1
            else:
                noiseValX = randObject.NextDouble()*2-1
                noiseValY = randObject.NextDouble()*2-1
                noiseVal = Vector2(noiseValX, noiseValY)
        
        newVal = previousVal + noiseVal * power   

        if isSingle:
            newVal = ConvertToFloat(newVal)
        dataFeed.SetChannelData(Node.PROPERTY_OUT_CHANNEL, newVal)