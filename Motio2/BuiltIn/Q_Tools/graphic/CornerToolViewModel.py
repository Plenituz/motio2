import Motio.UI.ViewModels.NodeToolViewModel as BaseClass
from Motio.UI.Gizmos import Gizmo
from Motio.Geometry import Vector2
from System.Windows.Media import Brushes, PointCollection, ScaleTransform, TranslateTransform, TransformGroup
from System.Windows.Shapes import Ellipse
from System.Windows import Point, HorizontalAlignment, VerticalAlignment
from Motio.ClickLogic import ClickAndDragHandler
from Motio.Configuration import Configs
import clr
import Motio.UI.Utils
clr.ImportExtensions(Motio.UI.Utils)
import Motio.NodeCore.Utils
clr.ImportExtensions(Motio.NodeCore.Utils)
import Motio.NodeImpl
clr.ImportExtensions(Motio.NodeImpl)

class CornerToolViewModel(BaseClass):

    def __new__(cls, *args):
        instance = BaseClass.__new__(cls, *args)
        instance.gizmos = []
        instance.dragStartPos = None
        instance.gizmoStartPos = None
        instance.props = []
        return instance

    def OnHide(self):
        #always call super method in ViewModels
        super(CornerToolViewModel, self).OnHide()
        #unsubscribe to viewport scaling event
        self._host.FindMainViewModel().RenderView.ContentScaleChanged -= self.content_scale_changed
        #unsubscribe to change event in position property
        if len(self.props) > 0:
            for prop in self.props:
                prop.PropertyChanged -= self.posPropChanged
                prop.Deleted -= self.selfDelete
        #unsubscribe to change in configs
        Configs.Instance.ConfigsChanged -= self.onConfigsChanged

        #remove current gizmo from viewport
        for gizmo in self.gizmos:
            Gizmo.Remove(gizmo)

    def UpdateDisplay(self):
        super(CornerToolViewModel, self).UpdateDisplay()
        self.updateGizmoPos()

    def getProperties(self):
        self._host.Original.Properties.WaitForProperty(self.Original.groupProperty)
        return self._host.Original.Properties[self.Original.groupProperty].Properties

    def selfDelete(self):
        #deleting the original will call OnDelete, the inverse is not true
        self.Original.Delete()

    def OnShow(self):
        #always call super method in ViewModels
        super(CornerToolViewModel, self).OnShow()
        # subscribe to viewport scaling event
        renderView = self._host.FindMainViewModel().RenderView
        renderView.ContentScaleChanged += self.content_scale_changed
        self.content_scale_changed(renderView.ContentScale)
        # subscribe to change in configs
        Configs.Instance.ConfigsChanged += self.onConfigsChanged

        # subscribe to change event in position property
        if len(self.props) == 0:
            properties = self.getProperties()
            self.props = [properties[i] for i in range(properties.Count)]

        # listener on every prop
        for prop in self.props:
            prop.Deleted += self.selfDelete
            prop.PropertyChanged += self.posPropChanged

        # if first time displaying gizmo, create and update it
        if len(self.gizmos) == 0:
            for prop in self.props:
                #create actual gizmo
                circle = self.createCircle()
                Gizmo.Add(circle)

                #create event handler for mouse hover
                circle.MouseEnter += self.mouseHover
                circle.MouseLeave += self.mouseLeave

                #create event handler for click and drag
                circleHandler = ClickAndDragHandler(circle)
                circleHandler.OnDrag = self.updatePos
                circleHandler.OnDragEnd = self.updatePosEnd

                self.gizmos.append(circle)
        else:
            for gizmo in self.gizmos:
                #if gzmo already created, show and update them
                Gizmo.Add(gizmo)

        self.updateGizmoPos()


    def Delete(self):
        #always call super method in ViewModels
        super(CornerToolViewModel, self).Delete()
        self.OnHide()
        self.gizmos = []

    def posPropChanged(self,sender,args):
        self.updateGizmoPos()

    def updateGizmoPos(self):
        for i in range(len(self.gizmos)):
            worldPos = self.props[i].GetCacheOrCalculateInForFrame(self._host.Original.GetTimeline().CurrentFrame)
            Gizmo.SetWorldPos(self.gizmos[i], worldPos.X, worldPos.Y)

    def content_scale_changed(self, scale):
        newScale = ((1/scale)/4)*Configs.GetValue(Configs.MoveGizmoSize)
        if len(self.gizmos) > 0:
            for gizmo in self.gizmos:
                gizmo.RenderTransform.Children[1].ScaleX = newScale
                gizmo.RenderTransform.Children[1].ScaleY = newScale

    def createCircle(self):
        scale = (self._host.FindMainViewModel().RenderView.InverseContentScale/4)*Configs.GetValue(Configs.MoveGizmoSize)
        circle = Ellipse()
        circle.Width = 50
        circle.Height = 50
        transformGroup = TransformGroup()
        transformGroup.Children.Add(TranslateTransform(-25,-25))
        transformGroup.Children.Add(ScaleTransform(scale, scale))
        circle.RenderTransform = transformGroup
        circle.Fill = Brushes.Blue
        circle.StrokeThickness = 0
        circle.Stroke = Brushes.DarkBlue
        return circle

    #mouse hover event handler
    def mouseHover(self, sender, args):
        sender.StrokeThickness = 8
        scale = (self._host.FindMainViewModel().RenderView.InverseContentScale/3.75)*Configs.GetValue(Configs.MoveGizmoSize)
        sender.RenderTransform.Children[1].ScaleX = scale
        sender.RenderTransform.Children[1].ScaleY = scale
    def mouseLeave(self, sender, args):
        sender.StrokeThickness = 0
        scale = (self._host.FindMainViewModel().RenderView.InverseContentScale/4)*Configs.GetValue(Configs.MoveGizmoSize)
        sender.RenderTransform.Children[1].ScaleX = scale
        sender.RenderTransform.Children[1].ScaleY = scale

    #config change handler
    def onConfigsChanged(self):
        scale = (self._host.FindMainViewModel().RenderView.InverseContentScale/4)*Configs.GetValue(Configs.MoveGizmoSize)
        if len(self.gizmos) > 0:
            for gizmo in self.gizmos:
                gizmo.RenderTransform.Children[1].ScaleX = scale
                gizmo.RenderTransform.Children[1].ScaleY = scale

    #click and drag event handler
    def updatePos(self, dragEvent):
        cursorPosCanvas = dragEvent.currentEvent.GetPosition(Gizmo.Canvas)
        cursorPosWorld = self._host.FindMainViewModel().AnimationTimeline.Canv2World(Point(cursorPosCanvas.X,cursorPosCanvas.Y))
        propIndex = self.gizmos.index(dragEvent.currentEvent.Source)
        #first drag call
        if not self.dragStartPos:
            self.dragStartPos = cursorPosWorld
            self.gizmoStartPos = self.props[propIndex].StaticValue
        deltaCursor =  cursorPosWorld - self.dragStartPos
        self.props[propIndex].StaticValue = Vector2(self.gizmoStartPos.X + deltaCursor.X, self.gizmoStartPos.Y + deltaCursor.Y)
        self.updateGizmoPos()

    def updatePosEnd(self, dragEvent):
        self.dragStartPos = None
        self.gizmoStartPos = None