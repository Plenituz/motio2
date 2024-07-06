import Motio.UI.ViewModels.GraphicsAffectingNodeViewModel as BaseClass
from Motio.UI.Utils import ProxyStatic
import Q_Tools.GraphicsNodeLister as GraphicsNodeLister

class DuplicateViewModel(BaseClass):
    def SetupViewModel(self):
        # manager
        dropdown = self.getProperty("duplicateIn")
        self.lister = GraphicsNodeLister.GraphicsNodeLister(self, GraphicsNodeLister.autoPopulateDropdown(dropdown))

        #subscribe to event
        self.Original.Properties["action"].PropertyChanged += self.onPropertyChanged
        self.Original.Properties["random"].PropertyChanged += self.onPropertyChanged

        #listProperties
        self.transformProps = map(self.getProperty, ['pos','rotation','rotCenter','scale','scaleCenter','order'])
        self.alongAroundProps = map(self.getProperty, ['random', 'orient'])
        self.inverseProps = map(self.getProperty, ['duplicateIn','inverse'])
        self.seedProps = map(self.getProperty, ['seed'])

        #first update interface
        self.updateInterface()

    # GENERAL INTERFACE VISIBILITY
    def onPropertyChanged(self,sender,args):
        if args.PropertyName == "StaticValue":
            self.updateInterface()

    def updateInterface(self):
        selected = self.Original.Properties.GetValue("action",0)
        
        if selected == "Transform":
            self.lister.stopWatching()
            self.setVisibility(self.transformProps, True)
            self.setVisibility(self.alongAroundProps, False)
            self.setVisibility(self.inverseProps, False)
            self.setVisibility(self.seedProps, False)
        elif selected == "Inside shape":
            self.lister.startWatching()
            self.lister.updateList()
            self.setVisibility(self.transformProps, False)
            self.setVisibility(self.alongAroundProps, False)
            self.setVisibility(self.inverseProps, True)
            self.setVisibility(self.seedProps, True)
        else:
            random = self.Original.Properties.GetValue("random",0)
            self.setVisibility(self.transformProps, False)
            self.setVisibility(self.alongAroundProps, True)
            if selected == "Along path":
                self.lister.stopWatching()
                self.setVisibility(self.inverseProps, False)
            else:
                self.lister.startWatching()
                self.lister.updateList()
                self.setVisibility(self.inverseProps, True)
            if random == True:
                self.setVisibility(self.seedProps, True)
            else:
                self.setVisibility(self.seedProps, False)

    def setVisibility(self, propList, visibility):
        for prop in propList:
            prop.Visible = visibility

    #HELPERS
    def getProperty(self, name):
        return ProxyStatic.GetProxyOf(self.Original.Properties[name])