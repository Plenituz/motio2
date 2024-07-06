import time
from Motio.Geometry import BoundingBox2D, Vector2, Vertex, Vector4, ContainmentType
from Motio.Meshing import MotioShape

class Volume:
    def __init__(self, resolution, shape = None, holes = False):
        """
        Volume representation of a shape using voxels
        :type resolution: Vector2
        :type shape: MotioShape
        """
        self.resolution = resolution
        self.voxelGrid = []
        self.hitGrid = []
        self.minPos = None
        self.maxPos = None

        if shape:
            if holes:
                self.addShapeAndHoles(shape)
            else:
                self.addShape(shape)

    @property
    def voxels(self):
        allVoxels = []
        for voxelRow in self.voxelGrid:
            allVoxels += voxelRow
        return allVoxels

    def addShape(self, shape):
        # were to hitTest
        hitPosX, hitPosY = self.generateHitPos(shape)

        # hit test in a grid
        for x in hitPosX:
            self.hitGrid.append([])
            for y in hitPosY:
                hitPos = Vector2(x,y)
                hit = shape.IsPointInside(hitPos) # hit = shape.HitTest(Vector2(x,y))
                self.hitGrid[len(self.hitGrid)-1].append(HitPoint(hitPos, hit))

        #convert hitGrid to voxels
        self.hitGridToVoxelGrid()

    def addShapeAndHoles(self, shape):
        # were to hitTest
        hitPosX, hitPosY = self.generateHitPos(shape)

        # hit test in a grid
        for x in hitPosX:
            self.hitGrid.append([])
            for y in hitPosY:
                hitPos = Vector2(x,y)
                hit = shape.HitTest(hitPos) # hit = shape.HitTest(Vector2(x,y))
                self.hitGrid[len(self.hitGrid)-1].append(HitPoint(hitPos, hit))

        #convert hitGrid to voxels
        self.hitGridToVoxelGrid()

    def generateHitPos(self, shape):
        # shape bbox
        bbox = BoundingBox2D.CreateFromPoints([v.position for v in shape.vertices])
        # convert to volume min pos
        self.minPos = Vector2(bbox.Min.X - (bbox.Min.X % self.resolution.X) - self.resolution.X / 2,
                              bbox.Min.Y - (bbox.Min.Y % self.resolution.Y) - self.resolution.Y / 2)
        # generate pos in bbox
        xPosArray = self.rangeFloat(self.minPos.X, bbox.Max.X, self.resolution.X)
        yPosArray = self.rangeFloat(self.minPos.Y, bbox.Max.Y, self.resolution.Y)
        # last hit pos is volume max pos
        self.maxPos = Vector2(xPosArray[len(xPosArray)-1], yPosArray[len(yPosArray)-1])
        return xPosArray, yPosArray

    def rangeFloat(self, min, max, increment):
        rangeArray = []
        while min < max:
            rangeArray.append(min)
            min += increment
        return rangeArray

    def blur(self, multiplier):
        if multiplier == 0:
            return

        # grow volume with empty voxels
        for i in range(multiplier):
            self.addEmptyHitRing()

        #calculate new value for each hitPoint
        newHitGrid = []
        hitGridSize = Vector2(len(self.hitGrid), len(self.hitGrid[0]))
        for x in range(hitGridSize.X):
            newHitGrid.append([])
            for y in range(hitGridSize.Y):
                # if self.hitGrid[x][y].val == True:
                #     newHitGrid[x].append(self.hitGrid[x][y])
                # else:
                newHitGrid[x].append(self.bluredHitPoint(x, y, hitGridSize, multiplier))

        #fit to 0-1 range
        maxValue = max([hit.val for hit in list(sum(newHitGrid, []))])
        maxValue = 1 / maxValue
        for i in range(len(newHitGrid)):
            for hit in newHitGrid[i]:
                hit.val *= maxValue

        self.hitGrid = newHitGrid
        self.hitGridToVoxelGrid()

    def bluredHitPoint(self, x, y, hitGridSize, multiplier):
        # get all voxels around in a square and there distance
        neighbourHitPoints = []
        neighbourHitDistance = []
        current = self.hitGrid[x][y]
        for localX in range(x - multiplier, x + multiplier + 1):
            for localY in range(y - multiplier, y + multiplier + 1):
                if 0 <= localX <= hitGridSize.X - 1 and 0 <= localY <= hitGridSize.Y - 1:
                    neighbour = self.hitGrid[localX][localY]
                    neighbourHitPoints.append(neighbour)
                    neighbourHitDistance.append(Vector2.Distance(current.position, neighbour.position))
                    # choose between simple and complex distance calculation
                    # neighbourHitDistance.append(abs(localX - x)*self.resolution.X + abs(localY - y)*self.resolution.Y)


        # add val weighted by distance for all voxels
        maxDistance = (multiplier+1)*self.resolution.X + (multiplier+1)*self.resolution.Y
        # choose between simple and complex distance calculation
        # maxDistance = self.resolution * (multiplier+1)
        # maxDistance = max(maxDistance.X, maxDistance.Y)
        newValue = current.val
        divider = 1
        for i in range(len(neighbourHitPoints)):
            weight = 1 - (neighbourHitDistance[i] / maxDistance)
            divider += weight
            newValue +=  weight * neighbourHitPoints[i].val
        return HitPoint(current.position, newValue / divider)

    def addEmptyHitRing(self):
        self.minPos -= self.resolution
        self.maxPos += self.resolution
        for column in self.hitGrid:
            column.append(HitPoint(Vector2(column[0].position.X, self.maxPos.Y), False))
            column.insert(0, HitPoint(Vector2(column[0].position.X, self.minPos.Y), False))
        leftEmptyColumn = [HitPoint(Vector2(hit.position.X - self.resolution.X, hit.position.Y), False) for hit in self.hitGrid[0]]
        self.hitGrid.insert(0, leftEmptyColumn)
        rightEmptyColumn = [HitPoint(Vector2(hit.position.X + self.resolution.X, hit.position.Y), False) for hit in self.hitGrid[len(self.hitGrid)-1]]
        self.hitGrid.append(rightEmptyColumn)

    def hitGridToVoxelGrid(self):
        self.voxelGrid = []
        xLen = len(self.hitGrid)
        yLen = len(self.hitGrid[0])
        for x in range(xLen):
            self.voxelGrid.append([])
            for y in range(yLen):
                position = self.hitGrid[x][y].position + self.resolution / 2
                voxel = Voxel(position)

                # corners
                voxel.downLeftCorner = self.hitGrid[x][y].val
                if x < xLen - 1 and y < yLen - 1:
                    voxel.upRightCorner = self.hitGrid[x + 1][y + 1].val
                    voxel.upLeftCorner = self.hitGrid[x][y + 1].val
                    voxel.downRightCorner = self.hitGrid[x + 1][y].val
                elif x < xLen - 1:
                    voxel.downRightCorner = self.hitGrid[x + 1][y].val
                elif y < yLen - 1:
                    voxel.upLeftCorner = self.hitGrid[x][y + 1].val

                # neighbours
                if x > 0:
                    voxel.leftVox = self.voxelGrid[x - 1][y]
                    self.voxelGrid[x - 1][y].rightVox = voxel
                if y > 0:
                    voxel.downVox = self.voxelGrid[x][y - 1]
                    self.voxelGrid[x][y - 1].upVox = voxel

                self.voxelGrid[x].append(voxel)

    def merge(self, volume1, volume2):
        newVolume = Volume(self.resolution)
        newVolume.minPos = Vector2(min(volume1.minPos.X, volume2.minPos.X), min(volume1.minPos.Y, volume2.minPos.Y))
        newVolume.maxPos = Vector2(max(volume1.maxPos.X, volume2.maxPos.X), max(volume1.maxPos.Y, volume2.maxPos.Y))
        trackVol1 = (newVolume.minPos - volume1.minPos)/self.resolution
        maxTrackVol1 = (len(volume1.hitGrid), len(volume1.hitGrid[0]))
        trackVol2 = (newVolume.minPos - volume2.minPos)/self.resolution
        maxTrackVol2 = (len(volume2.hitGrid), len(volume2.hitGrid[0]))
        for x in range(((newVolume.maxPos.X - newVolume.minPos.X) / self.resolution.X)+1):
            newVolume.hitGrid.append([])

            for y in range(((newVolume.maxPos.Y - newVolume.minPos.Y) / self.resolution.Y)+1):
                currentPos = newVolume.minPos + Vector2(x*self.resolution.X, y*self.resolution.Y)
                val = 0
                if 0 <= trackVol1.X < maxTrackVol1[0] and 0 <= trackVol1.Y < maxTrackVol1[1]:
                    val += volume1.hitGrid[int(trackVol1.X)][int(trackVol1.Y)].val
                if 0 <= trackVol2.X < maxTrackVol2[0] and 0 <= trackVol2.Y < maxTrackVol2[1]:
                    val += volume2.hitGrid[int(trackVol2.X)][int(trackVol2.Y)].val
                newVolume.hitGrid[x].append(HitPoint(currentPos, val))
                trackVol1 += Vector2.UnitY
                trackVol2 += Vector2.UnitY

            trackVol1 = Vector2(trackVol1.X + 1, (newVolume.minPos.Y - volume1.minPos.Y)/self.resolution.Y)
            trackVol2 = Vector2(trackVol2.X + 1, (newVolume.minPos.Y - volume2.minPos.Y)/self.resolution.Y)

        newVolume.hitGridToVoxelGrid()
        return newVolume

    def toShape(self, threshold):
        # get one of the border voxel and start drawing from it
        self.borderVoxels = []
        for voxel in self.voxels:
            if 0 < sum([c < threshold for c in voxel.corners]) < 4:
                self.borderVoxels.append(voxel)

        newShapes = []
        while self.borderVoxels:
            # draw the shape starting at a random border voxel from neighbour to neighbour
            self.firstBorderVoxel = None
            self.pointList = []
            self.populatePointList(self.borderVoxels[0], None, threshold)
            newShape = MotioShape()
            newShape.vertices = [Vertex(p) for p in self.pointList]
            newShapes.append(newShape)

        # construct holes
        # compute new shapes bbox
        bboxes = []
        for shape in newShapes:
            bboxes.append(BoundingBox2D.CreateFromPoints([v.position for v in shape.vertices]))
        # for each shape get other shape inside it
        alreadyDone = []
        for id in range(len(newShapes)):
            if id in alreadyDone:
                continue
            for otherShape in range(len(newShapes)):
                if id == otherShape or otherShape in alreadyDone:
                    continue
                if bboxes[id].Contains(bboxes[otherShape]) == ContainmentType.Contains:
                    newShapes[id].holes = [newShapes[otherShape]]
                    alreadyDone.append(otherShape)
                    break

        alreadyDone = [newShapes[done] for done in alreadyDone]
        for hole in alreadyDone:
            newShapes.remove(hole)

        return newShapes

    def populatePointList(self, voxel, previousVoxel, threshold):
        if not self.firstBorderVoxel:
            # first voxel
            self.firstBorderVoxel = voxel
            # set arbitrary previous voxel
            for side in ["upVox","rightVox","leftVox","downVox"]:
                exec("sideVoxel = voxel." + side)
                if sideVoxel:
                    if 0 < sum([c < threshold for c in sideVoxel.corners]) < 4:
                        previousVoxel = sideVoxel

        elif self.firstBorderVoxel == voxel:
            # last voxel
            return

        # remove voxel from list of voxel to convert to shape
        self.borderVoxels.remove(voxel)

        #easier check
        downLeftCorner = voxel.downLeftCorner < threshold
        upLeftCorner = voxel.upLeftCorner < threshold
        downRightCorner = voxel.downRightCorner < threshold
        upRightCorner = voxel.upRightCorner < threshold

        # SEARCH exiting side of the voxel taking account of previous voxel
        # exiting left
        if previousVoxel != voxel.leftVox and downLeftCorner != upLeftCorner:
            lower, higher = voxel.downLeftCorner, voxel.upLeftCorner
            if lower > higher:
                lower, higher = higher, lower
            fit = (threshold - lower) / (higher - lower)
            self.pointList.append(Vector2(voxel.position.X - self.resolution.X / 2,
                                          (voxel.position.Y - self.resolution.Y / 2) + fit * self.resolution.Y))
            nextVoxel = voxel.leftVox

        # exiting right
        elif previousVoxel != voxel.rightVox and downRightCorner != upRightCorner:
            lower, higher = voxel.downRightCorner, voxel.upRightCorner
            if lower < higher:
                lower, higher = higher, lower
            fit = (threshold - lower) / (higher - lower)
            self.pointList.append(Vector2(voxel.position.X + self.resolution.X / 2,
                                          (voxel.position.Y - self.resolution.Y / 2) + fit * self.resolution.Y))
            nextVoxel = voxel.rightVox

        # exiting up
        elif previousVoxel != voxel.upVox and upRightCorner != upLeftCorner:
            lower, higher = voxel.upLeftCorner, voxel.upRightCorner
            if lower > higher:
                lower, higher = higher, lower
            fit = (threshold - lower) / (higher - lower)
            self.pointList.append(Vector2((voxel.position.X - self.resolution.X / 2) + fit * self.resolution.X,
                                          voxel.position.Y + self.resolution.Y / 2))
            nextVoxel = voxel.upVox

        # exiting down
        elif previousVoxel != voxel.downVox and downLeftCorner != downRightCorner:
            lower, higher = voxel.downLeftCorner, voxel.downRightCorner
            if lower < higher:
                lower, higher = higher, lower
            fit = (threshold - lower) / (higher - lower)
            self.pointList.append(Vector2((voxel.position.X - self.resolution.X / 2) + fit * self.resolution.X,
                                  voxel.position.Y - self.resolution.Y / 2))
            nextVoxel = voxel.downVox

        # draw next neighbouring voxel
        return self.populatePointList(nextVoxel, voxel, threshold)


    def drawDebug(self):
        shapes = []
        for voxel in self.voxels:
            shapes.append(self.drawVoxel(voxel))
        return shapes

    def drawVoxel(self, voxel):
        pos = [
            Vector2(voxel.position.X + self.resolution.X / 2, voxel.position.Y + self.resolution.Y / 2),
            Vector2(voxel.position.X - self.resolution.X / 2, voxel.position.Y + self.resolution.Y / 2),
            Vector2(voxel.position.X - self.resolution.X / 2, voxel.position.Y - self.resolution.Y / 2),
            Vector2(voxel.position.X + self.resolution.X / 2, voxel.position.Y - self.resolution.Y / 2)
        ]
        shape = MotioShape()
        density = voxel.estimateDensity()
        shape.vertices = [Vertex(p, Vector4(density,density,density,1)) for p in pos]
        return shape


class Voxel:
    def __init__(self, position):
        self.position = position
        self.leftVox = None
        self.rightVox = None
        self.upVox = None
        self.downVox = None
        self.upLeftCorner = False
        self.upRightCorner = False
        self.downLeftCorner = False
        self.downRightCorner = False
        self.density = 0

    @property
    def corners(self):
        return [self.upLeftCorner, self.upRightCorner, self.downRightCorner, self.downLeftCorner]

    def estimateDensity(self):
        self.density = sum(self.corners)/4.0
        return self.density

    def isFull(self):
        return self.upRightCorner and self.upLeftCorner and self.downRightCorner and self.downLeftCorner

    def isEmpty(self):
        condition = self.upRightCorner or self.upLeftCorner or self.downRightCorner or self.downLeftCorner
        return not condition

    def isPartiallyFilled(self):
        notFull = not self.isFull()
        notEmpty = not self.isEmpty()
        return notFull and notEmpty

class HitPoint:
    def __init__(self, position, val):
        self.position = position
        self.val = val