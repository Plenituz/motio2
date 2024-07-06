using System;
using System.ComponentModel;
using System.Collections.Generic;
using Motio.NodeCore;
using Motio.ObjectStoring;
using Motio.Animation;
using Motio.NodeCore.Utils;
using Motio.Geometry;
using Motio.NodeCommon.Utils;
using Motio.NodeCommon;

namespace Motio.NodeImpl.PropertyAffectingNodes
{
    public class AnimationPropertyNode : PropertyAffectingNode
    {
        public static IList<Type> AcceptedPropertyTypes = new Type[] { typeof(double) };

        protected IFrameRange frameRange = FrameRange.Empty;
        public override IFrameRange IndividualCalculationRange => frameRange;

        [SaveMe]
        public KeyframeHolder keyframeHolder { get; set; } = new KeyframeHolder();
        /// <summary>
        /// used to make sur we don't listen to several properties at once
        /// </summary>
        private NodePropertyBase oldAttachedTo = null;

        public override void Delete()
        {
            base.Delete();
            keyframeHolder.PropertyChanged -= KeyframeHolder_PropertyChanged;
            if (oldAttachedTo != null)
            {
                oldAttachedTo.PropertyChanged -= PropertyHost_PropertyChanged;
            }
        }

        protected override void SetupNode()
        {
            keyframeHolder.PropertyChanged += KeyframeHolder_PropertyChanged;
        }

        public override void SetupProperties()
        {

        }

        protected virtual void UpdateFrameRange()
        {
            frameRange = UpdateFrameRange(frameRange, keyframeHolder);
        }

        protected IFrameRange UpdateFrameRange(IFrameRange frameRange, KeyframeHolder keyframeHolder)
        {
            if (keyframeHolder.Count == 0)
            {
                if(!(frameRange is EmptyFrameRange))
                    frameRange = FrameRange.Empty;
                return frameRange;
            }

            return new ClosedFrameRange(
                keyframeHolder.KeyframeAt(0).Time,
                keyframeHolder.KeyframeAt(keyframeHolder.Count - 1).Time);
        }

        protected virtual void KeyframeHolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //TODO only invalidate this property here 
            //en fait quand tu invalide une propriété tu dois invalider la graphics node aussi
            //pour devoir recalculer, mais de toute maniere les autres propriété etant
            //pas nvalidé ca va pas causer beaucoup de calcul
            if (propertyHost != null)
                propertyHost.InvalidateAllCachedFrames(this);
            UpdateFrameRange();
        }

        protected virtual void PropertyHost_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(NodePropertyBase.StaticValue)))
            {
                AddOrModifyKeyframeAtCurrentTime();
            }
        }

        public override void AttachToProperty(NodePropertyBase property)
        {
            base.AttachToProperty(property);

            SetupListener();

            if(keyframeHolder.Count == 0)
                AddOrModifyKeyframeAtCurrentTime();
        }        

        protected virtual void SetupListener()
        {
            //init the keyframe holder when you get attached to a property
            //since we are listening to changes in static value we need to check if the property we are on didn't change
            if (oldAttachedTo != null)
            {
                oldAttachedTo.PropertyChanged -= PropertyHost_PropertyChanged;
            }
            oldAttachedTo = propertyHost;
            propertyHost.PropertyChanged += PropertyHost_PropertyChanged;
        }

        protected virtual void AddOrModifyKeyframeAtCurrentTime()
        {
            int currentFrame = this.GetTimeline().CurrentFrame;
            KeyframeFloat keyframeAtCurrentTime = keyframeHolder.GetKeyframeAtTime(currentFrame);

            //work around so I don't have to write another AnimationPropertyNode just for int
            bool isInt = false;
            if (propertyHost.StaticValue.GetType() == typeof(int))
                isInt = true;

            if (keyframeAtCurrentTime != null)
            {
                if (isInt)
                    keyframeAtCurrentTime.Value = (int)propertyHost.StaticValue;
                else
                    keyframeAtCurrentTime.Value = (float)propertyHost.StaticValue;
            }
            else
            {
                keyframeHolder.AddKeyframe(
                    new KeyframeFloat(currentFrame,
                    isInt ? (int)propertyHost.StaticValue : (float)propertyHost.StaticValue)
                    {
                        LeftHandle = new Vector2(-10, 0),
                        RightHandle = new Vector2(10, 0)
                    });
            }
        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            object outData = keyframeHolder.GetValueAtTime(frame);

            if(outData == null)
                outData = propertyHost.StaticValue;

            if (propertyHost.StaticValue.GetType() == typeof(int))
                outData = ToolBox.ConvertToInt(outData);
            
            dataFeed.SetChannelData(PROPERTY_OUT_CHANNEL, outData ?? propertyHost.StaticValue);
        }
    }
}
