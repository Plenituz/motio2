using Motio.Animation;
using Motio.Geometry;
using Motio.NodeCommon.Utils;
using Motio.NodeCore.Utils;
using Motio.NodeImpl.NodePropertyTypes;
using Motio.NodeImpl.PropertyAffectingNodes;
using Motio.ObjectStoring;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Motio.NodeImpl.PropertyAffectingNodes
{
    public class AnimationVectorPropertyNode : AnimationPropertyNode
    {
        public static new IList<Type> AcceptedPropertyTypes = new Type[] { typeof(Vector2) };

        private FrameRange totalFrameRange = new FrameRange();
        private IFrameRange frameRangeY = FrameRange.Empty;
        public override IFrameRange IndividualCalculationRange => totalFrameRange;

        [SaveMe]
        public KeyframeHolder keyframeHolderY { get; set; } = new KeyframeHolder();

        public override void Delete()
        {
            base.Delete();
            keyframeHolderY.PropertyChanged -= KeyframeHolder_PropertyChanged;
        }

        protected override void UpdateFrameRange()
        {
            base.UpdateFrameRange();
            frameRangeY = UpdateFrameRange(frameRangeY, keyframeHolderY);
            totalFrameRange = new FrameRange();
            totalFrameRange.Add(frameRange);
            totalFrameRange.Add(frameRangeY);
        }

        protected override void SetupNode()
        {
            base.SetupNode();
            keyframeHolderY.PropertyChanged += KeyframeHolder_PropertyChanged;
        }

        protected override void PropertyHost_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(VectorNodeProperty.X)))
                AddOrModifyKeyframeValue(keyframeHolder, ((VectorNodeProperty)propertyHost).X);
            if (e.PropertyName.Equals(nameof(VectorNodeProperty.Y)))
                AddOrModifyKeyframeValue(keyframeHolderY, ((VectorNodeProperty)propertyHost).Y);
            if (e.PropertyName.Equals(nameof(VectorNodeProperty.StaticValue)))
                AddOrModifyKeyframeAtCurrentTime();
        }

        protected override void AddOrModifyKeyframeAtCurrentTime()
        {
            Vector2 point = (Vector2)propertyHost.StaticValue;
            AddOrModifyKeyframeValue(keyframeHolder, point.X);
            AddOrModifyKeyframeValue(keyframeHolderY, point.Y);
        }

        protected virtual void AddOrModifyKeyframeValue(KeyframeHolder holder, float value)
        {
            int currentFrame = this.GetTimeline().CurrentFrame;
            KeyframeFloat keyframeAtCurrentTime = holder.GetKeyframeAtTime(currentFrame);
            if(keyframeAtCurrentTime != null)
            {
                keyframeAtCurrentTime.Value = value;
            }
            else
            {
                holder.AddKeyframe(
                    new KeyframeFloat(currentFrame, value)
                    {
                        LeftHandle = new Vector2(-10, 0),
                        RightHandle = new Vector2(10, 0)
                    });
            }
        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            float? x = keyframeHolder.GetValueAtTime(frame);
            float? y = keyframeHolderY.GetValueAtTime(frame);
            Vector2 staticVal = (Vector2)propertyHost.StaticValue;
            Vector2 vect = new Vector2(x ?? staticVal.X, y ?? staticVal.Y);

            dataFeed.SetChannelData(PROPERTY_OUT_CHANNEL, vect);
        }
    }
}
