from Motio.Geometry import Vector2, Quaternion, Vector3, Vector4
from Motio.NodeCore import Node
from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import FloatNodeProperty, DropdownNodeProperty, SeparatorNodeProperty, \
    ColorNodeProperty, BoolNodeProperty
from Motio.Graphics import Color
from Q_Tools import Q_Helper
from math import pi
from Motio.NodeCommon.ToolBox import CreateMatrix
from Motio.Boolean import BooleanOperation, PolyFillType, ClipType
from Motio.Meshing import MotioShapeGroup


class Shadow(BaseClass):
    classNameStatic = "Shadow"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_properties(self):
        typeProp = DropdownNodeProperty(self, "Add shadow on the object or the background", "Type", ["Drop shadow simple", "Drop shadow", "Drop shadow extend", "Self shadow", "Self shadow simple"])
        self.Properties.Add("type", typeProp, "Drop shadow extend")
        rotationProp = FloatNodeProperty(self, "Rotation of the shadow", "Rotation (in degree)")
        rotationProp.SetRangeFrom(0, True)
        rotationProp.SetRangeTo(360, True)
        self.Properties.Add("rotation", rotationProp, 45)
        distanceProp = FloatNodeProperty(self, "How far is the shadow from the shape", "Distance")
        distanceProp.SetRangeFrom(0, False)
        distanceProp.SetRangeTo(2, False)
        self.Properties.Add("distance", distanceProp, 1)
        sizeProp = FloatNodeProperty(self, "Scale the shadow", "Size")
        sizeProp.SetRangeFrom(0, False)
        sizeProp.SetRangeTo(2, False)
        self.Properties.Add("size", sizeProp, 1)

        self.Properties.AddManually('separator1', SeparatorNodeProperty(self))

        colorMixProp = FloatNodeProperty(self, "Mix the original shape color with the shadow color", "Mix original color")
        colorMixProp.SetRangeFrom(0,True)
        colorMixProp.SetRangeTo(100, True)
        self.Properties.Add("colorMix", colorMixProp, 10)
        colorProp = ColorNodeProperty(self, "Color of the shadow", "Color")
        self.Properties.Add("color", colorProp, Color.SteelBlue)
        opacityProp = FloatNodeProperty(self, "Opacity of the shadow", "Opacity")
        opacityProp.SetRangeFrom(0,True)
        opacityProp.SetRangeTo(1, True)
        self.Properties.Add("opacity", opacityProp, 1)
        onlyProp = BoolNodeProperty(self, "Output only the shadow, removing the original shape", "Shadow only")
        self.Properties.Add("only", onlyProp, False)


    def evaluate_frame(self, frame, dataFeed):
        # check input
        if not dataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            return
        shapeGroup = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)

        # properties
        self.Properties.WaitForProperty("only")
        type = self.Properties.GetValue("type", frame)
        rotation = self.Properties.GetValue("rotation", frame)
        distance = self.Properties.GetValue("distance", frame)
        size = self.Properties.GetValue("size", frame)
        colorMix = self.Properties.GetValue("colorMix", frame)/100.0
        color = self.Properties.GetValue("color", frame).ToVector4()
        opacity = self.Properties.GetValue("opacity", frame)
        only = self.Properties.GetValue("only", frame)

        if only:
            outputShapeGroup = MotioShapeGroup()
        else:
            outputShapeGroup = shapeGroup

        if type == "Self shadow":
            distance = distance if 0<=distance<1 else 1
        newShapeGroup = shapeGroup.Clone()
        quaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, pi / 180 * rotation)
        moveVector = Vector2.Transform(-Vector2.UnitX * distance, quaternion)

        for shape in newShapeGroup:
            if shape.ShouldCalculateNormals():
                shape.CalculateNormals()

        # work
        if type == "Drop shadow simple":
            for shape in newShapeGroup:
                center = Q_Helper.estimatedCenter(shape)
                originalMatrix = shape.transform
                transformMatrix = CreateMatrix(
                    moveVector,
                    Vector3.Zero,
                    Vector2(size, size),
                    Vector2.Zero,
                    center,
                    ["S","R","T"]
                )
                shape.transform = originalMatrix * transformMatrix
                self.applyColor(shape, color, colorMix, opacity)
                self.addZindex(shape, -1)
                outputShapeGroup.Add(shape)

        elif type == "Drop shadow":
            for shape in newShapeGroup:
                for i in range(shape.vertices.Count):
                    v = shape.vertices[i]
                    if Vector2.Dot(-v.normal, moveVector)<=0:
                        v.SetPos(v.position + moveVector)
                        shape.vertices[i] = v
                    self.applyColor(shape, color, colorMix, opacity)
                    self.addZindex(shape, -1)
                    outputShapeGroup.Add(shape)

        elif type == "Drop shadow extend":
            for shape in newShapeGroup:
                for i in range(shape.vertices.Count):
                    v = shape.vertices[i]
                    dotProduct = Vector2.Dot(-v.normal, moveVector)
                    if dotProduct <=0:
                        v.SetPos(v.position + moveVector*abs(dotProduct)/distance)
                        shape.vertices[i] = v
                    self.applyColor(shape, color, colorMix, opacity)
                    self.addZindex(shape, -1)
                    outputShapeGroup.Add(shape)

        elif type == "Self shadow":
            for shape in newShapeGroup:
                for i in range(shape.vertices.Count):
                    v = shape.vertices[i]
                    if Vector2.Dot(v.normal, moveVector)<=0:
                        pos = v.position + moveVector*0.01
                        while shape.HitTest(pos):
                            pos += moveVector*0.01
                        v.SetPos(distance * pos + (1-distance)*v.position)
                    shape.vertices[i] = v
                    self.applyColor(shape, color, colorMix, opacity)
                    self.addZindex(shape, 1)
                    outputShapeGroup.Add(shape)
        else:
            for shape in newShapeGroup:
                center = Q_Helper.estimatedCenter(shape)
                originalMatrix = shape.transform
                transformMatrix = CreateMatrix(
                    -moveVector,
                    Vector3.Zero,
                    Vector2(size, size),
                    Vector2.Zero,
                    center,
                    ["S", "R", "T"]
                )
                shape.transform = originalMatrix * transformMatrix
                shape.BakeTransform()
            sucess, newShapeGroup = BooleanOperation.Execute(shapeGroup, newShapeGroup, PolyFillType.EvenOdd, ClipType.Difference)
            if sucess:
                for shape in newShapeGroup:
                    self.applyColor(shape, color, colorMix, opacity)
                    self.addZindex(shape, 1)
                    outputShapeGroup.Add(shape)

        dataFeed.SetChannelData(Node.POLYGON_CHANNEL, outputShapeGroup)


    def applyColor(self, shape, color, colorMix, opacity):
        for hole in shape.holes:
            self.applyColor(hole, color, colorMix, opacity)
        for i in range(shape.vertices.Count):
            v = shape.vertices[i]
            # TODO : faire color lerp
            v.SetColor(Vector4.Lerp(Vector4(color.X, color.Y, color.Z, opacity), Vector4(v.color.X, v.color.Y, v.color.Z, opacity), colorMix))
            shape.vertices[i] = v

    def addZindex(self, shape, zIndexOffset):
        for hole in shape.holes:
            self.addZindex(hole, zIndexOffset)
        shape.zIndex += zIndexOffset