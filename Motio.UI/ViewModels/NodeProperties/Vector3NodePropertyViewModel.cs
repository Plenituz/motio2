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
    public class Vector3NodePropertyViewModel : NodePropertyBaseViewModel, IProxy<Vector3NodeProperty>, INotifyPropertyChanged
    {
        Vector3NodeProperty _original;
        Vector3NodeProperty IProxy<Vector3NodeProperty>.Original => _original;

        Vector3PropertyDisplay _propertyDisplay;
        public override ContentControl PropertyDisplay => _propertyDisplay ?? (_propertyDisplay = new Vector3PropertyDisplay());
        Vector3TimelineDisplay _timelineDisplay;
        public override ContentControl TimelineDisplay => _timelineDisplay ?? (_timelineDisplay = new Vector3TimelineDisplay());

        public Vector3 VectorValue { get => (Vector3)_original.StaticValue; set => _original.StaticValue = value; }
        public float X { get => _original.X; set => _original.X = value; }
        public float Y { get => _original.Y; set => _original.Y = value; }
        public float Z { get => _original.Z; set => _original.Z = value; }
        public float Sensitivity { get; set; } = 0.1f;
        public KeyframeHolder KeyframeHolderY
        {
            get
            {
                AnimationVector3PropertyNode an = (AnimationVector3PropertyNode)_original.FindAnimationNode();
                if (an != null)
                    return an.keyframeHolderY;
                return null;
            }
            //set { }
        }
        public KeyframeHolder KeyframeHolderZ
        {
            get
            {
                AnimationVector3PropertyNode an = (AnimationVector3PropertyNode)_original.FindAnimationNode();
                if (an != null)
                    return an.keyframeHolderZ;
                return null;
            }
            //set { }
        }

        public Vector3NodePropertyViewModel(Vector3NodeProperty property) : base(property)
        {
            this._original = property;
        }

        public override void CreateAnimationNode()
        {
            base.CreateAnimationNode();
            KeyframeHolderY.PropertyChanged += KeyframeHolder_PropertyChanged;
            KeyframeHolderZ.PropertyChanged += KeyframeHolder_PropertyChanged;
        }

        protected override void KeyframeHolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("keyframes") || e.PropertyName.Equals("Time"))
            {
                //update both keyframeholders though the weaver
                OnPropertyChanged(nameof(KeyframeHolder));
                OnPropertyChanged(nameof(KeyframeHolderY));
                OnPropertyChanged(nameof(KeyframeHolderZ));
                //KeyframeHolder = null;
                //KeyframeHolderY = null;
                //KeyframeHolderZ = null;
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

        public Exception SetZFromUserInput(string value)
        {
            return _original.SetZFromUserInput(value);
        }
    }
}
