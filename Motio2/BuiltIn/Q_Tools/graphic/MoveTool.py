import Motio.NodeCore as Core

class MoveTool(Core.NodeTool):
    def __new__(cls, *args):
        instance = Core.NodeTool.__new__(cls, *args[:-1])
        instance.positionProperty = args[1]
        return instance