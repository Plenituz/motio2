from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import IntNodeProperty, DropdownNodeProperty
from Motio.NodeCore import Node

class Zindex(BaseClass):
    classNameStatic = "Zindex"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_properties(self):
        #zindex
        zIndexProp = IntNodeProperty(self, "Order in the stack of layer", "Z Index")
        zIndexProp.RangeFrom = -100
        zIndexProp.RangeTo = 100
        self.Properties.Add("zIndex", zIndexProp, 0)
        #action
        actionProp = DropdownNodeProperty(self, "Choose how to apply Z index", "Action", ["Replace", "Offset"])
        self.Properties.Add("action", actionProp, "Replace")

    def evaluate_frame(self, frame, dataFeed):
        # check input
        if not dataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            return
        shapeGroup = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)
    
        # properties
        self.Properties.WaitForProperty("action")
        action = self.Properties.GetValue("action",frame)
        zIndex = self.Properties.GetValue("zIndex",frame)

        for shape in shapeGroup:
            self.modifyZindex(shape, zIndex, action)

    def modifyZindex(self, shape, zIndex, action):
        #holes
        for hole in shape.holes:
            self.modifyZindex(hole, zIndex, action)

        if action == "Replace":
            shape.zIndex = zIndex
        else:
            shape.zIndex += zIndex