from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import DropdownNodeProperty, IntNodeProperty, FloatNodeProperty, BoolNodeProperty
from Motio.NodeImpl.NodePropertyTypes import SeparatorNodeProperty, ColorNodeProperty
from Motio.Meshing import StrokeTracerShape, MotioShapeGroup, MotioShape
from Motio.NodeCore import Node
from Motio.NodeCommon.ToolBox import ConvertToFloat
from Motio.Animation import SmartBezierSampler
from Motio.Graphics import Color
from Q_Tools import Q_Helper
from System import Enum

class Stroke(BaseClass):
    classNameStatic = "Stroke"
    saveAttrs = ["selection"]
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_node(self):
        self.selection = "All paths"

    def setup_properties(self):
        #path selection
        selection = DropdownNodeProperty(self, "Choose wich path to stroke", "Path", ["All paths"])
        self.Properties.Add("selection", selection, self.selection)
        #details
        detailProp = IntNodeProperty(self, "How much sampling detail to approximate curve", "Detail")
        detailProp.SetRangeFrom(0, True)
        detailProp.SetRangeTo(150, False)
        self.Properties.Add("detail", detailProp, 1)
        #thicknessStart
        thicknessStart = FloatNodeProperty(self, "Thickness of the stroke at the path's start", "Thickness at start")
        thicknessStart.SetRangeFrom(0, True)
        thicknessStart.SetRangeTo(5, False)
        self.Properties.Add("thicknessStart", thicknessStart, 1)
        #thicknessEnd
        thicknessEnd = FloatNodeProperty(self, "Thickness of the stroke at the path's end", "Thickness at end")
        thicknessEnd.SetRangeFrom(0, True)
        thicknessEnd.SetRangeTo(5, False)
        self.Properties.Add("thicknessEnd", thicknessEnd, 1)
        #fit thickness using crop
        fitThickness = BoolNodeProperty(self, "Fit the thicnkess range to the cropped zone or crop the thicnkess too", "Fit thickness to crop")
        self.Properties.Add("fitThickness", fitThickness, True)
        #cropStart
        cropStart = FloatNodeProperty(self, "Start the stroke at this percentage", "Start stroke at (%)")
        cropStart.SetRangeFrom(0, True)
        cropStart.SetRangeTo(100, True)
        self.Properties.Add("cropStart", cropStart, 0)
        #cropEnd
        cropEnd = FloatNodeProperty(self, "Thickness of the stroke at the path's end", "End stroke at (%)")
        cropEnd.SetRangeFrom(0, True)
        cropEnd.SetRangeTo(100, True)
        self.Properties.Add("cropEnd", cropEnd, 100)
        #cap selection
        allCaps = Enum.GetNames(StrokeTracerShape.EndCap)
        cap = DropdownNodeProperty(self, "Choose wich type of cap to use", "Cap", allCaps)
        self.Properties.Add("cap", cap, allCaps[0])
        self.Properties.AddManually('separator1', SeparatorNodeProperty(self))
        #color
        colorProp = ColorNodeProperty(self, "Color of the stroke", "Color")
        self.Properties.Add("color", colorProp, Color.DodgerBlue)
        #zindex
        zIndexProp = IntNodeProperty(self, "Order in the stack of layer", "Z Index")
        self.Properties.Add("zIndex", zIndexProp, 0)
        #deletePath
        deletePath = BoolNodeProperty(self, "Choose what to do with the path being stroked", "Delete stroked path")
        self.Properties.Add("deletePath", deletePath, True)

    def evaluate_frame(self, frame, dataFeed):
        self.Properties.WaitForProperty("deletePath")
        #check incoming data feed
        selectionProp = self.Properties["selection"]
        if not dataFeed.ChannelExists(Node.PATH_CHANNEL):
            selectionProp.SetError(1, "Needs a path") # no path found
            return
        else:
            selectionProp.ClearError(1)
        pathGroup = dataFeed.GetChannelData(Node.PATH_CHANNEL)

        #properties
        cropStart = self.Properties.GetValue("cropStart", frame)
        cropEnd = self.Properties.GetValue("cropEnd", frame)
        if cropStart == cropEnd:
            return
        if cropStart > cropEnd:
            cropStart, CropEnd = cropEnd, cropStart

        thicknessStart = self.Properties.GetValue("thicknessStart", frame)
        thicknessEnd = self.Properties.GetValue("thicknessEnd", frame)

        cap = Enum.Parse(StrokeTracerShape.EndCap, self.Properties.GetValue("cap", frame))

        color = self.Properties.GetValue("color", frame).ToVector4()
        deletePath = self.Properties.GetValue("deletePath",frame)
        zIndex = self.Properties.GetValue("zIndex",frame)
        detail = self.Properties.GetValue("detail", frame)/10
        fitThickness = self.Properties.GetValue("fitThickness", frame)

        # general objects
        if dataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            shapeGroup = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)
        else:
            shapeGroup = MotioShapeGroup()

        sampler = SmartBezierSampler()

        selection = self.Properties.GetValue("selection", frame)
        if selection == "All paths":
            pathToStroke = [path for path in pathGroup]
        else:
            pathToStroke = [pathGroup[int(selection)]]

        for path in pathToStroke:
            sampledPoints = self.smartSamplePath(path, sampler, detail)
            strokeShape = self.strokePath(path, sampledPoints, cap, thicknessStart, thicknessEnd, fitThickness, cropStart, cropEnd)

            strokeShape.zIndex = zIndex
            Q_Helper.colorShape(strokeShape, color)
            shapeGroup.Add(strokeShape)
            if deletePath:
                pathGroup.Remove(path)

        dataFeed.SetChannelData(Node.POLYGON_CHANNEL, shapeGroup)

    def strokePath(self, path, sampledPoints, strokeCap, thicknessStart, thicknessEnd, fitThickness, cropStart, cropEnd):
        #conform pos and calculate thickness per point
        pointPos = []
        mixPercent = []
        pathLength = path.PathLength
        previousLength = 0
        for i in range(0, len(sampledPoints), 2):
            currentLength = path.DistanceToNext(i/2)
            for j in range(len(sampledPoints[i])-1):
                pointPos.append(sampledPoints[i][j])
                mixPercent.append((sampledPoints[i+1][j]*currentLength+previousLength)/pathLength)
            previousLength += currentLength
        #last point
        pointPos.append(sampledPoints[i][j+1])
        mixPercent.append(1)

        # crop path
        if cropStart != 0 or cropEnd != 100:
            for i in range(len(pointPos)-1, -1, -1):
                if not cropStart <= mixPercent[i]*100 <= cropEnd:
                    pointPos.remove(pointPos[i])
                    mixPercent.remove(mixPercent[i])
                elif fitThickness:
                    mixPercent[i] = Q_Helper.fitRange(cropStart/100, cropEnd/100, 0, 1, mixPercent[i])
            #add first and last point precisely
            preciseFirstPoint = path.PointAtPercent(cropStart/100, True)
            if preciseFirstPoint:
                pointPos.insert(0, preciseFirstPoint)
                mixPercent.insert(0, 0.0 if fitThickness else cropStart/100)
            preciseSecondPoint = path.PointAtPercent(cropEnd/100, True)
            if preciseSecondPoint:
                pointPos.append(preciseSecondPoint)
                mixPercent.append(1.0 if fitThickness else cropEnd/100)

        stroke = StrokeTracerShape()
        stroke.Stroke(pointPos, [ConvertToFloat(mix*thicknessEnd + (1-mix)*thicknessStart) for mix in mixPercent], strokeCap, path.Closed)
        return stroke.MotioShape


    def smartSamplePath(self, path, sampler, detail):
        #smart sampling
        shapePoints = []
        shapePointsSet = set()
        firstPoint = path.Points[0]
        init = False
        currentPoint = firstPoint
        nextPoint = firstPoint.NextPoint
        while nextPoint:
            samplerResult = sampler.SampleCurve(currentPoint.Position, currentPoint.RightHandle+currentPoint.Position, nextPoint.LeftHandle+nextPoint.Position, nextPoint.Position, detail)
            for result in samplerResult:
                if result not in shapePointsSet:
                    shapePoints.append(result)
                    shapePointsSet.add(result)
            currentPoint = nextPoint
            nextPoint = currentPoint.NextPoint
            if currentPoint == firstPoint and init:
                break
            init = True
        return shapePoints