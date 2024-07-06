import Motio.UI.ViewModels.NodeToolViewModel as BaseClass
from Motio.UI.Gizmos import Gizmo
import Motio.Geometry as Geo
from System.Windows.Media import Brushes, PointCollection, ScaleTransform
from System.Windows.Shapes import Polygon, Line
from System.Windows import Point
from System.Windows.Media.Media3D import Point3D
from System.Windows.Controls import StackPanel
from Motio.UI.ViewModels import AnimationTimelineViewModel
from Motio.ClickLogic import ClickAndDragHandler
from Motio.Configuration import Configs
from Motio.NodeCore import Node
import time

import clr
import Motio.UI.Utils
clr.ImportExtensions(Motio.UI.Utils)
import Motio.NodeCore.Utils
clr.ImportExtensions(Motio.NodeCore.Utils)
import Motio.NodeImpl
clr.ImportExtensions(Motio.NodeImpl)

class MoveToolViewModel(BaseClass):

    def __new__(cls, *args):
        instance = BaseClass.__new__(cls, *args)
        instance.gizmos = []
        instance.dragStartPos = None
        instance.gizmoStartPos = None
        instance.prop = None
        return instance

    def OnHide(self):
        #always call super method in ViewModels
        super(MoveToolViewModel, self).OnHide()
        #unsubscribe to viewport scaling event
        self._host.FindMainViewModel().RenderView.ContentScaleChanged -= self.content_scale_changed
        #unsubscribe to change event in position property
        if self.prop is not None:
            self.prop.PropertyChanged -= self.posPropChanged
            self.prop.Deleted -= self.selfDelete
        #unsubscribe to change in configs
        Configs.Instance.ConfigsChanged -= self.onConfigsChanged

        #remove current gizmo from viewport
        for gizmo in self.gizmos:
            Gizmo.Remove(gizmo)

    def UpdateDisplay(self):
        super(MoveToolViewModel, self).UpdateDisplay()
        self.updateGizmosPos()

    def getProperty(self):
        self._host.Original.Properties.WaitForProperty(self.Original.positionProperty)
        return self._host.Original.Properties[self.Original.positionProperty]

    def selfDelete(self):
        #deleting the original will call OnDelete, the inverse is not true
        self.Original.Delete()

    def OnShow(self):
        #always call super method in ViewModels
        super(MoveToolViewModel, self).OnShow()
        #subscribe to viewport scaling event
        renderView = self._host.FindMainViewModel().RenderView
        renderView.ContentScaleChanged += self.content_scale_changed
        #subscribe to change event in position property
        if self.prop is None:
            self.prop = self.getProperty()
        self.prop.Deleted += self.selfDelete
        self.prop.PropertyChanged += self.posPropChanged
        #subscribe to change in configs
        Configs.Instance.ConfigsChanged += self.onConfigsChanged

        #if first time displaying gizmo, create and update it
        if len(self.gizmos) == 0:
            #create actual gizmo
            xArrow, yArrow, square = self.createArrows()
            moveGizmo = Gizmo.Add(xArrow)
            moveGizmo = Gizmo.Add(yArrow)
            moveGizmo = Gizmo.Add(square)
            self.gizmos.extend([xArrow,yArrow,square])


            #create event handler for mouse hover
            for gizmo in self.gizmos:
                gizmo.MouseEnter += self.mouseHover
                gizmo.MouseLeave += self.mouseLeave

            #create event handler for click and drag
            xHandler = ClickAndDragHandler(xArrow)
            xHandler.OnDrag = self.updateObjectPosX
            xHandler.OnDragEnd = self.updateObjectPosEnd
            yHandler = ClickAndDragHandler(yArrow)
            yHandler.OnDrag = self.updateObjectPosY
            yHandler.OnDragEnd = self.updateObjectPosEnd
            bothHandler = ClickAndDragHandler(square)
            bothHandler.OnDrag = self.updateObjectPosBoth
            bothHandler.OnDragEnd = self.updateObjectPosEnd

        else:
            #if gzmo already created, show and update them
            for gizmo in self.gizmos:
                Gizmo.Add(gizmo)
		self.content_scale_changed(renderView.ContentScale)
        self.updateGizmosPos()


    def Delete(self):
        #always call super method in ViewModels
        super(MoveToolViewModel, self).Delete()
        self.OnHide()
        self.gizmos = None

    def OnClickInViewport(self, ev, worldPos, canvasPos):
        #always call super method in ViewModels
        super(MoveToolViewModel, self).OnClickInViewport(ev, worldPos, canvasPos)

    def posPropChanged(self,sender,args):
        if args.PropertyName in ["X", "Y"]:
            self.updateGizmosPos()

    def updateGizmosPos(self):
        worldPos = self.getProperty().GetCacheOrCalculateInValue(self._host.Original.GetTimeline().CurrentFrame).GetChannelData(Node.PROPERTY_OUT_CHANNEL)
        for gizmo in self.gizmos:
            Gizmo.SetWorldPos(gizmo, worldPos.X, worldPos.Y)

    def content_scale_changed(self, scale):
        newScale = ((1/scale)/4)*Configs.GetValue(Configs.MoveGizmoSize)
        for gizmo in self.gizmos:
            gizmo.RenderTransform.ScaleX = newScale
            gizmo.RenderTransform.ScaleY = newScale

    def createArrows(self):
        scale = (self._host.FindMainViewModel().RenderView.InverseContentScale/4)*Configs.GetValue(Configs.MoveGizmoSize)
        xArrow = Polygon()
        xArrow.RenderTransform = ScaleTransform(scale, scale)
        xArrow.Fill = Brushes.Red
        xArrow.StrokeThickness = 0
        xArrow.Stroke = Brushes.DarkRed
        xPoints = PointCollection()
        xPoints.Add(Point(0,-6))
        xPoints.Add(Point(120,-6))
        xPoints.Add(Point(120,-20))
        xPoints.Add(Point(180,0))
        xPoints.Add(Point(120,20))
        xPoints.Add(Point(120,6))
        xPoints.Add(Point(0,6))
        xArrow.Points = xPoints
        yArrow = Polygon()
        yArrow.RenderTransform = ScaleTransform(scale, scale)
        yArrow.Fill = Brushes.Lime
        yArrow.StrokeThickness = 0
        yArrow.Stroke = Brushes.Green
        yPoints = PointCollection()
        yPoints.Add(Point(-6,0))
        yPoints.Add(Point(-6,-120))
        yPoints.Add(Point(-20,-120))
        yPoints.Add(Point(0,-180))
        yPoints.Add(Point(20,-120))
        yPoints.Add(Point(6,-120))
        yPoints.Add(Point(6,0))
        yArrow.Points = yPoints
        square = Polygon()
        square.RenderTransform = ScaleTransform(scale, scale)
        square.Fill = Brushes.SlateGray
        square.StrokeThickness = 0
        square.Stroke = Brushes.DarkSlateGray
        squarePoints = PointCollection()
        squarePoints.Add(Point(-15,-15))
        squarePoints.Add(Point(-15,15))
        squarePoints.Add(Point(15,15))
        squarePoints.Add(Point(15,-15))
        square.Points = squarePoints
        return xArrow, yArrow, square


    #mouse hover event handler
    def mouseHover(self, sender, args):
        sender.StrokeThickness = 4
        scale = (self._host.FindMainViewModel().RenderView.InverseContentScale/3.75)*Configs.GetValue(Configs.MoveGizmoSize)
        sender.RenderTransform.ScaleX = scale
        sender.RenderTransform.ScaleY = scale
    def mouseLeave(self, sender, args):
        sender.StrokeThickness = 0
        scale = (self._host.FindMainViewModel().RenderView.InverseContentScale/4)*Configs.GetValue(Configs.MoveGizmoSize)
        sender.RenderTransform.ScaleX = scale
        sender.RenderTransform.ScaleY = scale

    #config change handler
    def onConfigsChanged(self):
        scale = (self._host.FindMainViewModel().RenderView.InverseContentScale/4)*Configs.GetValue(Configs.MoveGizmoSize)
        for gizmo in self.gizmos:
            gizmo.RenderTransform.ScaleX = scale
            gizmo.RenderTransform.ScaleY = scale

    #click and drag event handler
    def updateObjectPosX(self, dragEvent):
        self.updateObjectPos(dragEvent,"X")

    def updateObjectPosY(self, dragEvent):
        self.updateObjectPos(dragEvent,"Y")

    def updateObjectPosBoth(self, dragEvent):
        self.updateObjectPos(dragEvent,"Both")

    def updateObjectPos(self,dragEvent,direction):
        cursorPosCanvas = dragEvent.currentEvent.GetPosition(Gizmo.Canvas)
        cursorPosWorld = self._host.FindMainViewModel().AnimationTimeline.Canv2World(Point(cursorPosCanvas.X,cursorPosCanvas.Y))
        #first drag call
        if not self.dragStartPos:
            self.dragStartPos = cursorPosWorld
            self.gizmoStartPos = self.getProperty().StaticValue
        deltaCursor =  cursorPosWorld - self.dragStartPos
        if direction ==  "X":
            self.getProperty().StaticValue = Geo.Vector2(self.gizmoStartPos.X + deltaCursor.X, self.gizmoStartPos.Y)
        elif direction == "Y":
            self.getProperty().StaticValue = Geo.Vector2(self.gizmoStartPos.X, self.gizmoStartPos.Y + deltaCursor.Y)
        else :
            self.getProperty().StaticValue = Geo.Vector2(self.gizmoStartPos.X + deltaCursor.X, self.gizmoStartPos.Y + deltaCursor.Y)
        self.updateGizmosPos()

    def updateObjectPosEnd(self,dragEvent):
        self.dragStartPos = None
        self.gizmoStartPos = None