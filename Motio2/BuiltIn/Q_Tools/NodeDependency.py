from Motio.NodeCore.Utils import EventHall
from time import sleep
import clr
import Motio.NodeCore.Utils
clr.ImportExtensions(Motio.NodeCore.Utils)

class NodeDependency:
    def __init__(self, dependentNode):
        self.dependentNode = dependentNode
        self.oldGAffNode = None
        self.graphicsNodeDependence = False


    def updateGDependency(self, graphicsNode):
        self.graphicsNodeDependence = True
        lastGAIndex = graphicsNode.attachedNodes.Count - 1
        newNode = graphicsNode.attachedNodes[lastGAIndex]
        self.updateGaffDependency(newNode)


    def updateGaffDependency(self, newNode):
        if self.oldGAffNode == newNode:
            return
        cacheManager = self.dependentNode.GetTimeline().CacheManager
        if self.oldGAffNode:
            cacheManager.UnregisterDependant(self.oldGAffNode, self.dependentNode)
            if self.graphicsNodeDependence:
                EventHall.Unsubscribe(self.oldGAffNode.nodeHost.UUID.ToString() + ".AttachedNodes", self.dependantAttachedNodesChanged)
        if newNode:
            cacheManager.RegisterDependant(newNode, self.dependentNode)
            if self.graphicsNodeDependence:
                EventHall.Subscribe(newNode.nodeHost.UUID.ToString() + ".AttachedNodes", self.dependantAttachedNodesChanged)
        self.oldGAffNode = newNode


    def dependantAttachedNodesChanged(self, triggerer, propertyName, data):
        if propertyName != "CollectionChanged":
            return
        self.updateGDependency(self.oldGAffNode.nodeHost)


    def updateGDependencyAndGetDatafeed(self, frame, UUIDstring):
        timeline = self.dependentNode.GetTimeline()
        # get reference to graphics node from uuid
        try:
            graphicsNode = timeline.uuidGroup.LookupNode(UUIDstring)
        except:
            return False

        if not graphicsNode:
            return False

        # register dependant and add listener
        self.updateGDependency(graphicsNode)

        # get datafeed of selectedNode
        completion, cachedDataFeed = timeline.CacheManager.TryGetCache(graphicsNode,
                                                                       graphicsNode.attachedNodes.Count - 1, frame)
        if not completion:
            return False
        while not cachedDataFeed:
            sleep(0.01)
            completion, cachedDataFeed = timeline.CacheManager.TryGetCache(graphicsNode,
                                                                           graphicsNode.attachedNodes.Count - 1,
                                                                           frame)
        return cachedDataFeed

    def clearDependency(self):
        if not self.oldGAffNode:
            return
        cacheManager = self.dependentNode.GetTimeline().CacheManager
        cacheManager.UnregisterDependant(self.oldGAffNode, self.dependentNode)
        if self.graphicsNodeDependence:
            EventHall.Unsubscribe(self.oldGAffNode.nodeHost.UUID.ToString() + ".AttachedNodes",
                                  self.dependantAttachedNodesChanged)
        self.oldGAffNode = None