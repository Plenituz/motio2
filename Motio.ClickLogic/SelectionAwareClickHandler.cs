using Motio.Selecting;
using Motio.UICommon;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Motio.ClickLogic
{
    public delegate void UpdateSelectable<SelectableType>(SelectableType selectable, Point position);

    public class SelectionAwareClickHandler<SelectableType> where SelectableType : UIElement, ISelectable
    {
        private bool moved = false;
        private Point downPos;
        /// <summary>
        /// selection saved on down 
        /// </summary>
        private Dictionary<SelectableType, Point> savedSelection = new Dictionary<SelectableType, Point>();

        public event UpdateSelectable<SelectableType> UpdatePosition;
        public event MouseEventHandler Click;
        /// <summary>
        /// container used for click positions, can be changed but must not be null
        /// </summary>
        public IInputElement container;
        public readonly string selectionGroup;

        public SelectionAwareClickHandler(IInputElement container, string selectionGroup)
        {
            this.container = container;
            this.selectionGroup = selectionGroup;
        }

        public void Hook(SelectableType element)
        {
            element.MouseDown += MouseDown;
            element.MouseMove += MouseMove;
            element.MouseUp += MouseUp;
            element.GotMouseCapture += GotMouseCapture;
            element.LostMouseCapture += LostMouseCapture;
        }

        public void UnHook(SelectableType element)
        {
            element.MouseDown -= MouseDown;
            element.MouseMove -= MouseMove;
            element.MouseUp -= MouseUp;
            element.GotMouseCapture -= GotMouseCapture;
            element.LostMouseCapture -= LostMouseCapture;
        }

        private void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is SelectableType ellipse && Mouse.Captured == null)
            {
                moved = false;
                downPos = e.GetPosition(container);
                ellipse.CaptureMouse();
            }
        }

        private void GotMouseCapture(object sender, MouseEventArgs e)
        {
            //Logger.WriteLine("capture");
            if (sender is SelectableType ellipse)
            {
                //si tu appuie sur shift au moment de clicker, c'est sur qu'il faut add 
                //l'ellipse a la selection
                //par contre si tu click pas c'est pas sur qu'il faut select
                //car ca pourait etre un click and drag
                //par contre si la selection est vide ou contient pas l'ellipse sur laquelle
                //on click ca veut dire qu'on a deja une selection
                //et qu'on vient de cliquer sur une keyframe qui est pas select
                //donc on clear la selection et on selectionne la keyframe cliquée

                //List<SelectableType> selection = Selection.ListType<SelectableType>();
                if (ToolBox.IsShiftHeld())
                {
                    Selection.Add(selectionGroup, ellipse);
                }
                else if (Selection.GroupCount(selectionGroup) == 0 || !Selection.GroupContains(selectionGroup, ellipse))
                {
                    Selection.ClearGroup(selectionGroup);
                    Selection.Add(selectionGroup, ellipse);
                }

                //save the current selection and the related position of 
                //each keyframe
                //so we can move them all at once 
                savedSelection.Clear();
                foreach (object selected in Selection.ListGroup(selectionGroup))
                {
                    if (!(selected is SelectableType filterType))
                        throw new System.Exception("found a " + selected.GetType()
                            + " in selection group " + selectionGroup
                            + ". I wasn't expecting that");
                    savedSelection.Add(
                        filterType,
                        new Point(Canvas.GetLeft(filterType),
                                  Canvas.GetTop(filterType))
                        );
                }
            }
        }

        private void MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is SelectableType ellipse && Mouse.Captured == ellipse)
            {
                //Logger.WriteLine("move");
                //attention : si ya un pb ici : 
                //peut etre qu'il faut voir la 
                //distance avant de pouvoir dire que ca a move
                moved = true;
                Vector deltaClick = e.GetPosition(container) - downPos;
                foreach (KeyValuePair<SelectableType, Point> pair in savedSelection)
                {
                    UpdatePosition?.Invoke(pair.Key, pair.Value + deltaClick);
                }
            }
        }

        private void MouseUp(object sender, MouseButtonEventArgs e)
        {
            //Logger.WriteLine("up");
            if (sender is SelectableType ellipse && Mouse.Captured == ellipse)
            {
                ellipse.ReleaseMouseCapture();
            }
        }

        private void LostMouseCapture(object sender, MouseEventArgs e)
        {
            //Logger.WriteLine("uncap-----------------------------");
            if (sender is SelectableType ellipse)
            {
                if (moved)
                {
                    //si on a bougé, update les keyframe qui on bougé
                    Vector deltaClick = e.GetPosition(container) - downPos;
                    foreach (KeyValuePair<SelectableType, Point> pair in savedSelection)
                    {
                        //ca risque de fuké parceque assigner une valeur va trigger un 
                        //nouveau layout et savedSelection sera changé ?
                        Point newPosCanvasSpace = pair.Value + deltaClick;
                        UpdatePosition?.Invoke(pair.Key, newPosCanvasSpace);
                        /* pair.Key.Time = Convert.ToInt32(
                             CanvasSpaceToKeyframeSpace(newPosCanvasSpace));*/
                    }
                    savedSelection.Clear();
                }
                else
                {
                    Click?.Invoke(sender, e);
                    //si on a pas bougé, c'est que c'etait
                    //un click sur une ellipse, on la select
                    if (ToolBox.IsShiftHeld())
                    {
                        Selection.Add(selectionGroup, ellipse);
                    }
                    else
                    {
                        Selection.ClearGroup(selectionGroup);
                        Selection.Add(selectionGroup, ellipse);
                    }
                }
            }
        }
    }
}
