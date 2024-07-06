import Motio.UI.ViewModels.GraphicsAffectingNodeViewModel as BaseClass
import Q_Tools.DropdownManager as DropdownManager

class StrokeViewModel(BaseClass):
    def SetupViewModel(self):
        dropdownManager = DropdownManager.DropdownManager(self, "selection", "Path", self.insertAllPath)
        dropdownManager.activate()

    def insertAllPath(self, choiceList):
        choiceList.insert(0, "All paths")
        return choiceList