from time import sleep
from math import atan2
from Motio.NodeCommon.ToolBox import CreateMatrix
from Motio.Geometry import Vector3, Vector2
from Motio.NodeCore import Node
from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import BoolNodeProperty, FloatNodeProperty, DropdownNodeProperty
from Q_Tools.Q_Helper import estimatedCenter, calculateTangent


class MoveAlongPath(BaseClass):
    classNameStatic = "Move along path"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_properties(self):
        # path
        pathProp = DropdownNodeProperty(self, "Choose which path to use", "Path", [])
        self.Properties.Add("path", pathProp, '')
        # position
        posProp = FloatNodeProperty(self, "Position relative to the path length", "Position (%)")
        posProp.SetRangeFrom(0, True)
        posProp.SetRangeTo(100, True)
        # reverse
        self.Properties.Add("reverse", BoolNodeProperty(self, "Invert start and end of the path", "Reverse"), False)
        self.Properties.Add("pos", posProp, False)
        # replace
        self.Properties.Add("replace", BoolNodeProperty(self, "Snap the shape to the path instead of offseting it", "Replace"),
                            True)
        # orient along path
        orientProp = BoolNodeProperty(self, "Orient geometry along path normal", "Orient along path")
        self.Properties.Add("orient", orientProp, True)
        # rotation
        rotationProp = FloatNodeProperty(self, "Rotation of the shape orientation to the path", "Orient offset")
        rotationProp.SetRangeFrom(-180, False)
        rotationProp.SetRangeTo(180, False)
        self.Properties.Add("rotation", rotationProp, 0)



    def evaluate_frame(self, frame, dataFeed):
        # get shapeGroup
        if not dataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            return
        shapeGroup = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)

        # get path
        if not dataFeed.ChannelExists(Node.PATH_CHANNEL):
            return
        pathGroup = dataFeed.GetChannelData(Node.PATH_CHANNEL)


        # get properties
        self.Properties.WaitForProperty("rotation")
        pathId = self.Properties.GetValue("path", frame)
        pos = self.Properties.GetValue("pos", frame)
        orient = self.Properties.GetValue("orient", frame)
        reverse = self.Properties.GetValue("reverse", frame)
        replace = self.Properties.GetValue("replace", frame)
        rotation = self.Properties.GetValue("rotation", frame)

        # inverse input
        pos = 0.999 - pos / 100 if reverse else pos / 100.1

        if not pathId:
            pathId = 0
        path = pathGroup[int(pathId)]

        offsetPos, curvePos = self.calculateOffset(path, pos)

        if orient:
            tangent = calculateTangent(path, pos)
            angle = Vector2().AngleBetween(Vector2.UnitX, tangent) + rotation
        else:
            angle = 0

        if replace:
            position = curvePos
            func = self.snapMeshToPath
        else:
            position = offsetPos
            func = self.offsetMeshByPath

        for shape in shapeGroup:
            func(shape, position, angle)

    def calculateOffset(self, path, percent):
        """
            :type path: Motio.Pathing.Path
            :type percent: float
            :rtype: Vector2, Vector2
        """

        startPos = path.Points[0].Position
        curvePos = path.PointAtPercent(percent, True)
        if curvePos:
            return curvePos - startPos, curvePos
        else:
            return Vector2.Zero, Vector2.Zero


    def snapMeshToPath(self, shape, pos, angle):
        """
            :type shape: Motio.Meshing.MotioShape
            :type pos: Vector2
        """

        originalTransform = shape.transform
        center = estimatedCenter(shape)
        transformMatrix = CreateMatrix(pos - center, Vector3(0, 0, angle), Vector2.One, center, Vector2.Zero,
                                       ["R","S","T"])
        shape.transform = originalTransform * transformMatrix


    def offsetMeshByPath(self, shape, pos, angle):
        """
            :type shape: Motio.Meshing.MotioShape
            :type pos: Vector2
        """

        originalTransform = shape.transform
        center = estimatedCenter(shape)
        transformMatrix = CreateMatrix(pos, Vector3(0, 0, angle), Vector2.One, center, Vector2.Zero,
                                       ["R", "S", "T"])
        shape.transform = originalTransform * transformMatrix

