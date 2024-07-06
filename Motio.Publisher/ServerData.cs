using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Linq;

namespace Motio.Publisher
{
    [AddINotifyPropertyChangedInterface]
    public class Channel
    {
        public string Name { get; set; }
        public int VersionDay { get; set; }
        public int VersionMonth { get; set; }
        public int VersionYear { get; set; }
        public int VersionNumber { get; set; }

        public string StringRepr => ToString();

        public Channel(string name, JObject channel)
        {
            this.Name = name;
            this.VersionDay = (int)channel["version_day"];
            this.VersionMonth = (int)channel["version_month"];
            this.VersionYear = (int)channel["version_year"];
            this.VersionNumber = (int)channel["version_n"];
        }

        public override string ToString()
        {
            return $"{VersionMonth}/{VersionDay}/{VersionYear}/{VersionNumber}";
        }
    }

    [AddINotifyPropertyChangedInterface]
    public class ChangeLog
    {
        public string Id { get; set; }
        public ObservableCollection<string> Entries = new ObservableCollection<string>();

        public ChangeLog(string id, JArray jarray)
        {
            this.Id = id;

            if (jarray == null)
                return;
            foreach(JValue val in jarray)
            {
                Entries.Add(val.ToString());
            }
        }
    }

    [AddINotifyPropertyChangedInterface]
    public class ServerData
    {
        public List<Channel> Channels { get; private set; } = new List<Channel>();
        private ObservableCollection<ChangeLog> changelog = new ObservableCollection<ChangeLog>();

        public ServerData(string rawData)
        {
            JObject jobj = JsonConvert.DeserializeObject<JObject>(rawData);
            Parse(jobj);
        }

        public ChangeLog GetOrMakeChangeLog(string id)
        {
            var q = from c in changelog
                    where c.Id.Equals(id)
                    select c;
            ChangeLog selected = q.FirstOrDefault();

            if(selected == null)
            {
                selected = new ChangeLog(id, null);
                changelog.Add(selected);
            }
            return selected;
        }

        public void RemoveEmptyChangeLog()
        {
            foreach(ChangeLog c in changelog.ToList())
            {
                if (c.Entries.Count == 0)
                    changelog.Remove(c);
            }
        }

        public string ToJson()
        {
            RemoveEmptyChangeLog();
            Dictionary<string, object> dict = new Dictionary<string, object>();
            Dictionary<string, object> channelsDict = new Dictionary<string, object>();
            Dictionary<string, object> changelogDict = new Dictionary<string, object>();
            dict.Add("change_log", changelogDict);
            dict.Add("channels", channelsDict);

            foreach(Channel channel in Channels)
            {
                Dictionary<string, int> channelDict = new Dictionary<string, int>
                {
                    { "version_day", channel.VersionDay },
                    { "version_month", channel.VersionMonth },
                    { "version_year", channel.VersionYear },
                    { "version_n", channel.VersionNumber }
                };
                channelsDict.Add(channel.Name, channelDict);
            }

            foreach(ChangeLog changelogEntry in changelog)
            {
                changelogDict.Add(changelogEntry.Id, changelogEntry.Entries.ToArray());
            }


            return JsonConvert.SerializeObject(dict, Formatting.Indented);
        }

        private void Parse(JObject jobj)
        {
            foreach(JProperty channelProp in ((JObject)jobj["channels"]).Properties())
            {
                Channels.Add(new Channel(channelProp.Name, (JObject)channelProp.Value));
            }
            JObject changelogJson = (JObject)jobj["change_log"];

            foreach(JProperty changelogProp in changelogJson.Properties())
            {
                changelog.Add(new ChangeLog(changelogProp.Name, (JArray)changelogProp.Value));
            }
        }

    }
}
