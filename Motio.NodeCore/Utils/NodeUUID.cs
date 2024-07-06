using Motio.ObjectStoring;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;

namespace Motio.NodeCore.Utils
{
    public class NodeUUID
    {
        [SaveMe]
        //don't set this without telling the group that you use this uuid
        private string uuid;
        public Node host;
        private NodeUUIDGroup group;

        public NodeUUID(Node host, NodeUUIDGroup group)
        {
            this.host = host;
            this.group = group;
            this.uuid = group.UnusedId();
            group.Use(uuid, this);
        }

        private NodeUUID(Node host)
        {
            this.host = host;
        }

        [CreateLoadInstance]
        static object CreateInstance(Node parent, Type type)
        {
            return new NodeUUID(parent);
        }

        [CustomSaver]
        object Saver()
        {
            return new Hashtable() { { "uuid", uuid } };
        }

        [CustomLoader]
        void Loader(JObject jobj)
        {
            this.uuid = (string)jobj.Property("uuid").Value;
        }

        [OnAllLoaded]
        void AllLoaded(JObject jobj)
        {
            group = host.GetTimeline().uuidGroup;
            group.Use(this.uuid, this);
        }

        public override string ToString()
        {
            return uuid;
        }
    }
}
