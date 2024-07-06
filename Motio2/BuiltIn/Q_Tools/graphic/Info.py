from Motio.NodeImpl.GraphicsAffectingNodes import PyGraphicsAffectingNodeBase
from Motio.NodeImpl.NodePropertyTypes import DropdownNodeProperty, StringNodeProperty
from Motio.NodeCore import Node
from Q_Tools import Q_Helper
from math import floor

import clr
import Motio.NodeCore.Utils
clr.ImportExtensions(Motio.NodeCore.Utils)

class Info(PyGraphicsAffectingNodeBase):
    classNameStatic = "Info"
    def __new__(cls, *args):
        return PyGraphicsAffectingNodeBase.__new__(cls, *args)

    def setup_properties(self):
        #choose what to display
        actionProp = DropdownNodeProperty(self, "What type of info to display", "Infos about", ["Nodes in scene", "Channels in dataFeed", "Points in shapes"])
        self.Properties.Add("action", actionProp, "Channels in dataFeed")

        #string property
        self.Properties.Add("infos", StringNodeProperty(self, "Infos here", "Infos"), "")

    def evaluate_frame(self, frame, dataFeed):
        self.Properties.WaitForProperty("infos")

        action = self.Properties.GetValue("action",frame)

        if action == "Nodes in scene":
            toDisplay = self.getInfosScene()
        elif action == "Channels in dataFeed":
            toDisplay = self.getInfosDataFeed(dataFeed)
        else:
            toDisplay = self.getInfosShapes(dataFeed)

        self.Properties["infos"].StaticValue = toDisplay


    def getInfosScene(self):
        allNodes = Q_Helper.listNodes(self)

        toDisplay = 'The scene contains {} nodes :\r\n'.format(sum([len(allNodes[nodeType]) for nodeType in allNodes]))
        toDisplay += ' - {} graphics nodes\r\n'.format(len(allNodes["GraphicsNodes"]))
        toDisplay += ' - {} graphics affecting nodes\r\n'.format(len(allNodes["GraphicsAffectingNodes"]))
        toDisplay += ' - {} property affecting nodes'.format(len(allNodes["PropertyAffectingNodes"]))

        for nodeType in allNodes:
            for node in allNodes[nodeType]:
                toDisplay += '\r\n\r\n'
                if nodeType in ["GraphicsAffectingNodes","PropertyAffectingNodes"]:
                    try:
                        classNameStatic = node.classNameStatic
                    except:
                        classNameStatic = node.ClassNameStatic
                    toDisplay += '{} is a {} of type {}\r\n'.format(node.UserGivenName, nodeType, classNameStatic)
                else:
                    toDisplay += '{} is a {}\r\n'.format(node.UserGivenName, nodeType)
                toDisplay += 'UUID : {}'.format(node.UUID)

        return toDisplay

    def getInfosDataFeed(self, dataFeed):
        dataChannels = dataFeed.ListChannels()

        toDisplay = 'This node branch contains {} channels :\r\n'.format(len(dataChannels))
        for channel in dataChannels:
            toDisplay += ' - {}\r\n'.format(channel)
        toDisplay += '\r\n\r\n'

        #special channels
        for channel in dataChannels:
            if channel == Node.POLYGON_CHANNEL:
                toDisplay += "Inside polygon channel we found :\r\n"
                shapeGroup = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)
                if shapeGroup:
                    toDisplay += '{} shape in the shapeGroup\r\n'.format(shapeGroup.Count)
                    toDisplay += 'Total points count {}\r\n'.format(sum([len(shape.vertices) for shape in shapeGroup]))
                    for shape in shapeGroup:
                        toDisplay += '- Shape : {} points, zIndex {}, transformable : {}, deformable : {}\r\n'.format(len(shape.vertices), shape.zIndex, shape.transformable, shape.deformable)
                else:
                    toDisplay += 'But no shapeGroup :(\r\n'
                toDisplay += '\r\n'
                
            if channel == Node.PATH_CHANNEL:
                toDisplay += "PATH channel detected !\r\n"
                pathGroup = dataFeed.GetChannelData(Node.PATH_CHANNEL)
                if pathGroup:
                    toDisplay += '{} path in the pathGroup\r\n'.format(pathGroup.Count)
                    toDisplay += 'Total points count {}\r\n'.format(sum([path.Points.Count for path in pathGroup]))
                    for path in pathGroup:
                        toDisplay += '- Path : {} points, closed : {}, length : {}\r\n'.format(path.Points.Count, path.Closed, path.PathLength)
                else:
                    toDisplay += 'But no pathGroup :(\r\n'
                toDisplay += '\r\n'
        
        return toDisplay

    def getInfosShapes(self, dataFeed):
        if not dataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            return 'No polygon/shape channel found'

        toDisplay = ''

        shapeGroup = dataFeed.GetChannelData(Node.POLYGON_CHANNEL)
        for i in range(shapeGroup.Count):
            shape = shapeGroup[i]
            # mesh header
            toDisplay += 'Shape number {} | Zindex : {}, '.format(i, shape.zIndex)
            toDisplay += 'Deformable : {}, Transformable : {}, '.format(shape.deformable, shape.transformable)
            toDisplay += 'Generation : {}, Transform : {}'.format(shape.generation, shape.transform)
            toDisplay += '\r\n'

            #creat array object
            infoArray = self.StringArray(['Id', 'Position', 'Normal', 'Color'])

            #normals calculation
            if shape.ShouldCalculateNormals():
                shape.CalculateNormals()

            #add current point info to array
            for j in range(len(shape.vertices)):
                infoArray.addRow(j, shape.vertices[j].position, shape.vertices[j].normal, shape.vertices[j].color)

            toDisplay += infoArray.buildArray()
            toDisplay += '\r\n'

        return toDisplay

    #Array object to simplify displaying point infos
    class StringArray:
        def __init__(self, columnNames = []):
            self.columnNames = columnNames
            self.columnSize = []
            self.arrayData = []
            self.builtArray = ''
            self.hChar = '-'
            self.vChar = '|'
            self.crossChar = '+'

        def addRow(self, *args):
            self.arrayData.append([str(arg) for arg in args])

        def addColumn(self, columnName):
            self.columnNames.append(columnName)

        def addColumns(self, columnsName):
            self.columnNames.extend(columnsName)

        def __str__(self):
            return self.buildArray()

        def buildArray(self):
            self.calculateColumnSize()

            #build header
            self.buildSeparator()
            self.buildTitle()
            self.buildEmpty()
            self.buildSeparator()

            #build rest of array
            for row in self.arrayData:
                self.buildRow(row)
                self.buildSeparator()

            return self.builtArray

        def calculateColumnSize(self):
            columnSize = []
            for i in range(len(self.columnNames)):
                stringSize = []
                for row in self.arrayData:
                    if i < len(row):
                        stringSize.append(len(row[i]))
                stringSize.append(len(self.columnNames[i]))
                columnSize.append(max(stringSize)+2)
            self.columnSize = columnSize

        def buildRow(self, rowData):
            self.builtArray += self.vChar
            for i in range(len(self.columnNames)):
                if i < len(rowData):
                    data = rowData[i]
                else:
                    data = ' '
                self.builtArray += self.centerString(data, self.columnSize[i]) + self.vChar
            self.newLine()

        def buildSeparator(self):
            self.builtArray += self.crossChar
            for colSize in self.columnSize:
                self.builtArray += self.hChar*colSize
                self.builtArray += self.crossChar
            self.newLine()

        def buildEmpty(self):
            self.builtArray += self.vChar
            for colSize in self.columnSize:
                self.builtArray += ' '*colSize
                self.builtArray += self.vChar
            self.newLine()

        def buildTitle(self):
            self.builtArray += self.vChar
            for i in range(len(self.columnNames)):
                self.builtArray += self.centerString(self.columnNames[i], self.columnSize[i]) + self.vChar
            self.newLine()

        def centerString(self, string, size):
            spaceAround = size - len(string)
            leftSpace = floor(float(spaceAround)/2)
            rightSpace = spaceAround - leftSpace
            string = ' ' * int(leftSpace) + string + ' ' * int(rightSpace)
            return string

        def newLine(self):
            self.builtArray += '\r\n'