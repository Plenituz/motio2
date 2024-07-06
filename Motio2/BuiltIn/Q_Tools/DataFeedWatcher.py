from Motio.NodeCore.Utils import EventHall
from Motio.NodeCommon.Utils import DataFeed
from time import sleep

class DataFeedWatcher:

    def __init__(self, graphicsNode, gAffNode, onDataFeedChanged):
        self.graphicsNode = graphicsNode
        self.gAffNode = gAffNode
        self.onDataFeedChanged = onDataFeedChanged
        self.watching = False

    def changeWatchedUUID(self, newUUID):
        wasWatching = self.watching
        if wasWatching:
            self.stopWatching()
        self.graphicsNodeUUID = newUUID
        if wasWatching:
            self.startWatching()

    def startWatching(self):
        self.watching = True
        EventHall.Subscribe(self.graphicsNode.UUID.ToString() + ".AttachedNodes", self.onAttachedNodesChanged)
    
    def stopWatching(self):
        self.watching = False
        EventHall.Unsubscribe(self.graphicsNode.UUID.ToString() + ".AttachedNodes", self.onAttachedNodesChanged)

    def updateDataFeed(self):
        previousGAIndex = self.graphicsNode.attachedNodes.IndexOf(self.gAffNode) - 1
        if previousGAIndex < 0:
            self.onDataFeedChanged(DataFeed())
            return

        cachedDataFeed = self.graphicsNode.GetCache(0, previousGAIndex)
        while not cachedDataFeed:
            sleep(0.03)
            cachedDataFeed = self.graphicsNode.GetCache(0, previousGAIndex)
        
        self.onDataFeedChanged(cachedDataFeed)

    def onAttachedNodesChanged(self, triggerer, propertyName, data):
        if propertyName != "CollectionChanged":
            return
        self.updateDataFeed()


