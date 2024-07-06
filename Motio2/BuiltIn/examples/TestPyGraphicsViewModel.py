import Motio.UI.ViewModels.GraphicsAffectingNodeViewModel as BaseClass

class TestPyGraphicsViewModel(BaseClass):

    def get_UserGivenName(self):
        return "custom name in the ui layer"
