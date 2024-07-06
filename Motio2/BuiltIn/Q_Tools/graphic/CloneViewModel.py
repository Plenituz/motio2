import Motio.UI.ViewModels.GraphicsAffectingNodeViewModel as BaseClass
from Q_Tools import Q_Helper
from Motio.NodeCore import GraphicsNode
from Motio.UI.Utils import ProxyStatic
from Motio.NodeCore.Utils import NodeUUIDGroup
import clr
import Motio.UI.Utils
clr.ImportExtensions(Motio.UI.Utils)

class CloneViewModel(BaseClass):
    def SetupViewModel(self):
        self.propertyPanel = self.FindPropertyPanel()
        self.timeline = self.propertyPanel.mainViewModel.AnimationTimeline
        self.propertyPanel.PropertyChanged += self.onPropertyPanelChanged
        self.propertyPanel.DisplayedLockedNodes.CollectionChanged += self.onLockedNodesChanged  # same as below
        self.timeline.GraphicsNodes.CollectionChanged += self.onGNodesListChanged
        self.getProperty("graphicsNode").PropertyChanged += self.onGNodeDropdownChanged
        self.shouldUpdate = True

    def onGNodeDropdownChanged(self, sender, args):
        self.updateGANodesDropdown(sender.Selected)

    def onPropertyPanelChanged(self, sender, args):
        if args.PropertyName == "DisplayedGraphicsAffectingNode":
            self.shouldUpdate = (self.propertyPanel.DisplayedGraphicsAffectingNode == self) or (self in self.propertyPanel.DisplayedLockedNodes)
            selectedGNode = self.updateGNodesDropdown()
            self.updateGANodesDropdown(selectedGNode)

    def onLockedNodesChanged(self, sender, args):
        self.shouldUpdate = (self.propertyPanel.DisplayedGraphicsAffectingNode == self) or (self in self.propertyPanel.DisplayedLockedNodes) 

    def onGNodesListChanged(self, sender, args):
        selectedGNode = self.updateGNodesDropdown()
        self.updateGANodesDropdown(selectedGNode)

    def updateGNodesDropdown(self):
        if not self.shouldUpdate:
            return
        dropdown = self.getProperty("graphicsNode")
        graphicsNodes = self.listGraphicsExceptThis()
        if len(graphicsNodes) == 0:
            return
        graphicsNodeNames = map(self.formatGraphicsNode, graphicsNodes)

        #save previous selection
        if dropdown.Selected in graphicsNodeNames:
            selectedIndex = graphicsNodeNames.index(dropdown.Selected)
        else:
            selectedIndex = 0

        dropdown.Choices.Clear()
        for name in graphicsNodeNames:
            dropdown.Choices.Add(name)
        
        #try to retreive the previously selected node (it could be deleted)
        #if it has been deleted just select the first one
        toSelect = self.formatGraphicsNode(graphicsNodes[selectedIndex])
        #same as .index but in c# it returns -1 if not found instead of crashing
        selectedIndex = dropdown.Choices.IndexOf(toSelect) 
        if selectedIndex == -1:
            selectedIndex = 0
        
        forceUpdate = False
        if dropdown.Selected == dropdown.Choices[selectedIndex]:
            forceUpdate = True
        dropdown.Selected = toSelect

        #even though we set the "Selected" value above (which is just a mirror of StaticValue added for better semantic)
        #the UI is actually bound to the "StaticValue" so sending a PropertyChanged on Selected won't work 
        if forceUpdate:
            dropdown.OnPropertyChanged("StaticValue")
        return dropdown.Selected

    def updateGANodesDropdown(self, selectedGNode):
        if not self.shouldUpdate:
            return
        if not selectedGNode:
            return
        
        #get reference to selected G node
        GNode = None
        for node in self.timeline.GraphicsNodes:
            if node.UUID.ToString() == selectedGNode[:NodeUUIDGroup.UUID_SIZE]:
                GNode = node
                break
        if GNode == None:
            return

        #get GA list
        GANodes = self.listGAFromGraphicsNode(GNode)
        GANodeNames = []
        for i in range(len(GANodes)):
            GANodeNames.append("{} : {}".format(i, GANodes[i].UserGivenName))
        GANodeNames.append('Last node in chain')
        GANodeNames = list(reversed(GANodeNames))
        
        #save previous selection
        dropdownGA = self.getProperty("graphicsAffectingNode")
        if dropdownGA.Selected in GANodeNames:
            selectedIndex = GANodeNames.index(dropdownGA.Selected)
        else:
            selectedIndex = 0

        dropdownGA.Choices.Clear()
        for name in GANodeNames:
            dropdownGA.Choices.Add(name)

        #try to retreive the previously selected node (it could be deleted)
        #if it has been deleted just select the first one
        if selectedIndex == 0:
            toSelect = 'Last node in chain'
        else:
            nodeListIndex = selectedIndex - len(GANodes) # due to reversed the index need to be reversed too
            toSelect = "{} : {}".format(selectedIndex, GANodes[selectedIndex].UserGivenName)
        #same as .index but in c# it returns -1 if not found instead of crashing
        selectedIndex = dropdownGA.Choices.IndexOf(toSelect) 
        if selectedIndex == -1:
            selectedIndex = 0
        
        forceUpdate = False
        if dropdownGA.Selected == dropdownGA.Choices[selectedIndex]:
            forceUpdate = True
        dropdownGA.Selected = toSelect

        #even though we set the "Selected" value above (which is just a mirror of StaticValue added for better semantic)
        #the UI is actually bound to the "StaticValue" so sending a PropertyChanged on Selected won't work 
        if forceUpdate:
            dropdownGA.OnPropertyChanged("StaticValue")


    def formatGraphicsNode(self, node):
        return node.UUID.ToString() + " : " + node.UserGivenName

    def listGraphicsExceptThis(self):
        parent, _ = self.FindGraphicsNode()#this returns the GraphicsNode AND GraphicsAffectingNode parents
        nodes = []
        for node in self.timeline.GraphicsNodes:
            if node != parent:
                nodes.append(node)
        return nodes

    def listGAFromGraphicsNode(sef, graphicsNode):
        return graphicsNode.Original.attachedNodes

    def getProperty(self, name):
        return ProxyStatic.GetProxyOf(self.Original.Properties[name])