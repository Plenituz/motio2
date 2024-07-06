import Motio.UI.ViewModels.PropertyAffectingNodeViewModel as BaseClass
import Q_Tools.DropdownManager as DropdownManager

class OrientAlongPathViewModel(BaseClass):
    def SetupViewModel(self):
        dropdownManager = DropdownManager.DropdownManager(self, "path", "Path")
        dropdownManager.activate()