using Newtonsoft.Json.Linq;
using System;

namespace Motio.UICommon.VersionChecking
{
    public class Version : IComparable<Version>
    {
        public DateTime VersionDate { get; private set; }
        public int VersionNumber { get; private set; }

        public Version(DateTime date, int number)
        {
            VersionDate = date;
            VersionNumber = number;
        }

        public override string ToString()
        {
            return $"{VersionDate.Month}/{VersionDate.Day}/{VersionDate.Year}/{VersionNumber}";
        }

        /// <summary>
        /// return the url of the file associated with this version
        /// </summary>
        /// <returns></returns>
        public string DownloadUrl()
        {
            return VersionChecker.BASE_URL + $"/build/motio_build_{VersionDate.Month}_{VersionDate.Day}_{VersionDate.Year}_{VersionNumber}.zip";
        }

        /// <summary>
        /// parse a version in the format "month/day/year/nb"
        /// </summary>
        /// <param name="versionStr"></param>
        /// <returns></returns>
        public static Version ParseVersionString(string versionStr)
        {
            string[] split = versionStr.Split('/');
            if (split.Length != 4)
                throw new ArgumentException("version number invalid");
            int month = int.Parse(split[0]);
            int day = int.Parse(split[1]);
            int year = int.Parse(split[2]);
            int nb = int.Parse(split[3]);

            return new Version(new DateTime(year, month, day), nb);
        }

        public static Version ParseVersionJson(string channel, JObject versionJson)
        {
            JObject channelJson = (JObject)versionJson["channels"][channel];

            DateTime serverDate = new DateTime(
                (int)channelJson["version_year"], 
                (int)channelJson["version_month"], 
                (int)channelJson["version_day"]);
            int serverVersionNb = (int)channelJson["version_n"];

            return new Version(serverDate, serverVersionNb);
        }

        public int CompareTo(Version other)
        {
            int comparison = DateTime.Compare(VersionDate, other.VersionDate);
            if (comparison == 0)
                return VersionNumber.CompareTo(other.VersionNumber);
            else
                return comparison;
        }
    }
}
