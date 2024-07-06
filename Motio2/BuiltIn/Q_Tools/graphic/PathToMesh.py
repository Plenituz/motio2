from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import FloatNodeProperty, ColorNodeProperty, DropdownNodeProperty, IntNodeProperty, SeparatorNodeProperty
from Motio.Meshing import MotioShape, MotioShapeGroup
from Motio.Geometry import Vector2, Vertex
from Motio.NodeCore import Node
from Motio.Graphics import Color
from Motio.Animation import SmartBezierSampler

class PathToMesh(BaseClass):
    classNameStatic = "Path to mesh"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_properties(self):
        #details
        detailProp = FloatNodeProperty(self, "How much detail on created mesh", "Detail")
        detailProp.SetRangeFrom(0, True)
        detailProp.SetRangeTo(150, False)
        self.Properties.Add("detail", detailProp, 1)
        self.Properties.AddManually('separator1', SeparatorNodeProperty(self))
        #color
        colorProp = ColorNodeProperty(self, "Color of the object", "Color")
        self.Properties.Add("color", colorProp, Color.DodgerBlue)
        #zindex
        zIndexProp = IntNodeProperty(self, "Order in the stack of layer", "Z Index")
        self.Properties.Add("zIndex", zIndexProp, 0)
        #action
        actionProp = DropdownNodeProperty(self, "Choose what to do with existing shapes", "Action", ["Replace", "Merge"])
        self.Properties.Add("action", actionProp, "Merge")

    def evaluate_frame(self, frame, dataFeed):
        detailProp = self.Properties["detail"]
        #check incoming data feed
        if not dataFeed.ChannelExists(Node.PATH_CHANNEL):
            detailProp.SetError(1, "Needs a path") # no path found
            return
        else:
            detailProp.ClearError(1)


        #PROPERTIES
        self.Properties.WaitForProperty("action")
        detail = self.Properties.GetValue("detail", frame)/10
        color = self.Properties.GetValue("color", frame).ToVector4()
        action = self.Properties.GetValue("action",frame)
        zIndex = self.Properties.GetValue("zIndex",frame)


        #MESH GEN
        # get path
        pathGroup = dataFeed.GetChannelData(Node.PATH_CHANNEL)
        path = pathGroup[0]
        shape = MotioShape()
        
        # smart sample path
        shapePoints = self.SmartSampledPoints(path, detail)
        shapePoints = [Vertex(pos, color) for pos in shapePoints]

        shape.vertices = shapePoints

        #OUTPUT
        shape.zIndex = zIndex
        shapeGroupInput = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)
        if action == "Merge" and shapeGroupInput:
            shapeGroupInput.Add(shape)
            shapeGroup = shapeGroupInput
        else:
            shapeGroup = MotioShapeGroup(shape)

        dataFeed.SetChannelData(Node.POLYGON_CHANNEL, shapeGroup)

    def SmartSampledPoints(self, path, detail):
        shapePoints = []
        shapePointsSet = set()
        firstPoint = path.Points[0]
        init = False
        currentPoint = firstPoint
        nextPoint = firstPoint.NextPoint
        sampler = SmartBezierSampler()
        while nextPoint:
            samplerResult = sampler.SampleCurve(currentPoint.Position, currentPoint.RightHandle+currentPoint.Position, nextPoint.LeftHandle+nextPoint.Position, nextPoint.Position, detail)
            for result in samplerResult[0]:
                if result not in shapePointsSet:
                    shapePoints.append(result)
                    shapePointsSet.add(result)
            currentPoint = nextPoint
            nextPoint = currentPoint.NextPoint
            if currentPoint == firstPoint and init:
                break
            init = True

        return shapePoints