from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import FloatNodeProperty, DropdownNodeProperty
from Motio.Meshing import EdgeSet, MotioShape, MotioShapeGroup, MotioShape2Mesh
from Motio.Geometry import Vector2, Vector4, Vertex
from Motio.NodeCore import Node
from math import pi, cos, sin

class Debug(BaseClass):
    classNameStatic = "Debug"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_properties(self):
        #radius
        radiusProp = FloatNodeProperty(self, "Points's radius", "Radius")
        radiusProp.SetRangeFrom(0.01, True)
        radiusProp.SetRangeTo(2, False)
        self.Properties.Add("radius", radiusProp, 0.3)
        #thickness
        thicknessProp = FloatNodeProperty(self, "Edge's thickness", "Thickness")
        thicknessProp.SetRangeFrom(0.01, True)
        thicknessProp.SetRangeTo(2, False)
        self.Properties.Add("thickness", thicknessProp, 0.2)
        #meshPath
        meshPathProp = DropdownNodeProperty(self, "Choose what input channel to debug","Debug", ["Path", "Polygon", "Mesh"])
        self.Properties.Add("meshPath", meshPathProp, "Polygon")

    def evaluate_frame(self, frame, dataFeed):
        # properties
        self.Properties.WaitForProperty("meshPath")
        radius = self.Properties.GetValue("radius", frame)
        thickness = self.Properties.GetValue("thickness", frame)
        meshPath = self.Properties.GetValue("meshPath", frame)

        # mesh gen
        shapeGroup = MotioShapeGroup()

        if meshPath == "Mesh":
            shapeGroupInput = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)
            if not shapeGroupInput:
                return

            # MESH DEBUG
            for shape in shapeGroupInput:
                meshGroup = MotioShape2Mesh.Convert(shape)
                for mesh in meshGroup:
                    mesh.BakeTransform()
                    # circles on mesh points
                    for vertex in mesh.vertices:
                        shapeGroup.Add(self.createCircle(radius, vertex.position, 6, vertex.color))
                    # line on mesh edges
                    edges = set()
                    for i in range(0,len(mesh.triangles),3):
                        edges.add(EdgeSet(mesh.triangles[i], mesh.triangles[i+1]))
                        edges.add(EdgeSet(mesh.triangles[i+1], mesh.triangles[i+2]))
                        edges.add(EdgeSet(mesh.triangles[i+2], mesh.triangles[i]))
                    for edge in edges:
                        shapeGroup.Add(self.createLine(mesh.vertices[edge.indexFirst].position, mesh.vertices[edge.indexSecond].position, thickness))

        elif meshPath == "Polygon":
            shapeGroupInput = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)
            if not shapeGroupInput:
                return
            # SHAPE DEBUG
            for shape in shapeGroupInput:
                self.debugShape(shape, shapeGroup, thickness, radius)

        else:
            pathGroupInput = dataFeed.GetChannelData(Node.PATH_CHANNEL)
            if not pathGroupInput:
                return
            # PATH DEBUG
            for path in pathGroupInput:
                # circles on path points
                for point in path.Points:
                    shapeGroup.Add(self.createCircle(radius, point.Position, 6))

        dataFeed.SetChannelData(Node.POLYGON_CHANNEL, shapeGroup)


    def debugShape(self, shape, shapeGroup, thickness, radius):
        if shape.holes.Count != 0:
            for hole in shape.holes:
                self.debugShape(hole, shapeGroup, thickness, radius)

        shape.BakeTransform()
        for i in range(shape.vertices.Count):
            if i < shape.vertices.Count - 1:
                shapeGroup.Add(self.createArrow(shape.vertices[i].position, shape.vertices[i + 1].position, thickness))
            else:
                shapeGroup.Add(
                    self.createArrow(shape.vertices[i].position, shape.vertices[0].position, thickness))
            shapeGroup.Add(self.createCircle(radius, shape.vertices[i].position, 6, shape.vertices[i].color))

    def createCircle(self, radius, pos, detail, color = Vector4(0,0,0,1)):
        # points
        vertices = []
        circleDivision = 2*pi/detail
        for i in range(detail):
            vertices.append(Vertex(
                Vector2(
                    pos.X + radius * cos(circleDivision*i),
                    pos.Y + radius * sin(circleDivision*i)
                ),
                color
            ))

        # normals
        for i in range(len(vertices)):
            vertices[i].SetNormal(Vector2.Normalize(vertices[i].position-pos))

        shape = MotioShape()
        shape.vertices = vertices
        return shape

    def createLine(self, pos1, pos2, thickness):
        # calculate normal vector in both ways
        normal1 = Vector2(pos2.Y - pos1.Y, pos1.X - pos2.X)
        normal1.Normalize()
        normal1 *= thickness / 2
        normal2 = - normal1

        # points
        color = Vector4(0, 0, 0, 1)
        vertices = [
            Vertex(pos1 + normal2, color),
            Vertex(pos1 + normal1, color),
            Vertex(pos2 + normal1, color),
            Vertex(pos2 + normal2, color)
        ]
        # normals
        vertices[0].SetNormal(Vector2.Normalize((pos2 - pos1) + normal1))
        vertices[1].SetNormal(Vector2.Normalize((pos2 - pos1) + normal2))
        vertices[2].SetNormal(Vector2.Normalize((pos1 - pos2) + normal1))
        vertices[3].SetNormal(Vector2.Normalize((pos1 - pos2) + normal2))

        newShape = MotioShape()
        newShape.vertices = vertices
        return newShape

    def createArrow(self, pos1, pos2, thickness):
        # calculate normal vector in both ways
        normal1 = Vector2(pos2.Y - pos1.Y, pos1.X - pos2.X)
        normal1.Normalize()
        normal1 *= thickness / 2
        normal2 = - normal1

        distance = Vector2.Distance(pos1,pos2)
        arrowDistance = (distance - 1) / distance if distance > 1 else 0


        # points
        color = Vector4(0, 0, 0, 1)
        vertices = [
            Vertex(pos1 + normal2, color),
            Vertex(pos1 + normal1, color),
            Vertex((pos2 * arrowDistance) + (pos1 * (1-arrowDistance)) + normal1, color),
            Vertex((pos2 * arrowDistance) + (pos1 * (1-arrowDistance)) + normal1 * 2, color),
            Vertex(pos2, color),
            Vertex((pos2 * arrowDistance) + (pos1 * (1-arrowDistance)) + normal2 * 2, color),
            Vertex((pos2 * arrowDistance) + (pos1 * (1-arrowDistance)) + normal2, color)
        ]

        newShape = MotioShape()
        newShape.vertices = vertices
        return newShape