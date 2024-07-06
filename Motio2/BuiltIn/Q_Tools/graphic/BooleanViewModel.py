import Motio.UI.ViewModels.GraphicsAffectingNodeViewModel as BaseClass
import Q_Tools.GraphicsNodeLister as GraphicsNodeLister
from Motio.UI.Utils import ProxyStatic

class BooleanViewModel(BaseClass):
    def SetupViewModel(self):
        # manager
        dropdown = self.getProperty("clipShape")
        self.lister = GraphicsNodeLister.GraphicsNodeLister(self, GraphicsNodeLister.autoPopulateDropdown(dropdown))
        self.lister.startWatching()
        #subscribe to event
        self.Original.Properties["operation"].PropertyChanged += self.onDropDownChanged
        self.updateInterface()

    def onDropDownChanged(self,sender,args):
        if args.PropertyName == "StaticValue":
            self.updateInterface()

    def updateInterface(self):
        displayState = True if self.Original.Properties.GetValue("operation",0) == "Difference" else False
        self.getProperty("invert").Visible = displayState

    def getProperty(self, name):
        return ProxyStatic.GetProxyOf(self.Original.Properties[name])