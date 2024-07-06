using Motio.Configuration;
using Motio.Debuging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows;

namespace Motio.UICommon.VersionChecking
{
    public class VersionChecker
    {
        public static Version version = new Version(new DateTime(2018, 4, 18), 0);
        public const string BASE_URL = "https://plenicorp.com";
        public const string URL_CURRENT_JSON = BASE_URL + "/build/current.json";
        public const string URL_CRASH_REPORT = BASE_URL + "/exceptionReport";

        private static string HttpGet(string url)
        {
            string html = string.Empty;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
            }
            return html;
        }

        public static string[] ExtractChangeLog(JObject changeLogJson, Version from, Version to)
        {
            List<string> changeLog = new List<string>();
            foreach (JProperty jprop in changeLogJson.Properties())
            {
                Version changeLogVersion = Version.ParseVersionString(jprop.Name);
                int greaterThanFrom = changeLogVersion.CompareTo(from);
                int greaterThanTo = changeLogVersion.CompareTo(to);

                if(greaterThanFrom > 0 && greaterThanTo <= 0)
                {
                    JArray logs = (JArray)jprop.Value;
                    foreach (string log in logs)
                        changeLog.Add(log);
                }
            }
            return changeLog.ToArray();
        }


        public static bool IsUpToDate(string channel, out Version serverVersion, out string[] changeLog)
        {
            try
            {
                string versionFile = HttpGet(URL_CURRENT_JSON);
                if (string.IsNullOrEmpty(versionFile) || string.IsNullOrWhiteSpace(versionFile))
                    throw new Exception("response was empty");
                JObject json = (JObject)JsonConvert.DeserializeObject(versionFile);

                serverVersion = Version.ParseVersionJson(channel, json);
                int comparison = serverVersion.CompareTo(version);
                if (comparison > 0)
                {
                    //new update available
                    changeLog = ExtractChangeLog((JObject)json["change_log"], version, serverVersion);
                    return false;
                }
                else if (comparison < 0)
                {
                    Logger.WriteLine("that's weird, your version is more up to date than the one on the server.\nYou must be special.");
                    changeLog = new string[0];
                    return true;
                }
                else
                {
                    changeLog = new string[0];
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.WriteLine("couldn't check for update:" + e);
                changeLog = new string[0];
                serverVersion = new Version(new DateTime(), 0);
                return true;
            }
        }

        public static bool ShouldUpdate(string channel, out Version serverVersion, out string[] changeLog)
        {
            bool upToDate = IsUpToDate(channel, out serverVersion, out changeLog);
            
            if(!upToDate)
            {
                string ignoreStr = Preferences.GetValue<string>(Preferences.IgnoreVersion);
                try
                {
                    Version ignoredVersion = Version.ParseVersionString(ignoreStr);
                    //this version should be ignored
                    if (ignoredVersion.CompareTo(serverVersion) == 0)
                        return false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("error parsing ignored version string:\n" + ex.Message);
                }
                
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
