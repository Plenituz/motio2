from Motio.Geometry import Quaternion, Vector3, Vector2, Vector4, Vertex
from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase
from Motio.NodeImpl.NodePropertyTypes import FloatNodeProperty, BoolNodeProperty, ColorNodeProperty, \
    SeparatorNodeProperty, DropdownNodeProperty
from Motio.NodeCore import Node
from Motio.Graphics import Color
from Motio.Meshing import StrokeTracerShape, MotioShape, MotioShapeGroup
from math import pi
from Q_Tools import Q_Helper
from Motio.NodeCommon.ToolBox import ConvertToFloat
from System import Enum


class Specular(PyGraphicsAffectingNodeBase):
    classNameStatic = "Specular"
    def __new__(cls, *args):
        return PyGraphicsAffectingNodeBase.__new__(cls, *args)

    def setup_properties(self):
        # position
        facingProp = FloatNodeProperty(self, "How much the spec is far from the border", "Facing")
        facingProp.SetRangeFrom(0, False)
        facingProp.SetRangeTo(4, False)
        self.Properties.Add("facing", facingProp, 1.5)
        directionProp = FloatNodeProperty(self, "Where is the spec around the mesh (in degree)", "Light direction (degree)")
        directionProp.SetRangeFrom(0, True)
        directionProp.SetRangeTo(360, True)
        self.Properties.Add("direction", directionProp, 220)
        # size
        thicknessProp = FloatNodeProperty(self, "How thick is the spec", "Width")
        thicknessProp.SetRangeFrom(0, True)
        thicknessProp.SetRangeTo(10, False)
        self.Properties.Add("thickness", thicknessProp, 0.25)
        lenghtProp = FloatNodeProperty(self, "How long is the spec", "Length")
        lenghtProp.SetRangeFrom(0, True)
        lenghtProp.SetRangeTo(1, True)
        self.Properties.Add("length", lenghtProp, 0.1)
        allCaps = [cap for cap in Enum.GetNames(StrokeTracerShape.EndCap)]
        allCaps.insert(0, "Tapered")
        styleProp = DropdownNodeProperty(self, "Choose what style of specular", "Style", allCaps)
        self.Properties.Add("style", styleProp, allCaps[0])

        self.Properties.AddManually('separator1', SeparatorNodeProperty(self))

        colorProp = ColorNodeProperty(self, "Color of the specular", "Color")
        self.Properties.Add("color", colorProp, Color.White)
        opacityProp = FloatNodeProperty(self, "Opacity of the specular", "Opacity")
        opacityProp.SetRangeFrom(0, True)
        opacityProp.SetRangeTo(1, True)
        self.Properties.Add("opacity", opacityProp, 1)
        onlyProp = BoolNodeProperty(self, "Output only the specular, removing the original shape", "Specular only")
        self.Properties.Add("only", onlyProp, False)

    def evaluate_frame(self, frame, dataFeed):
        # input check
        if not dataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            return
        shapeGroup = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)

        # properties
        self.Properties.WaitForProperty("color")
        facing = self.Properties.GetValue("facing", frame)
        direction = self.Properties.GetValue("direction", frame)
        thickness = self.Properties.GetValue("thickness", frame)
        length = self.Properties.GetValue("length", frame)*2-1
        color = self.Properties.GetValue("color", frame).ToVector4()
        opacity = self.Properties.GetValue("opacity", frame)
        only = self.Properties.GetValue("only", frame)
        style = self.Properties.GetValue("style", frame)


        if length == -1:
            return

        # direction vector
        quaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, pi / 180 * direction)
        moveVector = Vector2.Transform(Vector2.UnitX, quaternion)

        shapesToAdd = []
        for shape in shapeGroup:
            #normals
            if shape.ShouldCalculateNormals():
                shape.CalculateNormals()

            # train generation
            trains = []
            train = []
            for i in range(shape.vertices.Count):
                v = shape.vertices[i]
                test = Vector2.Dot(v.normal, moveVector) < length
                if test:
                    train.append(i)
                elif len(train) != 0:
                    trains.append(train)
                    train = []
            if len(train)!=0:
                trains.append(train)

            #merge first and last
            trainsNum = len(trains) - 1
            if trainsNum > 0:
                lastTrainNum = len(trains[trainsNum]) - 1
                if trains[0][0] == 0 and trains[trainsNum][lastTrainNum] == shape.vertices.Count - 1:
                    trains[0] = trains.pop() + trains[0]

            for train in trains:
                #add midPoints
                firstPoint = shape.vertices[train[0]]
                firstPointTest = Vector2.Dot(firstPoint.normal, moveVector)
                firstPointNeighbour = train[0]-1 if train[0] != 0 else shape.vertices.Count-1
                firstPointNeighbour = shape.vertices[firstPointNeighbour]
                firstPointNeighbourTest = Vector2.Dot(firstPointNeighbour.normal, moveVector)
                firstRatio = (length - firstPointNeighbourTest) / (firstPointTest - firstPointNeighbourTest)


                lastPoint = shape.vertices[train[len(train)-1]]
                lastPointTest = Vector2.Dot(lastPoint.normal, moveVector)
                lastPointNeighbour = train[len(train)-1]+1 if train[len(train)-1] != shape.vertices.Count-1 else 0
                lastPointNeighbour = shape.vertices[lastPointNeighbour]
                lastPointNeighbourTest = Vector2.Dot(lastPointNeighbour.normal, moveVector)
                lastRatio = (length - lastPointNeighbourTest) / (lastPointTest - lastPointNeighbourTest)

                firstVertexMid = Q_Helper.VertexLerp(firstPointNeighbour, firstPoint, firstRatio)
                lastVertexMid = Q_Helper.VertexLerp(lastPointNeighbour, lastPoint, lastRatio)

                train = [shape.vertices[i] for i in train]
                train.insert(0, firstVertexMid)
                train.append(lastVertexMid)

                #stroke trains
                tracer = StrokeTracerShape()
                trainPos = [v.position + v.normal*-facing for v in train]
                if style == "Tapered":
                    pNumber = (len(train) - 1) / 2.0
                    trainThickness = [ConvertToFloat(thickness * i/pNumber) if i <= pNumber else ConvertToFloat(thickness * (1-(i-pNumber)/pNumber)) for i in range(len(train))]
                    strokeType = StrokeTracerShape.EndCap.Square
                else:
                    trainThickness = [thickness for i in train]
                    strokeType = Enum.Parse(StrokeTracerShape.EndCap, style)
                tracer.Stroke(trainPos, trainThickness, strokeType, False)
                newShape = tracer.MotioShape
                newShape.zIndex = shape.zIndex + 1
                shapesToAdd.append(tracer.MotioShape)

        if only:
            shapeGroup = MotioShapeGroup()
        #add stroke to shapeGroup + color
        for shape in shapesToAdd:
            self.applyColor(shape, color, opacity)
            shapeGroup.Add(shape)
        dataFeed.SetChannelData(Node.POLYGON_CHANNEL, shapeGroup)

    def applyColor(self, shape, color, opacity):
        for i in range(shape.vertices.Count):
            v = shape.vertices[i]
            v.SetColor(Vector4(color.X, color.Y, color.Z, opacity))
            shape.vertices[i] = v