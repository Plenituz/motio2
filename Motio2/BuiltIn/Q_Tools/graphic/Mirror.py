from Motio.Geometry import Vector2, Vertex
from Motio.Meshing import MotioShape, MotioShapeGroup
from Motio.NodeCore import Node
from Motio.Boolean import BooleanOperation, PolyFillType, ClipType
from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import VectorNodeProperty, DropdownNodeProperty, BoolNodeProperty
from Motio.NodeImpl.NodeTools import MoveTool
import clr
import Motio.NodeCore.Utils
clr.ImportExtensions(Motio.NodeCore.Utils)


class Mirror(BaseClass):
    classNameStatic = "Mirror"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_node(self):
        self.PassiveTools.Add(MoveTool(self,"center")) #add move gizmo

    def setup_properties(self):
        # center
        centerProp = VectorNodeProperty(self, "From where the mirror operation are performing", "Center")
        self.Properties.Add("center", centerProp, Vector2(0,0))
        #vertical or horizontal
        vhProp = DropdownNodeProperty(self, "How to flip shapes", "Orientation", ["Horizontal", "Vertical"])
        self.Properties.Add("vh", vhProp, "Vertical")
        #reverse
        reverseProp = BoolNodeProperty(self, "Invert the sides", "Reverse")
        self.Properties.Add("reverse", reverseProp, False)
        # action
        actionProp = DropdownNodeProperty(self, "Choose what to do with the shapes", "Action",
                                          ["Modify", "Duplicate", "Cut"])
        self.Properties.Add("action", actionProp, "Cut")

    def evaluate_frame(self, frame, dataFeed):
        # INPUT CHECK
        if not dataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            return
        shapeGroup = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)

        # Properties
        self.Properties.WaitForProperty("action")
        center = self.Properties.GetValue("center", frame)
        vh = self.Properties.GetValue("vh", frame)
        reverse = self.Properties.GetValue("reverse", frame)
        action = self.Properties.GetValue("action", frame)

        # shape work
        # MODIFY
        if action == "Modify":
            for shape in shapeGroup:
                self.mirrorShape(shape, center, vh)


        # DUPLICATE
        elif action == "Duplicate":
            newShapes = []
            for shape in shapeGroup:
                newShape = shape.Clone()
                self.mirrorShape(newShape, center, vh)
                newShapes.append(newShape)
            for shape in newShapes:
                shapeGroup.Add(shape)
            dataFeed.SetChannelData(Node.POLYGON_CHANNEL, shapeGroup)


        # CUT
        else:
            # generate cuting shape
            left, right, top, bottom = self.getScreenSize()
            if vh == "Vertical":
                cornerCut = [Vector2(right, bottom), Vector2(right, top)] if reverse else [Vector2(left, bottom), Vector2(left, top)]
                cornerCut += [Vector2(center.X, top), Vector2(center.X, bottom)]
            else:
                cornerCut = [Vector2(left, bottom), Vector2(right, bottom)] if reverse else [Vector2(left, top),
                                                                                             Vector2(right, top)]
                cornerCut += [Vector2(right, center.Y), Vector2(left, center.Y)]
            shapeCut = MotioShape()
            shapeCut.vertices = [Vertex(p) for p in cornerCut]
            shapeGroupCut = MotioShapeGroup(shapeCut)

            # cut shape
            success, newShapeGroup = BooleanOperation.Execute(shapeGroup, shapeGroupCut, PolyFillType.EvenOdd, ClipType.Intersection)
            if not success:
                return

            # clone and mirror shape
            mirroredShapes = []
            for shape in newShapeGroup:
                mirroredShape = shape.Clone()
                self.mirrorShape(mirroredShape, center, vh)
                mirroredShapes.append(mirroredShape)
            for shape in mirroredShapes:
                newShapeGroup.Add(shape)
            dataFeed.SetChannelData(Node.POLYGON_CHANNEL, newShapeGroup)


    def getScreenSize(self):
        timeline = self.GetTimeline()
        height = timeline.CameraHeight
        width = timeline.CameraWidth
        left = -width / 2
        right = width / 2
        top = height / 2
        bottom = -height / 2
        return left, right, top, bottom

    def mirrorCoord(self, vertexPos, center, vh):
        if vh == "Vertical":
            return Vector2(vertexPos.X + (center.X - vertexPos.X) * 2, vertexPos.Y)
        else:
            return Vector2(vertexPos.X, vertexPos.Y + (center.Y - vertexPos.Y) * 2)

    def verticalSort(self, pos, center):
        return pos.X > center.X

    def horizontalSort(self, pos, center):
        return pos.Y > center.Y

    def mirrorShape(self, shape, center, vh):
        for hole in shape.holes:
            self.mirrorShape(hole, center, vh)
        for i in range(shape.vertices.Count):
            vertex = shape.vertices[i]
            vertex.SetPos(self.mirrorCoord(vertex.position, center, vh))
            shape.vertices[i] = vertex