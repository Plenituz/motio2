from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase as BaseClass
from Motio.NodeImpl.NodePropertyTypes import DropdownNodeProperty, FloatNodeProperty, IntNodeProperty, BoolNodeProperty
from Motio.NodeImpl.NodePropertyTypes import VectorNodeProperty, OrderNodeProperty, SeparatorNodeProperty
from Motio.NodeCommon.ToolBox import CreateMatrix
from Motio.Geometry import Vector2, Vector3, Matrix, BoundingBox2D
from Motio.NodeCore import Node
from Motio.NodeCore.Utils import NodeUUIDGroup
from System import Random
from Q_Tools import Q_Helper
from Q_Tools.NodeDependency import NodeDependency

import clr
import Motio.NodeCore.Utils
clr.ImportExtensions(Motio.NodeCore.Utils)

class Duplicate(BaseClass):
    classNameStatic = "Duplicate"
    def __new__(cls, *args):
        return BaseClass.__new__(cls, *args)

    def setup_node(self):
        self.nodeDependency = NodeDependency(self)

    def setup_properties(self):
        #GENERAL
        #action
        actionProp = DropdownNodeProperty(self, "How to duplicate", "Duplicate", ["Transform", "Along path", "Around shape", "Inside shape"])
        self.Properties.Add("action", actionProp, "Transform")
        #number of copy
        numberProp = IntNodeProperty(self, "Number of copies", "Number of copies")
        numberProp.SetRangeFrom(0, True)
        numberProp.SetRangeTo(100, False)
        self.Properties.Add("number", numberProp, 2)
        #center shape before copy
        self.Properties.Add('centered', BoolNodeProperty(self, "Use the center of each shape to rotate and scale from", "Use center for rotate/scale"), True)
        #remove original shape
        self.Properties.Add('removeOriginal', BoolNodeProperty(self, "Remove the original shape leaving only clones", "Remove original"), False)
        #copy below
        self.Properties.Add('copyBelow', BoolNodeProperty(self, "Stacking the duplicates behind each others", "Copies behind"), False)
        self.Properties.AddManually('separator1', SeparatorNodeProperty(self))

        #TRANSFORM
        #position
        posProp = VectorNodeProperty(self, "Offset for every duplicate", "Position")
        self.Properties.Add("pos", posProp, Vector2(0, 0))
        #rotation
        rotationProp = FloatNodeProperty(self, "Rotation added to every duplicate", "Rotation")
        rotationProp.RangeFrom = 0
        rotationProp.RangeTo = 90
        self.Properties.Add("rotation", rotationProp, 0)
        #rotation center
        rotCenterProp = VectorNodeProperty(self, "From wich center the rotation occur", "Rotation center")
        self.Properties.Add("rotCenter", rotCenterProp, Vector2(0, 0))
        #scale
        scaleProp = VectorNodeProperty(self, "Scale apply to every duplicate", "Scale")
        self.Properties.Add("scale", scaleProp, Vector2(1,1))
        self.Properties["scale"].uniform = True
        #scale center
        scaleCenterProp = VectorNodeProperty(self, "From wich center the scale occur", "Scale center")
        self.Properties.Add("scaleCenter", scaleCenterProp, Vector2(0, 0))
        #transform order
        orderProp = OrderNodeProperty(self, "Order of transformation applied", "Transform order", ["Scale","Rotate","Translate"])
        self.Properties.AddManually("order", orderProp)

        #For along path, around shape and inside shape
        #graphic node
        shapeProp = DropdownNodeProperty(self, "Choose in which shape to duplicate", "Duplicate in", [])
        self.Properties.Add("duplicateIn", shapeProp, '')

        #orient along path
        orientProp = BoolNodeProperty(self, "Orient the duplicates shapes along the normal of the template", "Orient along template")
        self.Properties.Add("orient", orientProp, True)

        #random or regular position
        randomProp = BoolNodeProperty(self, "Random or regular position", "Position random")
        self.Properties.Add("random",randomProp, False)

        #seed
        seedProp = FloatNodeProperty(self, "Seed for random position","Seed")
        seedProp.RangeFrom = 0
        seedProp.RangeTo = 10
        self.Properties.Add('seed',seedProp, 0)

        #inverse shapes
        inverseProp = BoolNodeProperty(self, "inverse the two shapes used", "Inverse shapes")
        self.Properties.Add('inverse',inverseProp, False)

    def get_IndividualCalculationRange(self):
        self.Properties.WaitForProperty("duplicateIn")
        actionProp = self.Properties.GetValue("action", 0)
        if actionProp == "Transform" or actionProp == "Along path":
            return super(Duplicate, self).IndividualCalculationRange
        otherShape = self.Properties.GetValue("duplicateIn", 0)
        otherGNode = self.getGNodeFromUUID(otherShape[:NodeUUIDGroup.UUID_SIZE])
        if not otherGNode:
            return super(Duplicate, self).IndividualCalculationRange
        otherLastGA = otherGNode.attachedNodes[otherGNode.attachedNodes.Count - 1]
        return otherLastGA.CalculationRange

    def evaluate_frame(self, frame, dataFeed):
        self.Properties.WaitForProperty("inverse")

        action = self.Properties.GetValue("action",frame)
        shapeGroup = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)

        actionProp = self.Properties["action"]

        #checks input and call action appropriate
        if not shapeGroup:
            actionProp.SetError(1, "Needs at least 1 shape") # no shape found
            return
        else:
            actionProp.ClearError(1)

        if action == "Transform":
            self.nodeDependency.clearDependency()
            self.duplicateTransform(frame,shapeGroup)

        elif action == "Along path":
            self.nodeDependency.clearDependency()
            pathGroup = dataFeed.GetChannelData(Node.PATH_CHANNEL)
            if not pathGroup:
                self.Properties["action"].SetError(2, "Needs a path") # no path found
                return
            else:
                actionProp.ClearError(2)
            if pathGroup.Count < 1:
                self.Properties["action"].SetError(3, "Needs at least 1 path") # found several path
                return
            else:
                actionProp.ClearError(3)
            self.duplicateAlongPath(frame, shapeGroup, pathGroup)
                         
        elif action == "Inside shape":
            self.duplicateInsideShape(frame, shapeGroup)

        else:
            self.duplicateAroundShape(frame, shapeGroup)

    ############### MAIN ACTIONS ###############
    # DUPLICATE TRANSFORM
    def duplicateTransform(self, frame, shapeGroup):
        #get properties
        number = self.Properties.GetValue("number",frame)
        if number < 1:
            return
        pos = self.Properties.GetValue("pos", frame)
        rotation = self.Properties.GetValue("rotation",frame)
        rotationCenter = self.Properties.GetValue("rotCenter",frame)
        scale = self.Properties.GetValue("scale", frame)
        scaleCenter = self.Properties.GetValue("scaleCenter", frame)
        order = self.Properties["order"].items
        removeOriginal = self.Properties.GetValue("removeOriginal", frame)
        centered = self.Properties.GetValue("centered",frame)
        copyBelow = self.Properties.GetValue("copyBelow",frame)

        #actual duplicate
        originalShapes = [shape for shape in shapeGroup]
        for originalShape in originalShapes:
            originalMatrix = originalShape.transform
            originalzIndex = originalShape.zIndex
            if centered:
                shapeCenter = Q_Helper.estimatedCenter(originalShape)
            else:
                shapeCenter = Vector2.Zero
            for i in range(1,number+1):
                newShape = originalShape.Clone()
                transformMatrix = CreateMatrix(
                    pos*i,
                    Vector3(0,0,rotation)*i, 
                    Vector2(((scale.X-1)*i)+1,((scale.Y-1)*i)+1),
                    rotationCenter + shapeCenter,
                    scaleCenter + shapeCenter,
                    order
                )
                newShape.transform = originalMatrix*transformMatrix
                if copyBelow:
                    newShape.zIndex = originalzIndex-i
                else:
                    newShape.zIndex = originalzIndex 
                shapeGroup.Add(newShape)
            if removeOriginal:
                shapeGroup.Remove(originalShape)

    # DUPLICATE ALONG PATH
    def duplicateAlongPath(self, frame, shapeGroup, pathGroup):
        #get properties
        number = self.Properties.GetValue("number",frame)
        if number < 1:
            return
        randomCheckbox = self.Properties.GetValue("random", frame)
        copyBelow = self.Properties.GetValue("copyBelow",frame)
        centered = self.Properties.GetValue("centered", frame)
        removeOriginal = self.Properties.GetValue("removeOriginal", frame)
        seed = self.Properties.GetValue("seed", frame)
        orient = self.Properties.GetValue("orient", frame)

        #generate pos in percent on curve
        duplicatePercentPos = self.generateRelativePos(number, randomCheckbox, seed, 100)

        #convert percent pos in world pos
        duplicateWorldPos = []
        for path in pathGroup:
            duplicateWorldPos.append([self.percentOnPathToWorldPos(path, percent/100) for percent in duplicatePercentPos])

        #actual duplicate
        originalShapes = [shape for shape in shapeGroup]
        for originalShape in originalShapes:
            originalMatrix = originalShape.transform
            originalzIndex = originalShape.zIndex
            if centered:
                shapeCenter = Q_Helper.estimatedCenter(originalShape)
            else:
                shapeCenter = Vector2.Zero
            for i in range(pathGroup.Count):
                for j in range(len(duplicateWorldPos[i])):
                    newShape = originalShape.Clone()
                    if orient:
                        tangent = Q_Helper.calculateTangent(pathGroup[i], duplicatePercentPos[j] / 100.1)
                        angle = Vector2().AngleBetween(Vector2.UnitX, tangent)
                    else:
                        angle = 0
                    transformMatrix = CreateMatrix(
                        duplicateWorldPos[i][j],
                        Vector3(0, 0, angle),
                        Vector2.One,
                        shapeCenter,
                        Vector2.Zero,
                        ["R", "S", "T"]
                    )
                    newShape.transform = originalMatrix*transformMatrix

                    if copyBelow:
                        newShape.zIndex = originalzIndex-j
                    else:
                        newShape.zIndex = originalzIndex 
                    shapeGroup.Add(newShape)
                if removeOriginal:
                    shapeGroup.Remove(originalShape)
    
    # DUPLICATE AROUND SHAPE
    def duplicateAroundShape(self, frame, shapeGroup):
        #get properties
        number = self.Properties.GetValue("number",frame)
        if number < 1:
            return
        randomCheckbox = self.Properties.GetValue("random", frame)
        seed = self.Properties.GetValue("seed", frame)
        centered = self.Properties.GetValue("centered", frame)
        inverse = self.Properties.GetValue("inverse", frame)
        copyBelow = self.Properties.GetValue("copyBelow",frame)
        removeOriginal = self.Properties.GetValue("removeOriginal", frame)
        duplicateIn = self.Properties.GetValue("duplicateIn", frame)
        orient = self.Properties.GetValue("orient",frame)

        #get shape group for selected graphics node
        cachedDataFeed = self.nodeDependency.updateGDependencyAndGetDatafeed(frame, duplicateIn[:NodeUUIDGroup.UUID_SIZE])
        if not cachedDataFeed:
            return
        duplicateProp = self.Properties["duplicateIn"]
        if not cachedDataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            duplicateProp.SetError(4, "No mesh in selected graphics node")
            return
        else:
            duplicateProp.ClearError(4)
        dropdownShapeGroup = cachedDataFeed.GetChannelData(Node.POLYGON_CHANNEL)

        # know wich shape group is template and toDuplicate
        if inverse:
            toDuplicate = dropdownShapeGroup
            template = shapeGroup
        else:
            toDuplicate = shapeGroup
            template = dropdownShapeGroup

        if removeOriginal:
            toDelete = [shape for shape in shapeGroup]

        duplicateWorldPos = []
        for shape in template:
            #calculate normals
            if shape.ShouldCalculateNormals():
                shape.CalculateNormals()

            #get real point pos
            shape.BakeTransform()

            #calculate total length of shape border
            totalLength = 0
            for i in range(shape.vertices.Count):
                j = i + 1 if i < shape.vertices.Count - 1 else 0
                totalLength += Vector2.Distance(shape.vertices[i].position, shape.vertices[j].position)
            
            #generate pos in length on totalLength
            duplicateRelativePos = self.generateRelativePos(number, randomCheckbox, seed, totalLength)

            duplicateWorldPos = []
            #convert pos length relative to world pos
            lengthCumul = 0
            for i in range(shape.vertices.Count):
                j = i + 1 if i < shape.vertices.Count - 1 else 0
                lengthCumul += Vector2.Distance(shape.vertices[i].position, shape.vertices[j].position)
                relPosToRemove = []
                for relPos in duplicateRelativePos:
                    if relPos < lengthCumul:
                        relPosToRemove.append(relPos)
                        firstPoint = shape.vertices[i].position
                        firstNormal = shape.vertices[i].normal
                        secondPoint = shape.vertices[j].position
                        secondNormal = shape.vertices[j].normal
                        direction = firstPoint - secondPoint
                        posOnSegment = (relPos - lengthCumul) / direction.Length()
                        direction *= posOnSegment
                        #normalAvg = firstNormal * posOnSegment + secondNormal * (1-posOnSegment)
                        normalAvg = Vector2(-direction.Y, direction.X)
                        angle = Vector2().AngleBetween(Vector2.UnitY, normalAvg)
                        duplicateWorldPos.append((firstPoint+direction, angle))
                for relPos in relPosToRemove:
                    duplicateRelativePos.remove(relPos)

        #actual duplicate
        originalShapes = [shape for shape in toDuplicate]
        for originalShape in originalShapes:
            originalMatrix = originalShape.transform
            originalzIndex = originalShape.zIndex
            if centered:
                shapeCenter = Q_Helper.estimatedCenter(originalShape)
            else:
                shapeCenter = Vector2.Zero

            for posAngle in duplicateWorldPos:
                newShape = originalShape.Clone()
                if orient:
                    angle = posAngle[1]
                else:
                    angle = 0
                transformMatrix = CreateMatrix(
                    posAngle[0],
                    Vector3(0, 0, angle),
                    Vector2.One,
                    shapeCenter,
                    Vector2.Zero,
                    ["R", "S", "T"]
                )
                newShape.transform = originalMatrix*transformMatrix

                if copyBelow:
                    newShape.zIndex = originalzIndex-j
                else:
                    newShape.zIndex = originalzIndex
                shapeGroup.Add(newShape)

        #remove original
        if removeOriginal:
            for i in range(toDelete.Count):
                shapeGroup.Remove(toDelete[i])

    # DUPLICATE INSIDE SHAPE
    def duplicateInsideShape(self, frame, shapeGroup):
        # TODO add orient using closest point normal mixed
        #get properties
        number = self.Properties.GetValue("number",frame)
        if number < 1:
            return
        seed = self.Properties.GetValue("seed", frame)
        inverse = self.Properties.GetValue("inverse", frame)
        copyBelow = self.Properties.GetValue("copyBelow",frame)
        removeOriginal = self.Properties.GetValue("removeOriginal", frame)
        duplicateIn = self.Properties.GetValue("duplicateIn", frame)

        # get shape group for selected graphics node
        cachedDataFeed = self.nodeDependency.updateGDependencyAndGetDatafeed(frame, duplicateIn[:NodeUUIDGroup.UUID_SIZE])
        if not cachedDataFeed:
            return
        duplicateProp = self.Properties["duplicateIn"]
        if not cachedDataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            duplicateProp.SetError(5, "No mesh in selected graphics node")
            return
        else:
            duplicateProp.ClearError(5)
        dropdownShapeGroup = cachedDataFeed.GetChannelData(Node.POLYGON_CHANNEL)

        # know which shapegroup is template and toDuplicate
        if inverse:
            toDuplicate = dropdownShapeGroup
            template = shapeGroup
        else:
            toDuplicate = shapeGroup
            template = dropdownShapeGroup

        if removeOriginal:
            toDelete = [shape for shape in shapeGroup]

        #scatter points on pattern
        copyPos = []
        for i in range(template.Count):
            shape = template[i]

            shapeTransformed = shape.Clone()
            shapeTransformed.BakeTransform()

            bbox = BoundingBox2D.CreateFromPoints([vertex.position for vertex in shapeTransformed.vertices])
            randObject = Random(seed)

            while(len(copyPos)<number*(i+1)):
                x = randObject.NextDouble()*((bbox.Max.X-bbox.Min.X)*2+bbox.Min.X - bbox.Min.X) + bbox.Min.X
                y = randObject.NextDouble()*((bbox.Max.Y-bbox.Min.Y)*2+bbox.Min.Y - bbox.Min.Y) + bbox.Min.Y
                randPos = Vector2(x,y)
                if shapeTransformed.HitTest(randPos):
                    copyPos.append(randPos)

        #actual duplicate
        originalShapes = [shape for shape in toDuplicate]
        for originalShape in originalShapes:
            originalMatrix = originalShape.transform
            originalzIndex = originalShape.zIndex

            for i in range(len(copyPos)):
                newShape = originalShape.Clone()
                transformMatrix = Matrix.CreateTranslation(copyPos[i].X,copyPos[i].Y,0)
                newShape.transform = originalMatrix*transformMatrix

                if copyBelow:
                    newShape.zIndex = originalzIndex-i
                else:
                    newShape.zIndex = originalzIndex 
                shapeGroup.Add(newShape)

        #remove original
        if removeOriginal:
            for i in range(toDelete.Count):
                shapeGroup.Remove(toDelete[i])

    ############### HELPERS FUNCTIONS ###############
    # convert percent on path to pos
    def percentOnPathToWorldPos(self, path, percent):
        if percent == 1:
            percent = 0.999
        curvePos = path.PointAtPercent(percent, True)
        if curvePos:
            return curvePos
        else:
            return Vector2(0,0)

    # generate list of positions on a percent scale (random or evenly spaced)
    def generateRelativePos(self, number, randomCheckbox, seed, rangeEnd):
        duplicatePercentPos = []
        if randomCheckbox:
            randObject = Random(seed)
            for i in range(number):
                duplicatePercentPos.append(randObject.NextDouble()*rangeEnd)
        else:
            separation = float(rangeEnd)/float(number)
            for i in range(number):
                duplicatePercentPos.append(i*separation)
        return duplicatePercentPos

    def getGNodeFromUUID(self, UUIDstring):
        timeline = self.GetTimeline()
        # get reference to graphics node from uuid
        try:
            graphicsNode = timeline.uuidGroup.LookupNode(UUIDstring)
        except:
            return False

        if not graphicsNode:
            return False
        return graphicsNode