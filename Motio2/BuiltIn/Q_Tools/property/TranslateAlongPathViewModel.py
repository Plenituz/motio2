import Motio.UI.ViewModels.PropertyAffectingNodeViewModel as BaseClass
import Q_Tools.DropdownManager as DropdownManager

class TranslateAlongPathViewModel(BaseClass):
    def SetupViewModel(self):
        dropDownManager = DropdownManager.DropdownManager(self, "path", "Path")
        dropDownManager.activate()