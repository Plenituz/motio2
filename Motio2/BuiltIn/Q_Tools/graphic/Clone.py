from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase
from Motio.NodeImpl.NodePropertyTypes import DropdownNodeProperty
from Motio.NodeCore import Node
from Motio.NodeCore.Utils import NodeUUIDGroup
from Motio.NodeCommon.Utils import FrameRange
from Q_Tools import Q_Helper
from time import sleep

import clr
import Motio.NodeCore.Utils
clr.ImportExtensions(Motio.NodeCore.Utils)

class Clone(PyGraphicsAffectingNodeBase):
    classNameStatic = "Clone"
    def __new__(cls, *args):
        return PyGraphicsAffectingNodeBase.__new__(cls, *args)

    def setup_properties(self):
        #graphic node
        graphicsNodeProp = DropdownNodeProperty(self, "Choose which graphics node to search on", "From", [])
        self.Properties.Add("graphicsNode", graphicsNodeProp, '')
        #graphic affecting node
        graphicsAffectingNodeProp = DropdownNodeProperty(self, "Choose which graphics node to clone", "clone", [])
        self.Properties.Add("graphicsAffectingNode", graphicsAffectingNodeProp, '')
        #action
        actionProp = DropdownNodeProperty(self, "Choose what to do with existing shapes", "Action", ["Replace", "Merge"])
        self.Properties.Add("action", actionProp, "Merge")
        #clone type
        typeProp = DropdownNodeProperty(self, "Choose what data type to clone", "Data type", ["All", "Shape", "Path"])
        self.Properties.Add("type", typeProp, "All")

    def setup_node(self):
        self.oldGAffNode = None

    # def get_IndividualCalculationRange(self):
    #     graphicsNode, GANode = self.getSelectedNodes()
    #     if not GANode:
    #         return
    #     return GANode.CalculationRange
    #     # return FrameRange.Infinite

    def evaluate_frame(self, frame, dataFeed):
        graphicsNode, GANode = self.getSelectedNodes()
        self.checkNodeChanged(GANode)
        if not GANode:
            return

        #get datafeed of selectedNode
        cachedDataFeed = graphicsNode.GetCache(frame,GANode)
        while not cachedDataFeed:
            sleep(0.01)
            cachedDataFeed = graphicsNode.GetCache(frame,GANode)

        action = self.Properties.GetValue("action", frame)
        type = self.Properties.GetValue("type", frame)

        #clone and merge datafeeds
        newDataFeed = cachedDataFeed.Clone()
        #print([chan for chan in dataFeed.ListChannels()])
        if type == "All" or type == "Shape":
            self.mergeChannelIntoDataFeed(Node.POLYGON_CHANNEL, dataFeed, newDataFeed, action)
        if type == "All" or type == "Path":
            self.mergeChannelIntoDataFeed(Node.PATH_CHANNEL, dataFeed, newDataFeed, action)

    def mergeChannelIntoDataFeed(self, channel, dataFeed, newDataFeed, action):
        if action == "Merge" and dataFeed.ChannelExists(channel):
            if newDataFeed.ChannelExists(channel):
                oldChannelGroup = dataFeed.GetChannelData(channel)
                newChannelGroup = newDataFeed.GetChannelData(channel)
                for data in newChannelGroup:
                    oldChannelGroup.Add(data)
            else:
                return
        else:
            if newDataFeed.ChannelExists(channel):
                dataFeed.SetChannelData(channel, newDataFeed.GetChannelData(channel))

    def getSelectedNodes(self):
        self.Properties.WaitForProperty("type")
        graphicsNodeName = self.Properties.GetValue("graphicsNode", 0)
        graphicsAffectingNode = self.Properties.GetValue("graphicsAffectingNode", 0)
        # print(graphicsNodeName, graphicsAffectingNode)
        if not graphicsNodeName or not graphicsAffectingNode:
            return False, False

        # get reference to graphics node
        try:
            graphicsNode = self.GetTimeline().uuidGroup.LookupNode(graphicsNodeName[:NodeUUIDGroup.UUID_SIZE])
        except:
            self.Properties["graphicsNode"].SetError(1, "UUID not found")
            return False, False

        # get id of GA node on G node
        if graphicsAffectingNode == 'Last node in chain':
            if graphicsNode.attachedNodes.Count == 0:
                return graphicsNode, False
            idGANode = graphicsNode.attachedNodes.Count - 1
        else:
            idGANode = int(graphicsAffectingNode.split(' ')[0])
        # print(idGANode)

        return graphicsNode, graphicsNode.attachedNodes[idGANode]

    def checkNodeChanged(self, newNode):
        if self.oldGAffNode == newNode:
            return
        cacheManager = self.GetTimeline().CacheManager
        if self.oldGAffNode:
            cacheManager.UnregisterDependant(self.oldGAffNode, self)
        if newNode:
            cacheManager.RegisterDependant(newNode, self)
        self.oldGAffNode = newNode
