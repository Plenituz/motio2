using Motio.Animation;
using Motio.Geometry;
using Motio.NodeCommon.Utils;
using Motio.NodeCore.Utils;
using Motio.NodeImpl.NodePropertyTypes;
using Motio.ObjectStoring;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Motio.NodeImpl.PropertyAffectingNodes
{
    public class AnimationVector3PropertyNode : AnimationPropertyNode
    {
        public static new IList<Type> AcceptedPropertyTypes = new Type[] { typeof(Vector3) };

        private FrameRange totalFrameRange = new FrameRange();
        private IFrameRange frameRangeY = FrameRange.Empty;
        private IFrameRange frameRangeZ = FrameRange.Empty;
        public override IFrameRange IndividualCalculationRange => totalFrameRange;

        [SaveMe]
        public KeyframeHolder keyframeHolderY { get; set; } = new KeyframeHolder();
        [SaveMe]
        public KeyframeHolder keyframeHolderZ { get; set; } = new KeyframeHolder();

        public override void Delete()
        {
            base.Delete();
            keyframeHolderY.PropertyChanged -= KeyframeHolder_PropertyChanged;
            keyframeHolderZ.PropertyChanged -= KeyframeHolder_PropertyChanged;
        }

        protected override void SetupNode()
        {
            base.SetupNode();
            keyframeHolderY.PropertyChanged += KeyframeHolder_PropertyChanged;
            keyframeHolderZ.PropertyChanged += KeyframeHolder_PropertyChanged;
        }

        protected override void UpdateFrameRange()
        {
            base.UpdateFrameRange();
            frameRangeY = UpdateFrameRange(frameRangeY, keyframeHolderY);
            frameRangeZ = UpdateFrameRange(frameRangeZ, keyframeHolderZ);
            totalFrameRange = new FrameRange();
            totalFrameRange.Add(frameRange);
            totalFrameRange.Add(frameRangeY);
            totalFrameRange.Add(frameRangeZ);
        }

        protected override void PropertyHost_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.PropertyHost_PropertyChanged(sender, e);
            if (e.PropertyName.Equals(nameof(Vector3NodeProperty.X)))
                AddOrModifyKeyframeValue(keyframeHolder, ((Vector3NodeProperty)propertyHost).X);

            if (e.PropertyName.Equals(nameof(Vector3NodeProperty.Y)))
                AddOrModifyKeyframeValue(keyframeHolderY, ((Vector3NodeProperty)propertyHost).Y);

            if (e.PropertyName.Equals(nameof(Vector3NodeProperty.Z)))
                AddOrModifyKeyframeValue(keyframeHolderZ, ((Vector3NodeProperty)propertyHost).Z);
            if (e.PropertyName.Equals(nameof(Vector3NodeProperty.StaticValue)))
                AddOrModifyKeyframeAtCurrentTime();
        }

        protected override void AddOrModifyKeyframeAtCurrentTime()
        {
            Vector3 point = (Vector3)propertyHost.StaticValue;
            AddOrModifyKeyframeValue(keyframeHolder, point.X);
            AddOrModifyKeyframeValue(keyframeHolderY, point.Y);
            AddOrModifyKeyframeValue(keyframeHolderZ, point.Z);
        }

        protected virtual void AddOrModifyKeyframeValue(KeyframeHolder holder, float value)
        {
            int currentFrame = this.GetTimeline().CurrentFrame;
            KeyframeFloat keyframeAtCurrentTime = holder.GetKeyframeAtTime(currentFrame);
            if (keyframeAtCurrentTime != null)
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
            float? z = keyframeHolderZ.GetValueAtTime(frame);
            Vector3 staticVal = (Vector3)propertyHost.StaticValue;
            Vector3 vect = new Vector3(x ?? staticVal.X, y ?? staticVal.Y, z ?? staticVal.Z);

            dataFeed.SetChannelData(PROPERTY_OUT_CHANNEL, vect);
        }
    }
}
