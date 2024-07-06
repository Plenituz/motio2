using Motio.NodeCommon.StandardInterfaces;
using Motio.NodeCore;
using Motio.ObjectStoring;
using Newtonsoft.Json.Linq;

namespace Motio.NodeCore
{
    public class GroupNodeProperty : NodePropertyBase
    {
        public override bool IsKeyframable => false;
        public override object StaticValue { get { return 0; } set { } }

        [SaveMe]
        public PropertyGroup Properties { get; set; }

        [CustomLoader]
        void OnLoad(JObject jobj)
        {
            foreach (JProperty jprop in jobj.Properties())
            {
                if (jprop.Name.Equals(TimelineSaver.TYPE_NAME))
                    continue;
                if(!jprop.Name.Equals("Properties"))
                {
                    object This = this;
                    TimelineLoader.SetPropertyValue(ref This, jprop);
                }
                else
                {
                    //we have to override the loading process only for the Properties property
                    //because we have to pass the PropertyGroupNoSetup as a parent when creating the NodeProperty objects 
                    //otherwise the parent would be some shit that makes everything crash
                    JObject propertiesJObj = (JObject)jprop.Value;
                    //create the Properties object
                    Properties = (PropertyGroup)TimelineLoader.CreateObjectInstance(propertiesJObj, typeof(PropertyGroupNoSetup), nodeHost);
                    //create each NodeProperty and add them to the 
                    foreach (JProperty propertyJprop in propertiesJObj.Properties())
                    {
                        NodePropertyBase nodeProp = (NodePropertyBase)TimelineLoader.LoadObjectFromJson(propertyJprop.Value, parent: Properties);
                        Properties.AddManually(propertyJprop.Name, nodeProp);
                        //don't forget to call SetHost
                        ((IHasHost)nodeProp).Host = nodeHost;
                    }
                }
            }
        }

        public GroupNodeProperty(Node nodeHost) : base(nodeHost)
        {
            Properties = new PropertyGroupNoSetup(nodeHost);
        }

        public GroupNodeProperty(Node nodeHost, string description, string name)
            : base(nodeHost, description, name)
        {
            Properties = new PropertyGroupNoSetup(nodeHost);
        }

        public override void InvalidateAllCachedFrames(PropertyAffectingNode fromNode)
        {
            throw new System.Exception("GroupNodeProperty can't be invalidated");
        }

        public override void InvalidateCache(int frame, PropertyAffectingNode fromNode)
        {
            throw new System.Exception("GroupNodeProperty can't be invalidated");
        }

        public override void CreateAnimationNode()
        {
            
        }

        public override void Delete()
        {
            base.Delete();
            Properties.Delete();
        }
    }
}
