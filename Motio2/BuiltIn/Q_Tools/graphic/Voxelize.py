from Motio.Meshing import MotioShape, MotioShapeGroup
from Motio.NodeCore import Node
from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import VectorNodeProperty
from Motio.Geometry import BoundingBox2D, Vector2, Vertex
from Q_Tools.Volume import Volume

class Voxelize(BaseClass):
    classNameStatic = "Voxelize"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_properties(self):
        sizeProp = VectorNodeProperty(self, "Size of a box", "Voxel size")
        sizeProp.uniform = True
        self.Properties.Add("size", sizeProp, Vector2(1,1))

    def evaluate_frame(self, frame, dataFeed):
        # input check
        if not dataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            return
        shapeGroup = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)

        # properties
        self.Properties.WaitForProperty("size")
        size = self.Properties.GetValue("size", frame)
        sizeProp = self.Properties["size"]
        if size.X <= 0 or size.Y <= 0:
            sizeProp.SetError(1, "Size must be greater than 0")
            return
        else:
            sizeProp.ClearError(1)

        newShapeGroup = MotioShapeGroup()

        for shape in shapeGroup:
            shape.BakeTransform()
            newShape = self.voxelizeShape(shape, size)
            if not newShape:
                return
            newShapeGroup.Add(newShape)

        dataFeed.SetChannelData(Node.POLYGON_CHANNEL, newShapeGroup)

    def voxelizeShape(self, shape, size):
        # holes voxelization
        newHoles = []
        for hole in shape.holes:
            newHole = self.voxelizeShape(hole, size)
            if newHole:
                newHoles.append(newHole)

        # convert shape to volume
        volume = Volume(size, shape)

        # get one of the border voxel and start drawing from it
        randomBorderVoxel = None
        for voxel in volume.voxels:
            if voxel.isPartiallyFilled():
                randomBorderVoxel = voxel
                break
        if not randomBorderVoxel:
            return

        # draw the voxelize shape starting at a random border voxel from neighbour to neighbour
        self.firstBorderVoxel = None
        self.pointList = []
        color = shape.vertices[0].color
        newShape = self.drawVoxel(randomBorderVoxel, None, size, color)
        if not newShape:
            return
        newShape.holes = newHoles
        return newShape

    def drawVoxel(self, voxel, previousVoxel, size, color):
        if not self.firstBorderVoxel:
            # first voxel
            self.firstBorderVoxel = voxel
            # set arbitrary previous voxel
            for side in ["upVox","rightVox","leftVox","downVox"]:
                exec("sideVoxel = voxel." + side)
                if sideVoxel:
                    if sideVoxel.isPartiallyFilled():
                        previousVoxel = sideVoxel

        elif self.firstBorderVoxel == voxel:
            # last voxel
            return self.optimAndBuildShape(color)

        # count number of active corners
        activeCorners = voxel.estimateDensity()*4
        # this is a corner, add a point in center of voxel
        if activeCorners == 3 or activeCorners == 1:
            self.pointList.append(voxel.position)

        # SEARCH exiting side of the voxel taking account of previous voxel
        # exiting left
        if previousVoxel != voxel.leftVox and voxel.downLeftCorner != voxel.upLeftCorner:
            self.pointList.append(Vector2(voxel.position.X - size.X/2, voxel.position.Y))
            nextVoxel = voxel.leftVox

        # exiting right
        elif previousVoxel != voxel.rightVox and voxel.downRightCorner != voxel.upRightCorner:
            self.pointList.append(Vector2(voxel.position.X + size.X/2, voxel.position.Y))
            nextVoxel = voxel.rightVox

        # exiting up
        elif previousVoxel != voxel.upVox and voxel.upRightCorner != voxel.upLeftCorner:
            self.pointList.append(Vector2(voxel.position.X, voxel.position.Y + size.Y/2))
            nextVoxel = voxel.upVox

        # exiting down
        elif previousVoxel != voxel.downVox and voxel.downLeftCorner != voxel.downRightCorner:
            self.pointList.append(Vector2(voxel.position.X, voxel.position.Y - size.Y/2))
            nextVoxel = voxel.downVox

        # draw next neighbouring voxel
        return self.drawVoxel(nextVoxel, voxel, size, color)

    def optimAndBuildShape(self, color):
        # remove points on same line
        currentIndex = 0
        while currentIndex < len(self.pointList) - 2:
            p1 = self.pointList[currentIndex]
            p2 = self.pointList[currentIndex + 1]
            p3 = self.pointList[currentIndex + 2]
            if p1.X == p2.X == p3.X:
                self.pointList.remove(p2)
            elif p1.Y == p2.Y == p3.Y:
                self.pointList.remove(p2)
            else:
                currentIndex += 1

        # create the motioShape
        shape = MotioShape()
        shape.vertices = [Vertex(p, color) for p in self.pointList]
        return shape