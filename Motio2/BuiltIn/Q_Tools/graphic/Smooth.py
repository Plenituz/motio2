from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import IntNodeProperty, BoolNodeProperty
from Motio.Geometry import Vertex
from Motio.NodeCore import Node

class Smooth(BaseClass):
    classNameStatic = "Smooth"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_properties(self):
        #iteration
        iterationProp = IntNodeProperty(self, "How many smoothing iteration", "Iteration")
        iterationProp.SetRangeFrom(0, True)
        iterationProp.SetRangeTo(4, False)
        self.Properties.Add("iteration", iterationProp, 1)
        #only subdiv
        subdivProp = BoolNodeProperty(self, "Just subdivide the mesh without smoothing it", "Only subdivide")
        self.Properties.Add("subdiv", subdivProp, False)

    def evaluate_frame(self, frame, dataFeed):
        # INPUT CHECK
        if not dataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            return
        shapeGroup = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)

        # PROPERTIES
        self.Properties.WaitForProperty("subdiv")
        iteration = self.Properties.GetValue("iteration",frame)
        subdivOnly = self.Properties.GetValue("subdiv", frame)

        # MESH WORK
        for loop in range(iteration):
            for shape in shapeGroup:
               self.smooth(shape, subdivOnly)

    def smooth(self, shape, subdivOnly):
        # hole in shape
        for holeShape in shape.holes:
            self.smooth(holeShape, subdivOnly)

        # actual smooth
        newVertices = []
        vCount = shape.vertices.Count
        for i in range(vCount):
            prev = i - 1 if i > 0 else vCount - 1
            next = i + 1 if i < vCount - 1 else 0
            # points from old shape
            if not subdivOnly:
                pointAvg = shape.vertices[i].position * 0.75
                pointAvg += shape.vertices[prev].position / 8
                pointAvg += shape.vertices[next].position / 8
                v = shape.vertices[i]
                v.SetPos(pointAvg)
                newVertices.append(v)
            else:
                newVertices.append(shape.vertices[i])

            # new points
            curV = shape.vertices[i]
            nextV = shape.vertices[next]
            pointAvg = (curV.position + nextV.position) / 2
            colorAvg = (curV.color + nextV.color) / 2
            normalAvg = (curV.normal + nextV.normal) / 2
            uvAvg = (curV.uv + nextV.uv) / 2
            newVertices.append(Vertex(pointAvg, colorAvg, normalAvg, uvAvg))

        shape.vertices = newVertices