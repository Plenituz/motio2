import Motio.NodeCore as Core

class CornerTool(Core.NodeTool):
    def __new__(cls, *args):
        instance = Core.NodeTool.__new__(cls, *args[:-1])
        instance.groupProperty = args[1]
        return instance