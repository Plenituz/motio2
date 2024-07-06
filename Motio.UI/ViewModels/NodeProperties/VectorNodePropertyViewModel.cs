using Motio.Animation;
using Motio.Geometry;
using Motio.NodeImpl;
using Motio.NodeImpl.NodePropertyTypes;
using Motio.NodeImpl.PropertyAffectingNodes;
using Motio.UI.Utils;
using Motio.UI.Views.PropertyDisplays;
using Motio.UI.Views.TimelineDisplays;
using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace Motio.UI.ViewModels
{
    public class VectorNodePropertyViewModel : NodePropertyBaseViewModel, IProxy<VectorNodeProperty>, INotifyPropertyChanged
    {
        private VectorNodeProperty _original;
        VectorNodeProperty IProxy<VectorNodeProperty>.Original => _original;

        VectorPropertyDisplay _propertyDisplay;
        public override ContentControl PropertyDisplay => _propertyDisplay ?? (_propertyDisplay = new VectorPropertyDisplay());
        VectorTimelineDisplay _timelineDisplay;
        public override ContentControl TimelineDisplay => _timelineDisplay ?? (_timelineDisplay = new VectorTimelineDisplay());

        public Vector2 VectorValue { get => (Vector2)_original.StaticValue; set => _original.StaticValue = value; }
        public float X { get => _original.X; set => _original.X = value; }
        public float Y { get => _original.Y; set => _original.Y = value; }
        public bool Uniform { get => _original.uniform; set => _original.uniform = value; }
        public float Sensitivity { get; set; } = 0.1f;
        public KeyframeHolder KeyframeHolderY
        {
            get
            {
                AnimationVectorPropertyNode an = (AnimationVectorPropertyNode)_original.FindAnimationNode();
                if (an != null)
                    return an.keyframeHolderY;
                return null;
            }
            //set { }
        }

        public VectorNodePropertyViewModel(VectorNodeProperty property) : base(property)
        {
            this._original = property;
        }

        public override void CreateAnimationNode()
        {
            base.CreateAnimationNode();
            KeyframeHolderY.PropertyChanged += KeyframeHolder_PropertyChanged;
        }

        protected override void KeyframeHolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("keyframes") || e.PropertyName.Equals("Time"))
            {
                //update both keyframeholders though the weaver
                OnPropertyChanged(nameof(KeyframeHolder));
                OnPropertyChanged(nameof(KeyframeHolderY));
                //KeyframeHolder = null;
                //KeyframeHolderY = null;
            }
        }

        public virtual Exception SetXFromUserInput(string value)
        {
            return _original.SetXFromUserInput(value);
        }

        public virtual Exception SetYFromUserInput(string value)
        {
            return _original.SetYFromUserInput(value);
        }
    }
}
