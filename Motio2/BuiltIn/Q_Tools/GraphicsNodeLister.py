from Motio.NodeCore.Utils import EventHall, NodeUUIDGroup
from Motio.UI.Utils import ProxyStatic

import clr
import Motio.UI.Utils
clr.ImportExtensions(Motio.UI.Utils)

def autoPopulateDropdown(dropdown):
    def onListUpdate(list):
        if len(list) == 0:
            dropdown.Selected = "";
            dropdown.Choices.Clear()
            return

        currentUUID = dropdown.Selected[:NodeUUIDGroup.UUID_SIZE]
        uuidList = [item[:NodeUUIDGroup.UUID_SIZE] for item in list]
        
        try:
            newSelectionIndex = uuidList.index(currentUUID)
        except:
            newSelectionIndex = 0

        dropdown.Choices.Clear()
        for name in list:
            dropdown.Choices.Add(name)

        dropdown.Selected = list[newSelectionIndex]
        #force the StaticValue update if the selection didnt change otherwise the dropdown appears empty
        if currentUUID == uuidList[newSelectionIndex]:
            dropdown.OnPropertyChanged("StaticValue")
    return onListUpdate

class GraphicsNodeLister:

    def __init__(self, gaffViewModel, onListUpdate):
        self.onListUpdate = onListUpdate
        self.gaffViewModel = gaffViewModel
        self.propertyPanel = gaffViewModel.FindPropertyPanel()
        self.shouldUpdate = True

    def startWatching(self):
        EventHall.Subscribe("PropertyPanel", self.onPropertyPanelChanged)
        EventHall.Subscribe("PropertyPanel.DisplayedLockedNodes", self.onDisplayerLockedNodesChanged)
        EventHall.Subscribe("AnimationTimeline.GraphicsNodes", self.onGraphicsNodesChanged)

    def stopWatching(self):
        EventHall.Unsubscribe("PropertyPanel", self.onPropertyPanelChanged)
        EventHall.Unsubscribe("PropertyPanel.DisplayedLockedNodes", self.onDisplayerLockedNodesChanged)
        EventHall.Unsubscribe("AnimationTimeline.GraphicsNodes", self.onGraphicsNodesChanged)

    def updateList(self):
        if not self.shouldUpdate:
            return

        graphicsNodes = self.listGraphicsExcept(self.gaffViewModel)
        if len(graphicsNodes) == 0:
            self.onListUpdate([])
            return

        graphicsNodeNames = map(self.formatGraphicsNode, graphicsNodes)
        self.onListUpdate(graphicsNodeNames)

    def onPropertyPanelChanged(self, triggerer, propertyName, data):
        if propertyName != "DisplayedGraphicsAffectingNode": 
            return
        self.shouldUpdate = self.propertyPanel.IsVisible(self.gaffViewModel)
        self.updateList()
           
    def onGraphicsNodesChanged(self, triggerer, propertyName, data):
        if propertyName in ["CollectionChanged", "UserGivenName"]:
            self.updateList()

    def onDisplayerLockedNodesChanged(self, triggerer, propertyName, data):
        self.shouldUpdate = self.propertyPanel.IsVisible(self.gaffViewModel)
        self.updateList()

    def formatGraphicsNode(self, node):
        return node.UUID.ToString() + " : " + node.UserGivenName

    def listGraphicsExcept(self, toExclude):
        parent, _ = toExclude.FindGraphicsNode()#this returns the GraphicsNode AND GraphicsAffectingNode parents
        nodes = [node for node in parent.TimelineHost.Original.GraphicsNodes if node != parent.Original]
        return nodes
