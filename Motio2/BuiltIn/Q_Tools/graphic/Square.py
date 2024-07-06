from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import VectorNodeProperty, ColorNodeProperty, DropdownNodeProperty, IntNodeProperty
from Motio.NodeImpl.NodePropertyTypes import FloatNodeProperty, SeparatorNodeProperty
from Motio.Meshing import MotioShape, MotioShapeGroup
from Motio.Geometry import Vector2, Vector3, Quaternion, Vertex
from Motio.NodeCore import Node
from Motio.Graphics import Color
from Motio.NodeImpl.NodeTools import MoveTool
from math import pi

class Square(BaseClass):
    classNameStatic = "Square"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_node(self):
        self.PassiveTools.Add(MoveTool(self,"pos")) #add move gizmo

    def setup_properties(self):
        #position
        posProp = VectorNodeProperty(self, "Position of the square", "Position")
        self.Properties.Add("pos", posProp, Vector2(0, 0))
        #size
        sizeProp = VectorNodeProperty(self, "Size of the square", "Size")
        self.Properties.Add("size", sizeProp, Vector2(5,5))
        self.Properties["size"].uniform = True
        #roundCorner
        roundProp = FloatNodeProperty(self, "Round the 4 square corners", "Round corners (%)")
        roundProp.SetRangeFrom(0, True)
        roundProp.SetRangeTo(100, True)
        self.Properties.Add("roundCorner", roundProp, 0)
        #round detail
        detailProp = IntNodeProperty(self, "Detail of the rounded corners", "Round corners detail")
        detailProp.SetRangeFrom(0,True)
        detailProp.SetRangeTo(32, False)
        self.Properties.Add("detail", detailProp, 3)
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
        # properties
        self.Properties.WaitForProperty("action")
        size = self.Properties.GetValue("size", frame)
        pos = self.Properties.GetValue("pos", frame)
        color = self.Properties.GetValue("color", frame).ToVector4()
        action = self.Properties.GetValue("action",frame)
        zIndex = self.Properties.GetValue("zIndex",frame)
        roundCorner = self.Properties.GetValue("roundCorner",frame)

        # shape gen
        if roundCorner == 0:
            #simple square
            vertices = self.createSquare(pos, color, size)
        else:
            #rounded square
            detail = self.Properties.GetValue("detail", frame)
            vertices = self.createRoundedSquare(pos, color, size, roundCorner, detail)

        # output
        shape = MotioShape()
        shape.vertices = vertices
        shape.UpdateNormalsGeneration()
        shape.zIndex = zIndex

        shapeGroupInput = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)
        if action == "Merge" and shapeGroupInput:
            shapeGroupInput.Add(shape)
            shapeGroup = shapeGroupInput
        else:
            shapeGroup = MotioShapeGroup(shape)

        dataFeed.SetChannelData(Node.POLYGON_CHANNEL, shapeGroup)

    def createSquare(self, pos, color, size):
        # points
        vertices = [
            Vertex(Vector2(size.X/2, size.Y/2)+pos,color),
            Vertex(Vector2(-size.X/2, size.Y/2)+pos,color),
            Vertex(Vector2(-size.X/2, -size.Y/2)+pos,color),
            Vertex(Vector2(size.X/2, -size.Y/2)+pos,color)
        ]

        # normals
        vertices[0].SetNormal(Vector2.Normalize(Vector2(size.Y, size.X)))
        vertices[1].SetNormal(Vector2.Normalize(Vector2(-size.Y, size.X)))
        vertices[2].SetNormal(Vector2.Normalize(Vector2(-size.Y, -size.X)))
        vertices[3].SetNormal(Vector2.Normalize(Vector2(size.Y, -size.X)))

        return vertices

    def createRoundedSquare(self, pos, color, size, roundCorner, detail):
        # points
        if size.X > size.Y:
            xOffset = size.X - (roundCorner/100.0)*size.Y
            yOffset = size.Y - (roundCorner/100.0)*size.Y
        else:
            xOffset = size.X - (roundCorner/100.0)*size.X
            yOffset = size.Y - (roundCorner/100.0)*size.X

        cornerCenter = [
            Vector2(xOffset / 2, yOffset / 2) + pos,
            Vector2(-xOffset / 2, yOffset / 2) + pos,
            Vector2(-xOffset / 2, -yOffset / 2) + pos,
            Vector2(xOffset / 2, -yOffset / 2) + pos
        ]

        vertices = [
            # outer squares
            Vertex(Vector2(size.X/2, yOffset/2)+pos, color),
            Vertex(Vector2(xOffset/2, size.Y/2)+pos, color),
            Vertex(Vector2(-xOffset/2, size.Y/2)+pos, color),
            Vertex(Vector2(-size.X/2, yOffset/2)+pos, color),
            Vertex(Vector2(-size.X/2, -yOffset/2)+pos, color),
            Vertex(Vector2(-xOffset/2, -size.Y/2)+pos, color),
            Vertex(Vector2(xOffset/2, -size.Y/2)+pos, color),
            Vertex(Vector2(size.X/2, -yOffset/2)+pos, color)
        ]

        # normals
        vertices[0].SetNormal(Vector2.Normalize(Vector2(size.X/2, yOffset/2)))
        vertices[1].SetNormal(Vector2.Normalize(Vector2(xOffset/2, size.Y/2)))
        vertices[2].SetNormal(Vector2.Normalize(Vector2(-xOffset/2, size.Y/2)))
        vertices[3].SetNormal(Vector2.Normalize(Vector2(-size.X/2, yOffset/2)))
        vertices[4].SetNormal(Vector2.Normalize(Vector2(-size.X/2, -yOffset/2)))
        vertices[5].SetNormal(Vector2.Normalize(Vector2(-xOffset/2, -size.Y/2)))
        vertices[6].SetNormal(Vector2.Normalize(Vector2(xOffset/2, -size.Y/2)))
        vertices[7].SetNormal(Vector2.Normalize(Vector2(size.X/2, -yOffset/2)))
        
        # ROUNDED CORNERS
        if detail != 0: # not adding points
            newVertices = []
            for i in range(4):
                # corner's points
                newVertices += self.createRoundedCorner(detail, color, cornerCenter[i], vertices[i*2].position)
                # corner's normals
                for j in range(len(newVertices)):
                    newVertices[j].SetNormal(Vector2.Normalize(newVertices[j].position - cornerCenter[i]))
            for i in range(4):
                newVertices.insert(i*detail + i*2, vertices[i*2])
                newVertices.insert((i+1)*detail + i*2 + 1, vertices[i*2+1])
            return newVertices
        else:
            return vertices

    def createRoundedCorner(self, detail, color, centerPos, startPos):
        newVertices = []
        quaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (pi/2)/(detail+1))
        vectorRotated = startPos-centerPos
        for i in range(detail):
            vectorRotated = Vector2.Transform(vectorRotated, quaternion)
            newVertices.append(Vertex(vectorRotated+centerPos, color))
        return newVertices
