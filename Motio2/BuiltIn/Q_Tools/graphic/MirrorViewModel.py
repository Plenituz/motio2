import Motio.UI.ViewModels.GraphicsAffectingNodeViewModel as BaseClass
from Motio.UI.Utils import ProxyStatic

class MirrorViewModel(BaseClass):
    def SetupViewModel(self):
        #subscribe to event
        self.Original.Properties["action"].PropertyChanged += self.onDropDownChanged
        self.updateInterface()

    def onDropDownChanged(self,sender,args):
        if args.PropertyName == "StaticValue":
            self.updateInterface()

    def updateInterface(self):
        displayState = True if self.Original.Properties.GetValue("action",0) == "Cut" else False
        self.getProperty("reverse").Visible = displayState

    def getProperty(self, name):
        return ProxyStatic.GetProxyOf(self.Original.Properties[name])