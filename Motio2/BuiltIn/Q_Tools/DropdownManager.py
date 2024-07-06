from Motio.UI.Utils import ProxyStatic
from Motio.NodeCore import Node, PropertyAffectingNode
from time import sleep
import clr
import Motio.UI.Utils
clr.ImportExtensions(Motio.UI.Utils)

class DropdownManager:
    def __init__(self, viewModelInstance, dropDownName, managerType, listModifier = False):
        """
        :type viewModelInstance: Motio.UI.ViewModels.GraphicsAffectingNodeViewModel
        :param dropDownName: string
        :param listModifier: function
        """
        self.graphicsOrPropertyViewModelInstance = viewModelInstance
        # check if the dropdown is on a PA or GA node and get GA if PA
        if isinstance(viewModelInstance.Original, PropertyAffectingNode):
            self.graphicsViewModelInstance = viewModelInstance.Host.Host
        else:
            self.graphicsViewModelInstance = viewModelInstance
        self.dropDownName = dropDownName
        self.listModifier = listModifier
        if managerType == "Path":
            self.updateInterface = self.pathUpdate
        else:
            self.updateInterface = self.GNUpdate

    def activate(self):
        # dropdown update
        self.propertyPanel = self.graphicsOrPropertyViewModelInstance.FindPropertyPanel()
        self.timeline = self.propertyPanel.mainViewModel.AnimationTimeline
        self.propertyPanel.PropertyChanged += self.onPropertyPanelChanged
        self.propertyPanel.DisplayedLockedNodes.CollectionChanged += self.onLockedNodesChanged  # same as below
        self.timeline.GraphicsNodes.CollectionChanged += self.onGNodesListChanged
        self.shouldUpdate = True

        # first update interface
        self.updateInterface()

    # DROPDOWN GRAPHICS NODES UPDATER
    def onPropertyPanelChanged(self, sender, args):
        if args.PropertyName == "DisplayedGraphicsAffectingNode":
            self.shouldUpdate = (self.propertyPanel.DisplayedGraphicsAffectingNode == self.graphicsViewModelInstance) or (
                self.graphicsViewModelInstance in self.propertyPanel.DisplayedLockedNodes)
            self.updateInterface()

    def onLockedNodesChanged(self, sender, args):
        self.shouldUpdate = (self.propertyPanel.DisplayedGraphicsAffectingNode == self.graphicsViewModelInstance) or (
            self.graphicsViewModelInstance in self.propertyPanel.DisplayedLockedNodes)

    def onGNodesListChanged(self, sender, args):
        self.updateInterface()

    def pathUpdate(self):
        if not self.shouldUpdate:
            return
        
        # get previous node data feed
        parent, _ = self.graphicsOrPropertyViewModelInstance.FindGraphicsNode()
        parent = parent.Original

        previousGAIndex = parent.attachedNodes.IndexOf(self.graphicsViewModelInstance.Original) - 1
        if previousGAIndex < 0:
            return
        cachedDataFeed = parent.GetCache(0, previousGAIndex)
        while not cachedDataFeed:
            sleep(0.01)
            cachedDataFeed = parent.GetCache(0, previousGAIndex)
        if cachedDataFeed.ChannelExists(Node.PATH_CHANNEL):
            pathGroup = cachedDataFeed.GetChannelData(Node.PATH_CHANNEL)
        else:
            pathGroup = []

        # get selected and create new choices
        dropdown = self.getProperty(self.dropDownName)
        selection = dropdown.Selected
        newSelectionList = [str(i) for i in range(pathGroup.Count)]
        if self.listModifier:
            newSelectionList = self.listModifier(newSelectionList)
        dropdown.Choices.Clear()
        for path in newSelectionList:
            dropdown.Choices.Add(path)

        # change to selected if it is still there
        selectedIndex = dropdown.Choices.IndexOf(selection)
        if selectedIndex == -1:
            selectedIndex = 0
        forceUpdate = False
        if dropdown.Selected == dropdown.Choices[selectedIndex]:
            forceUpdate = True
        dropdown.Selected = newSelectionList[selectedIndex]
        if forceUpdate:
            dropdown.OnPropertyChanged("StaticValue")

    def GNUpdate(self):
        if not self.shouldUpdate:
            return
        dropdown = self.getProperty(self.dropDownName)
        graphicsNodes = self.listGraphicsExceptThis()
        if len(graphicsNodes) == 0:
            return
        graphicsNodeNames = map(self.formatGraphicsNode, graphicsNodes)

        # save previous selection
        if dropdown.Selected in graphicsNodeNames:
            selectedIndex = graphicsNodeNames.index(dropdown.Selected)
        else:
            selectedIndex = 0

        if self.listModifier:
            graphicsNodeNames = self.listModifier(graphicsNodeNames)

        dropdown.Choices.Clear()
        for name in graphicsNodeNames:
            dropdown.Choices.Add(name)

        toSelect = self.formatGraphicsNode(graphicsNodes[selectedIndex])
        selectedIndex = dropdown.Choices.IndexOf(toSelect)
        if selectedIndex == -1:
            selectedIndex = 0
        forceUpdate = False
        if dropdown.Selected == dropdown.Choices[selectedIndex]:
            forceUpdate = True
        dropdown.Selected = toSelect
        if forceUpdate:
            dropdown.OnPropertyChanged("StaticValue")

    # HELPERS
    def getProperty(self, name):
        return ProxyStatic.GetProxyOf(self.graphicsOrPropertyViewModelInstance.Original.Properties[name])

    def formatGraphicsNode(self, node):
        return node.UUID.ToString() + " : " + node.UserGivenName

    def listGraphicsExceptThis(self):
        parent, _ = self.graphicsOrPropertyViewModelInstance.FindGraphicsNode()#this returns the GraphicsNode AND GraphicsAffectingNode parents
        nodes = []
        for node in self.timeline.GraphicsNodes:
            if node != parent:
                nodes.append(node)
        return nodes