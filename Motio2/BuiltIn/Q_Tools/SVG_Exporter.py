from Motio.UI.Utils.Export import TimelineExporter as BaseClass
from System.IO.File import WriteAllText, ReadAllText
from Motio.NodeCore import Node
from Motio.Meshing import MotioShapeGroup
from Motio.Geometry import Vector2
from time import sleep
from os.path import basename, realpath, dirname
from Motio.Configuration import ConfigEntry
from System import Boolean, Int32


class SVGExporter(BaseClass):
    classNameStatic = "SVG"

    def Filter(self):
        return "SVG File (*.svg)|*.svg"

    def MakeOptions(self):
        entry1 = ConfigEntry[Boolean]()
        entry1.ShortName = "Loop"
        entry1.LongName = "The animation is played once or in a loop"
        entry1.Value = True

        entry2 = ConfigEntry[Int32]()
        entry2.ShortName = "Playback framerate"
        entry2.LongName = "Frame per second displayed by the animation"
        entry2.Value = 25

        entry3 = ConfigEntry[Boolean]()
        entry3.ShortName = "Minify"
        entry3.LongName = "Reduce the size of embeded javascript"
        entry3.Value = True

        return [entry1, entry2, entry3]

    def BeforeExport(self):
        self.svgName = basename(self.path).split('#')[0]
        self.path = self.path.replace('#', '')

        # initialise vars
        self.svgColor = {}
        self.svgBody = ''


    def ExportCurrentFrame(self, path):
        frame = self.mainViewModel.AnimationTimeline.CurrentFrame
        shapeGroups = self.getShapeGroupsAtFrame(frame, self.mainViewModel.AnimationTimeline)
        self.svgBody += self.populateSvgAtFrame(shapeGroups, frame - self.startFrame)

    def AfterExport(self):
        # get document infos
        animationTimeline = self.mainViewModel.AnimationTimeline
        cameraW = animationTimeline.CameraWidth
        cameraH = animationTimeline.CameraHeight

        # header of svg file
        svgHeader = '<svg xmlns="http://www.w3.org/2000/svg" id="{}" '.format(self.svgName)
        svgHeader += 'viewBox="{} {} {} {}">\n'.format(-cameraW / 2, -cameraH / 2, cameraW, cameraH)
        svgHeader += '<defs><style>'
        for colorClass in self.svgColor.itervalues():
            svgHeader +=  '.%s{fill:%s;fill-rule:evenodd;}' % colorClass
        svgHeader += '</style></defs>\n'

        svgString = svgHeader + self.svgBody

        # add javascript animation
        svgString += '<script type="text/javascript">\n'
        if self.Options["Minify"].Value:
            jsPath = dirname(realpath(__file__)) + "/svgScriptTemplateMin.js"
        else:
            jsPath = dirname(realpath(__file__)) + "/svgScriptTemplateIE.js"
        jsTemplate = ReadAllText(jsPath)
        # here we input python vars into javascript
        jsTemplate = jsTemplate.replace('/*LASTFRAME*/', str(self.endFrame-self.startFrame))
        jsTemplate = jsTemplate.replace('/*FILENAME*/', str(self.svgName))
        loop = str(self.Options["Loop"].Value)
        jsTemplate = jsTemplate.replace('/*LOOPOPTION*/', loop.lower())
        jsTemplate = jsTemplate.replace('/*PLAYBACKFRAMERATE*/', str(self.Options["Playback framerate"].Value))
        svgString += jsTemplate
        svgString += '\n</script>\n'

        # end
        svgString += '</svg>'
        WriteAllText(self.path, svgString)
        print("SVG written to disk")


    def convertShapeToSvg(self, shape):
        """
        :type shape: Motio.Meshing.MotioShape
        :rtype: str
        """
        shapeTransform = shape.transform
        colorClass = self.getColorClass(shape.vertices[0].color)
        # start polygon
        svgString = '<path class="{}" d="'.format(colorClass)
        #convert shape and holes in paths
        svgString += self.shapeToPath(shape, shapeTransform)
        # end polygon
        return svgString + '"></path>\n'

    def shapeToPath(self, shape, shapeTransform):
        svgString = ''
        for i in range(shape.vertices.Count):
            if i == 0:
                svgString += 'M'
            else:
                svgString += 'L'
            pointPos = Vector2.Transform(shape.vertices[i].position, shapeTransform)
            svgString += '{},{}'.format(pointPos.X, -pointPos.Y)
        svgString += 'Z'

        holeSvgString = ''
        for hole in shape.holes:
            holeSvgString += self.shapeToPath(hole, shapeTransform)

        return svgString + holeSvgString

    def populateSvgAtFrame(self, shapeGroups, frame):
        """
        :param meshGroups: list[MotioShapeGroup]
        :param frame: int
        :rtype: str
        """
        # frame group
        displayType = "inline" if frame == 0 else "none"
        svgString = '<g id="Motio.{}.frame{}" display="{}">\n'.format(self.svgName, frame, displayType)

        # sort shapes by zIndex
        allShapes = []
        allShapesIndexed = {}
        for shapeGroup in shapeGroups:
            for shape in shapeGroup:
                if shape.zIndex in allShapesIndexed:
                    allShapesIndexed[int(shape.zIndex)].append(shape)
                else:
                    allShapesIndexed[int(shape.zIndex)] = [shape]
        indexes = allShapesIndexed.keys()
        indexes.sort()
        for index in indexes:
            allShapes.extend(allShapesIndexed[index])

        #convert shapes to svg code
        for shape in allShapes:
            svgString += self.convertShapeToSvg(shape)

        # end frame group
        svgString += '</g>\n'
        return svgString

    def getShapeGroupsAtFrame(self, frame, timeline):
        """
        :type frame: int
        :type graphicsNodes: list
        :rtype: list[MeshGroup]
        """
        shapeGroups = []
        for graphicsNode in timeline.GraphicsNodes:
            if graphicsNode.Visible:
                shapeGroup = self.getShapeGroup(frame, graphicsNode, timeline)
                shapeGroups.append(shapeGroup)
        return shapeGroups

    def getShapeGroup(self, frame, graphicsNode, timeline):
        """
        :type frame: int
        :type graphicsNode: graphics node
        :rtype: MeshGroup
        """
        graphicsNode = graphicsNode.Original
        completion, dataFeed = timeline.Original.CacheManager.TryGetCache(graphicsNode,
                                                                       graphicsNode.attachedNodes.Count - 1, frame)
        if not completion:
            return MotioShapeGroup()
        while not dataFeed:
            sleep(0.01)
            completion, dataFeed = timeline.Original.CacheManager.TryGetCache(graphicsNode,
                                                                           graphicsNode.attachedNodes.Count - 1,
                                                                           frame)
        if not dataFeed.ChannelExists(Node.POLYGON_CHANNEL):
            return MotioShapeGroup()
        return dataFeed.GetChannelData(Node.POLYGON_CHANNEL)

    def getColorClass(self, color):
        """
        :type color: Motio.Geometry.Vector4
        :rtype: str
        """
        colorHash = color.GetHashCode()
        if colorHash in self.svgColor:
            return self.svgColor[colorHash][0]

        newClassName = "cls{}".format(len(self.svgColor))
        newRGBA = "rgba({},{},{},{})".format(int(color.X * 255), int(color.Y * 255), int(color.Z * 255), color.W)

        self.svgColor[colorHash] = (newClassName, newRGBA)
        return self.svgColor[colorHash][0]