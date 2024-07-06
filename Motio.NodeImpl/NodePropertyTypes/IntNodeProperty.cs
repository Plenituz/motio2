using Microsoft.Scripting.Hosting;
using Motio.NodeCommon;
using Motio.NodeCommon.Utils;
using Motio.NodeCore;
using Motio.NodeCore.Utils;
using Motio.NodeImpl.PropertyAffectingNodes;
using Motio.ObjectStoring;
using Motio.PythonRunning;
using System;

namespace Motio.NodeImpl.NodePropertyTypes
{
    public class IntNodeProperty : NodePropertyBase
    {
        public IntNodeProperty(Node nodeHost) : base(nodeHost) { }

        public IntNodeProperty(Node nodeHost, string description, string name)
            : base(nodeHost, description, name) { }

        /// <summary>
        /// Minimum value for this property, only taken in account if <see cref="HardRangeFrom"/> is true
        /// </summary>
        [SaveMe]
        public int? RangeFrom { get; set; } = null;
        /// <summary>
        /// Maximum value for this property, only taken in account if <see cref="HardRangeTo"/> is true
        /// </summary>
        [SaveMe]
        public int? RangeTo { get; set; } = null;
        /// <summary>
        /// if this is set to true the value is garanteed to 
        /// be below <see cref="RangeTo"/> 
        /// </summary>
        [SaveMe]
        public bool HardRangeTo { get; set; }
        /// <summary>
        /// if this is set to true the value is garanteed to 
        /// be above <see cref="RangeFrom"/> 
        /// </summary>
        [SaveMe]
        public bool HardRangeFrom { get; set; }

        public override bool IsKeyframable => true;
        private ScriptScope pythonScope;

        private int _staticValue = 0;
        public override object StaticValue
        {
            get => _staticValue;
            set
            {
                int inValue = ToolBox.ConvertToInt(value);
                inValue = ApplyClamp(inValue);
                _staticValue = inValue;
            }
        }

        private int ApplyClamp(int inValue)
        {
            if (HardRangeTo && RangeTo.HasValue && inValue > RangeTo)
                inValue = RangeTo.Value;
            if (HardRangeFrom && RangeFrom.HasValue && inValue < RangeFrom)
                inValue = RangeFrom.Value;
            return inValue;
        }

        public void SetRangeTo(int rangeTo, bool hard)
        {
            this.RangeTo = rangeTo;
            this.HardRangeTo = hard;
        }

        public void SetRangeFrom(int rangeFrom, bool hard)
        {
            this.RangeFrom = rangeFrom;
            this.HardRangeFrom = hard;
        }

        public Exception SetPropertyValueFromUserInput(string value)
        {
            object cachedVal =
                GetCache(nodeHost.GetTimeline().CurrentFrame, hiddenNodes.Count - 1).GetChannelData(Node.PROPERTY_OUT_CHANNEL);
            if (cachedVal == null)
                return new Exception("cached in value not available");

            if (pythonScope == null)
                pythonScope = Python.Engine.CreateScope();
            pythonScope.SetVariable("value", cachedVal);
            try
            {
                dynamic res = Python.Engine.Execute(value, pythonScope);
                StaticValue = ToolBox.ConvertToInt(res);
            }
            catch (Exception e)
            {
                return e;
            }
            return null;
        }

        public override void CreateAnimationNode()
        {
            if (this.FindAnimationNode() == null)
            {
                //this is not the right way to do this if you add a normal node use TryAttachNode
                ForceAttachHiddenNode(new AnimationPropertyNode());
            }
        }

        public override DataFeed GetCache(int frame, int nodeNb)
        {
            DataFeed datafeed = base.GetCache(frame, nodeNb);
            if(datafeed.TryGetChannelData(Node.PROPERTY_OUT_CHANNEL, out int outIntVal))
            {
                outIntVal = ApplyClamp(outIntVal);
                datafeed.SetChannelData(Node.PROPERTY_OUT_CHANNEL, outIntVal);
            }
            return datafeed;
        }

    }
}
