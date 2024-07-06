using System;
using System.Collections.Generic;

namespace Motio.NodeCore.Utils
{
    public class NodeUUIDGroup
    {
        public const string UUID_LETTERS = "1234567890AZERTYUIOPQSDFGHJKLMWXCVBNazertyuiopqsdfghjklmwxcvbn%*$-=+#&";
        public const int UUID_SIZE = 8;

        private static Random random = new Random();
        private Dictionary<string, NodeUUID> usedUUIDs = new Dictionary<string, NodeUUID>();

        public static string RandomString()
        {
            string str = "";
            for (int i = 0; i < UUID_SIZE; i++)
            {
                str += UUID_LETTERS[random.Next(UUID_LETTERS.Length)];
            }
            return str;
        }

        public string UnusedId()
        {
            string str = RandomString();
            if (usedUUIDs.ContainsKey(str))
                return UnusedId();
            return str;
        }

        public void Use(string uuid, NodeUUID nodeUUID)
        {
            usedUUIDs.Add(uuid, nodeUUID);
        }

        public Node LookupNode(string uuid)
        {
            if(usedUUIDs.TryGetValue(uuid, out NodeUUID node))
                return node.host;
            return null;
        }

        public Exception LookupProperty(string uuidPath, out Node node, out NodePropertyBase property)
        {
            bool validPath = Separate(uuidPath, out string uuid, out string propName);
            if (!validPath)
            {
                node = null;
                property = null;
                return new Exception("Path couldn't be parsed, make sure the format is: [UUID].[unique name]");
            }

            node = LookupNode(uuid);
            if (node == null)
            {
                node = null;
                property = null;
                return new Exception("Couldn't find node with UUID " + uuid);
            }

            bool foundProp = node.Properties.TryGetProperty(propName, out property);
            if (!foundProp)
            {
                return new Exception("Couldn't find property named " + propName
                    + " on node " + node.UserGivenName + " make sur you use the unique name, not the displayed name");
            }

            return null;
        }

        private bool Separate(string input, out string uuid, out string propName)
        {
            string[] split = input.Split(new char[] { '.' }, 2);
            if (split.Length != 2)
            {
                uuid = null;
                propName = null;
                return false;
            }

            uuid = split[0];
            propName = split[1];
            return true;
        }
    }
}
