import Motio.UI.ViewModels.GraphicsAffectingNodeViewModel as BaseClass
from Motio.UI.Utils import ProxyStatic

class ColorViewModel(BaseClass):
    def SetupViewModel(self):
        #subscribe to event
        self.Original.Properties["type"].PropertyChanged += self.onDropDownChanged
        self.updateInterface()

    def onDropDownChanged(self,sender,args):
        if args.PropertyName == "StaticValue":
            self.updateInterface()

    def updateInterface(self):
        displayState = True if self.Original.Properties.GetValue("type",0) == "Constant" else False
        self.getProperty("color").Visible = displayState
        self.getProperty("seed").Visible = not displayState

    def getProperty(self, name):
        return ProxyStatic.GetProxyOf(self.Original.Properties[name])