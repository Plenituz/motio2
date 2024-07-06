#helper functions for WaterMotion python development
from System import Random
from math import sqrt
from Motio.Geometry import Vector2, Vector4, Vertex
from Motio.NodeCore import Node, GraphicsAffectingNode, PropertyAffectingNode
import clr
import Motio.NodeCore.Utils
clr.ImportExtensions(Motio.NodeCore.Utils)

def colorShape(shape, color):
    """
    :type shape: Motio.Meshing.MotioShape
    :type color: Vector4
    """
    for hole in shape.holes:
        colorShape(hole, color)
    for i in range(len(shape.vertices)):
        v = shape.vertices[i]
        v.SetColor(color)
        shape.vertices[i] = v

def VertexLerp(v1, v2, ratio):
    """
    :type v1: Motio.Geometry.Vertex
    :type v2: Motio.Geometry.Vertex
    :type ratio: float
    :rtype: Motio.Geometry.Vertex
    """
    v = Vertex(v1.position * (1-ratio) + v2.position * ratio)
    if v1.color and v2.color:
        v.SetColor(v1.color * (1-ratio) + v2.color * ratio)
    if v1.normal and v2.normal:
        v.SetNormal(v1.normal * (1-ratio) + v2.normal * ratio)
    if v1.uv and v2.uv:
        v.SetUv(v1.uv * (1-ratio) + v2.uv * ratio)
    return v

def fitRange(inMin, inMax, outMin, outMax, value):
    """
    :type inMin: float
    :type inMax: float
    :type outMin: float
    :type outMax: float
    :type value: float
    :rtype: float
    """
    if (inMax - inMin) == 0:
        return inMax
    percent = (value - inMin) / (inMax - inMin)
    return ((outMax - outMin) * percent) + outMin

def listNodes(currentNode):
    """
    :type currentNode: Motio.NodeCore.Node
    :rtype: dict{GraphicsNodes, GraphicsAffectingNodes, PropertyAffectingNodes}
    """
    timeline = currentNode.GetTimeline()
    allNodes = []
    for graphicNode in timeline.GraphicsNodes:
        searchDown(graphicNode, allNodes)
    return sortNodeByType(allNodes)

def searchDown(node, nodeList):
    """
    :type node: Motio.NodeCore.Node
    :type nodeList: list[Motio.NodeCore.Node]
    """
    # store if it's a node
    if isinstance(node,Node):
        nodeList.append(node)
    # search if attached node
    if hasattr(node, "attachedNodes"):
        for member in node.attachedNodes:
            searchDown(member, nodeList)
    # search for property nodes attached
    if hasattr(node, "Properties"):
        for nodeProperty in node.Properties:
            searchDown(nodeProperty, nodeList)

def sortNodeByType(allNodes):
    """
    :type allNodes: list[Motio.NodeCore.Node]
    :rtype: dict{GraphicsNodes, GraphicsAffectingNodes, PropertyAffectingNodes}
    """
    allNodesSorted = {"GraphicsNodes":[],"GraphicsAffectingNodes":[], "PropertyAffectingNodes":[]}
    for node in allNodes:
        if isinstance(node,GraphicsAffectingNode):
            allNodesSorted["GraphicsAffectingNodes"].append(node)
        elif isinstance(node,PropertyAffectingNode):
            allNodesSorted["PropertyAffectingNodes"].append(node)
        else:
            allNodesSorted["GraphicsNodes"].append(node)
    return allNodesSorted


#return a random vector4 object using a seed
def random_color(seed, randObject = None):
    """
    :type seed: float
    :type randObject: Random
    :rtype: Vector4
    """
    if not randObject:
        randObject = Random(seed)
    return Vector4(randObject.NextDouble(),randObject.NextDouble(),randObject.NextDouble(),1)


#calculate the estimated center of the mesh
def estimatedCenter(shape):
    """
    :type mesh: Motio.Meshing.MotioShape
    :rtype: Vector2
    """
    meanCoord = Vector2(0,0)
    for i in range(len(shape.vertices)):
        meanCoord += shape.vertices[i].position
    return Vector2.Transform(meanCoord/len(shape.vertices), shape.transform)


#search parent node of type t
def searchUp(on, t):
    """
    :type on: Motio.NodeCore.Node
    :type t: type
    :rtype: Motio.NodeCore.Node
    """
    if(isinstance(on, t)):
        return on
    if(hasattr(on, "Host")):
        return searchUp(on.Host, t)


def calculateTangent(path, percent):
    """
    :type path: Motio.Pathing.Path
    :type percent: float
    :rtype: Vector2
    """

    # search between which path points the percent is
    searchingLength = percent * path.PathLength
    cumulatedLength = 0
    for i in range(len(path.Points)):
        cumulatedLength += path.DistanceToNext(i)
        if searchingLength < cumulatedLength:
            break

    # calculate percent between path points
    segmentLength = path.DistanceToNext(i)
    cumulatedLength -= segmentLength
    segmentPercent = (searchingLength - cumulatedLength) / segmentLength

    firstPoint = path.Points[i].Position
    firstHandle = firstPoint + path.Points[i].RightHandle
    secondPoint = path.Points[i].NextPoint.Position
    secondHandle = secondPoint + path.Points[i].NextPoint.LeftHandle
    tangent = TangentEquation(segmentPercent, firstPoint, firstHandle, secondHandle, secondPoint)
    return tangent

def TangentEquation(percent, firstPoint, firstHandle, secondHandle, secondPoint):
    """
    :type posAttrib: float
    :type firstPoint: Vector2
    :type firstHandle: Vector2
    :type secondHandle: Vector2
    :type secondPoint: Vector2
    :rtype: Vector2
    """

    t, P0, P1, P2, P3 = percent, firstPoint, firstHandle, secondHandle, secondPoint

    return P0 * -3 * (1 - t)**2 + P1 * 3 * (1 - t)**2 - P1 * 6 * t * (1 - t) - P2 * 3 * t**2 + P2 * 6 * t * (1 - t) + P3 *  3 * t**2


# Perlin noise translated from C
# Original C from https://openclassrooms.com/courses/bruit-de-perlin
def Get2DPerlinNoiseValue(x, y, res):
    """
    :type x: float
    :type y: float
    :type res: float
    :rtype: float
    """
    if x > 255:
        x = x - 255 * int(x / 255)
    if y > 255:
        y = y - 255 * int(y / 255)

    unit = 1.0/sqrt(2)
    gradient2 = [[unit,unit],[-unit,unit],[unit,-unit],[-unit,-unit],[1,0],[-1,0],[0,1],[0,-1]]
    perm =[
        151,160,137,91,90,15,131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,
        142,8,99,37,240,21,10,23,190,6,148,247,120,234,75,0,26,197,62,94,252,219,
        203,117,35,11,32,57,177,33,88,237,149,56,87,174,20,125,136,171,168,68,175,
        74,165,71,134,139,48,27,166,77,146,158,231,83,111,229,122,60,211,133,230,220,
        105,92,41,55,46,245,40,244,102,143,54,65,25,63,161,1,216,80,73,209,76,132,
        187,208,89,18,169,200,196,135,130,116,188,159,86,164,100,109,198,173,186,3,
        64,52,217,226,250,124,123,5,202,38,147,118,126,255,82,85,212,207,206,59,227,
        47,16,58,17,182,189,28,42,223,183,170,213,119,248,152,2,44,154,163,70,221,
        153,101,155,167,43,172,9,129,22,39,253,19,98,108,110,79,113,224,232,178,185,
        112,104,218,246,97,228,251,34,242,193,238,210,144,12,191,179,162,241,81,51,145,
        235,249,14,239,107,49,192,214,31,181,199,106,157,184,84,204,176,115,121,50,45,
        127,4,150,254,138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,
        156,180
    ]
    perm.extend(perm)

    #Adapter pour la resolution
    x /= res
    y /= res

    #On recupere les positions de la grille associee a (x,y)
    x0 = int(x)
    y0 = int(y)

    #Masquage
    ii = x0 & 255
    jj = y0 & 255

    #Pour recuperer les vecteurs
    gi0 = perm[ii + perm[jj]] % 8
    gi1 = perm[ii + 1 + perm[jj]] % 8
    gi2 = perm[ii + perm[jj + 1]] % 8
    gi3 = perm[ii + 1 + perm[jj + 1]] % 8

    #on recupere les vecteurs et on pondere
    tempX = x-x0
    tempY = y-y0
    s = gradient2[gi0][0]*tempX + gradient2[gi0][1]*tempY

    tempX = x-(x0+1)
    tempY = y-y0
    t = gradient2[gi1][0]*tempX + gradient2[gi1][1]*tempY

    tempX = x-x0
    tempY = y-(y0+1)
    u = gradient2[gi2][0]*tempX + gradient2[gi2][1]*tempY

    tempX = x-(x0+1)
    tempY = y-(y0+1)
    v = gradient2[gi3][0]*tempX + gradient2[gi3][1]*tempY


    #Lissage
    tmp = x-x0
    Cx = 3 * tmp * tmp - 2 * tmp * tmp * tmp

    Li1 = s + Cx*(t-s)
    Li2 = u + Cx*(v-u)

    tmp = y - y0
    Cy = 3 * tmp * tmp - 2 * tmp * tmp * tmp

    return Li1 + Cy*(Li2-Li1)