using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Motio.NodeCore;
using Motio.ObjectStoring;
using Motio.NodeCore.Utils;
using Motio.NodeImpl.NodePropertyTypes;
using System.Collections.Concurrent;
using Motio.NodeCommon.Utils;

namespace Motio.NodeImpl.PropertyAffectingNodes
{
    public class CopyPropertyNode : PropertyAffectingNode
    {
        [OnAllLoaded]
        void AllLoaded(JObject jobj)
        {
            var prop = Properties["ref"];
            prop.ValidateValue(prop.StaticValue);
        }

        public static string ClassNameStatic = "Copy";
        public static IList<Type> AcceptedPropertyTypes = new Type[] { typeof(object) };

        private NodePropertyBase _propertyToCopy;
        public NodePropertyBase PropertyToCopy
        {
            get => _propertyToCopy;
            set
            {
                if (value == _propertyToCopy)
                    return;
                CacheManager cacheManager = propertyHost.nodeHost.GetTimeline().CacheManager;
                propertyHost.nodeHost.FindGraphicsNode(out var gAffMe);

                if (_propertyToCopy != null)
                {
                    _propertyToCopy.nodeHost.FindGraphicsNode(out var gAffToCopyOld);
                    if(gAffMe != gAffToCopyOld)
                        cacheManager.UnregisterDependant(gAffToCopyOld, gAffMe);
                }
                if(value != null)
                {
                    value.nodeHost.FindGraphicsNode(out var gAffNew);
                    if(gAffMe != gAffNew)
                        cacheManager.RegisterDependant(gAffNew, gAffMe);
                }
                _propertyToCopy = value;
            }
        }
        private ConcurrentDictionary<int, byte> antiLoopDict = new ConcurrentDictionary<int, byte>();

        public override IFrameRange IndividualCalculationRange => PropertyToCopy?.CalculationRange ?? FrameRange.Empty;

        public override void SetupProperties()
        {
            Properties.Add("ref",
                new StringNodeProperty(this, "Path to the property to copy\nHere is the syntax:" 
                    + "\n\t[Node UUID].[PropertyName]\nOr simply right-click on the property -> copy path", "Path"), "");
        }

        public override Exception ValidatePropertyValue(NodePropertyBase property, object value)
        {
            if (property == Properties["ref"])
            {
                Exception ex = this.GetTimeline().uuidGroup.LookupProperty((string)value, out Node node, out var tmpToCopy);
                PropertyToCopy = tmpToCopy;
                if (ex != null)
                    return ex;
            }
            return null;
        }

        public override void Prepare()
        {
            base.Prepare();
            antiLoopDict.Clear();
            Properties["ref"].ClearError(2);
        }

        public override void EvaluateFrame(int frame, DataFeed dataFeed)
        {
            if (antiLoopDict.ContainsKey(frame))
            {
                Properties["ref"].SetError(2, "You created an infinite loop!");
                return;
            }
            antiLoopDict.TryAdd(frame, 0);

            if (PropertyToCopy == null)
                return;
            object inData = dataFeed.GetChannelData(PROPERTY_OUT_CHANNEL);
            if(inData.GetType() != PropertyToCopy.StaticValue.GetType())
            {
                Properties["ref"].SetError(1, "the copied property is not the same type as the hosting property");
                return;
            }
            else if(Properties["ref"].HasError(1))
            {
                Properties["ref"].ClearError(1);
            }

            object copiedData = PropertyToCopy.GetCacheOrCalculateEndOfChain(frame).GetChannelData(PROPERTY_OUT_CHANNEL);
            dataFeed.SetChannelData(PROPERTY_OUT_CHANNEL, copiedData);
        }
    }
}
