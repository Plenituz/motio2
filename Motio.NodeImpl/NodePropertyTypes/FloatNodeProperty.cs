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
    public class FloatNodeProperty : NodePropertyBase
    {
        private ScriptScope pythonScope;

        public override bool IsKeyframable => true;

        /// <summary>
        /// Minimum value for this property, only taken in account if <see cref="HardRangeFrom"/> is true
        /// </summary>
        [SaveMe]
        public float? RangeFrom { get; set; } = null;
        /// <summary>
        /// Maximum value for this property, only taken in account if <see cref="HardRangeTo"/> is true
        /// </summary>
        [SaveMe]
        public float? RangeTo { get; set; } = null;
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

        public float FloatValue => _staticValue;
        protected float _staticValue = 0;
        public override object StaticValue 
        {
            get => _staticValue;
            set
            {
                float inValue = ToolBox.ConvertToFloat(value);
                inValue = ApplyClamp(inValue);
                _staticValue = inValue;
              //  OnPropertyChanged(nameof(StaticValue));
            }
        }

        private float ApplyClamp(float inValue)
        {
            if (HardRangeTo && RangeTo.HasValue && inValue > RangeTo)
                inValue = RangeTo.Value;
            if (HardRangeFrom && RangeFrom.HasValue && inValue < RangeFrom)
                inValue = RangeFrom.Value;
            return inValue;
        }

        public FloatNodeProperty(Node nodeHost) : base(nodeHost)
        {
        }

        public FloatNodeProperty(Node nodeHost, string description, string name)
            : base(nodeHost, description, name)
        {
        }

        public void SetRangeTo(float rangeTo, bool hard)
        {
            this.RangeTo = rangeTo;
            this.HardRangeTo = hard;
        }

        public void SetRangeFrom(float rangeFrom, bool hard)
        {
            this.RangeFrom = rangeFrom;
            this.HardRangeFrom = hard;
        }

        /// <summary>
        /// this will get called when the user has click on the property value 
        /// and wants to set it. He might have typed an expression or something
        /// returns the error if there is one
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public Exception SetPropertyValueFromUserInput(string value)
        {
            object cachedVal =
                GetCache(nodeHost.GetTimeline().CurrentFrame, hiddenNodes.Count - 1);
            if (cachedVal == null)
                return new Exception("cached in value not available");

            if (pythonScope == null)
                pythonScope = Python.Engine.CreateScope();
            pythonScope.SetVariable("value", cachedVal);
            try
            {
                dynamic res = Python.Engine.Execute(value, pythonScope);
                StaticValue = ToolBox.ConvertToFloat(res);
            }
            catch(Exception e)
            {
                return e;
            }
            return null;
        }

        public override void CreateAnimationNode()
        {
            if(this.FindAnimationNode() == null)
            {
                //this is not the right way to do this if you add a normal node use TryAttachNode
                ForceAttachHiddenNode(new AnimationPropertyNode());
            }
        }

        public override DataFeed GetCache(int frame, int nodeNb)
        {
            DataFeed datafeed = base.GetCache(frame, nodeNb);
            if (datafeed.TryGetChannelData(Node.PROPERTY_OUT_CHANNEL, out float outFloatVal))
            {
                outFloatVal = ApplyClamp(outFloatVal);
                datafeed.SetChannelData(Node.PROPERTY_OUT_CHANNEL, outFloatVal);
            }
            return datafeed;
        }
    }
}
