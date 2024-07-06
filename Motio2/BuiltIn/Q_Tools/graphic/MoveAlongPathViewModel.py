import Motio.UI.ViewModels.GraphicsAffectingNodeViewModel as BaseClass
import Q_Tools.DropdownManager as DropdownManager
# import Q_Tools.DataFeedWatcher as DataFeedWatcher
from Motio.NodeCore import Node
from Motio.UI.Utils import ProxyStatic

import Motio.NodeCore.Utils 
import clr
clr.ImportExtensions(Motio.NodeCore.Utils)


class MoveAlongPathViewModel(BaseClass):
    def SetupViewModel(self):
        gAffNode = self.Original
        graphicsNode = gAffNode.nodeHost
        # watcher = DataFeedWatcher.DataFeedWatcher(graphicsNode, gAffNode, self.onDataFeedChanged)
        # watcher.startWatching()
        dropdownManager = DropdownManager.DropdownManager(self, "path", "Path")
        dropdownManager.activate()

    def getProperty(self, name):
        return ProxyStatic.GetProxyOf(self.Original.Properties[name])


    def onDataFeedChanged(self, dataFeed):
        print(dataFeed)
        gotPaths, paths = dataFeed.TryGetChannelData(Node.PATH_CHANNEL)
        pathProp = self.getProperty("path")

        print(gotPaths, paths)

        if not gotPaths:
            pathProp.Choices.Clear()
            return

        oldSelected = pathProp.Selected
        pathProp.Choices.Clear()
        for num in range(paths.Count):
            print("adding", num)
            pathProp.Choices.Add(str(num))