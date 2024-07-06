using Motio.Debuging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Motio.Graphics
{
    public static class IconStorage
    {
        private static Dictionary<string, string> icons = new Dictionary<string, string>();

        public static string GetOrCreateCache(string path)
        {
            if (icons.TryGetValue(path, out string icon))
                return icon;

            try
            {
                icon = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Logger.WriteLine("Failed to read icon file " + path + ":\n" + e);
            }

            icons.Add(path, icon);
            return icon;
        }

    }
}
