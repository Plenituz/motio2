from Motio.NodeImpl.PropertyAffectingNodes import PyPropertyAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import FloatNodeProperty, BoolNodeProperty, DropdownNodeProperty
from Motio.Geometry import Vector2
from Motio.NodeCore import Node
from time import sleep

import clr
import Motio.NodeCore.Utils
clr.ImportExtensions(Motio.NodeCore.Utils)

class TranslateAlongPath(BaseClass):
    classNameStatic = "Move along path"
    acceptedPropertyTypes = [Vector2.__clrtype__()]

    def setup_properties(self):
        #path
        pathProp = DropdownNodeProperty(self, "Choose which path to use", "Path", [])
        self.Properties.Add("path", pathProp, '')
        #position
        posProp = FloatNodeProperty(self, "Position relative to the path length", "Position (%)")
        posProp.SetRangeFrom(0, True)
        posProp.SetRangeTo(100, True)
        self.Properties.Add("pos", posProp, 0)
        #reverse
        self.Properties.Add("reverse", BoolNodeProperty(self, "Invert start and end of the path", "Reverse"), False)
        #replace
        self.Properties.Add("replace", BoolNodeProperty(self, "Replace the value instead of adding to it", "Replace"), False)

    def evaluate_frame(self, frame, dataFeed):
        previousVal = dataFeed.GetChannelData(Node.PROPERTY_OUT_CHANNEL)

        self.Properties.WaitForProperty("pos")
        posProp = self.Properties["pos"]

        #get node parents
        graphicsNode, GANode = self.FindGraphicsNode()
        currentId = graphicsNode.attachedNodes.IndexOf(GANode)
        if currentId <1:
            posProp.SetError(1, "Needs a path, cannot be on first node of the chain") # no path found
            return
        else:
            posProp.ClearError(1)

        #get datafeed of previous GANode
        cachedDataFeed = graphicsNode.GetCache(frame,currentId-1)
        while not cachedDataFeed:
            sleep(0.01)
            cachedDataFeed = graphicsNode.GetCache(frame,currentId-1)

        #check incoming data feed
        if not cachedDataFeed.ChannelExists(Node.PATH_CHANNEL):
            posProp.SetError(2, "No path found in dataFeed") # no path found
            return
        else:
            posProp.ClearError(2)
        
        #properties
        self.Properties.WaitForProperty("replace")
        pos = self.Properties.GetValue("pos", frame)
        reverse = self.Properties.GetValue("reverse", frame)
        replace = self.Properties.GetValue("replace", frame)
        pathId = self.Properties.GetValue("path", frame)

        #inverse input
        pos = 0.999 - pos/100 if reverse else pos/100.1

        #get path and calculate offset
        pathGroup = cachedDataFeed.GetChannelData(Node.PATH_CHANNEL)
        if not pathId:
            pathId = 0
        path = pathGroup[int(pathId)]

        offsetPos, curvePos = self.calculateOffset(path, pos)
        if replace:
            dataFeed.SetChannelData(Node.PROPERTY_OUT_CHANNEL, curvePos)
        else:
            dataFeed.SetChannelData(Node.PROPERTY_OUT_CHANNEL, previousVal + offsetPos)

    def calculateOffset(self, path, percent):
        startPos = path.Points[0].Position
        curvePos = path.PointAtPercent(percent, True)
        if curvePos:
            return curvePos - startPos, curvePos
        else:
            return Vector2.Zero, Vector2.Zero