import Motio.UI.ViewModels.NodeToolViewModel as BaseClass
from Motio.UI.Gizmos import Gizmo
from System.Windows.Media import Brushes
from System.Windows.Shapes import Rectangle
from System.Windows.Shapes import Ellipse

import clr
import Motio.UI.Utils
clr.ImportExtensions(Motio.UI.Utils)

class TestPyNodeToolViewModel(BaseClass):

    def __new__(cls, *args):
        instance = BaseClass.__new__(cls, *args)
        instance.gizmos = []
        return instance

    def content_scale_changed(self, scale):
        print "scale changed:%s" % scale
        self.update_circles(scale)

    def update_circles(self, scale):
        scale = 1/scale
        for gizmo in self.gizmos:
            if type(gizmo) == Ellipse:
                gizmo.Width = scale*8
                gizmo.Height = scale*8

    #def get_Icon(self):
    #    return "IC"   

    def Delete(self):
        #always call super method in ViewModels
        super(TestPyNodeToolViewModel, self).Delete()
        print "tool deleted, unsubscribing from all events"

    def UpdateDisplay(self):
        super(TestPyNodeToolViewModel, self).UpdateDisplay()
        print "updating gizmos"

    def OnHide(self):
        super(TestPyNodeToolViewModel, self).OnHide()
        print "tool hidden, hidding gizmos..."
        self._host.FindMainViewModel().RenderView.ContentScaleChanged -= self.content_scale_changed
        for gizmo in self.gizmos:
            Gizmo.Remove(gizmo)

    def OnShow(self):
        #always call super method in ViewModels
        super(TestPyNodeToolViewModel, self).OnShow()
        print "tool shown, displaying gizmos..."
        self._host.FindMainViewModel().RenderView.ContentScaleChanged += self.content_scale_changed
        for gizmo in self.gizmos:
            Gizmo.Add(gizmo)

    def OnSelect(self):
        super(TestPyNodeToolViewModel, self).OnSelect()
        print "tool selected"

    def OnDeselect(self):
        super(TestPyNodeToolViewModel, self).OnDeselect()
        print "tool deselected"

    def OnClickInViewport(self, ev, worldPos, canvasPos):
        super(TestPyNodeToolViewModel, self).OnClickInViewport(ev, worldPos, canvasPos)
        print "user clicked on viewport while this tool was selected"
        #Gizmo.AddCircle add the gizmo to the canvas and returns it 
        ellipse = Gizmo.AddCircle(canvasPos, 8*self._host.FindMainViewModel().RenderView.InverseContentScale, Brushes.Brown, False)
        #to remove the gizmos OnHide we need to keep track of them
        self.gizmos.append(ellipse)

    def OnDragEnterInViewport(self, dragEvent):
        super(TestPyNodeToolViewModel, self).OnDragEnterInViewport(dragEvent)
        print "user started to drag on viewport while this tool was selected"
        rect = Rectangle()
        rect.Fill = Brushes.Blue
        rect.Stroke = Brushes.Black
        rect.StrokeThickness = 10
        self.drawingRect = rect
        self.gizmos.append(rect)
        Gizmo.Add(rect)

    def OnDragInViewport(self, dragEvent):
        super(TestPyNodeToolViewModel, self).OnDragInViewport(dragEvent)
        print "user dragged on viewport while this tool was selected"
        self.adapt_rect(dragEvent.startPos, dragEvent.currentPos)

    def OnDragEndInViewport(self, dragEvent):
        super(TestPyNodeToolViewModel, self).OnDragEndInViewport(dragEvent)
        print "user finished to drag on viewport while this tool was selected"
        self.drawingRect = None

    def adapt_rect(self, startPos, endPos):
        if self.drawingRect == None:
            return
        left = min(startPos.X, endPos.X)
        top = min(startPos.Y, endPos.Y)
        Gizmo.SetCanvasPos(self.drawingRect, left, top)
        self.drawingRect.Width = abs(startPos.X - endPos.X)
        self.drawingRect.Height = abs(startPos.Y - endPos.Y)
