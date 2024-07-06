using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Motio.Configuration
{
    public class Preferences
    {
        public const string PrivateKeyPath = "PublicKeyPath";
        public const string PathToVersionCs = "PathToVersionCs";
        public const string BuildOutputDir = "BuildOutputDir";


        public static string PrependCurrentDir(string path)
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
        }

        public virtual string PREFERENCES_PATH => PrependCurrentDir("prefs");

        private Dictionary<string, PreferenceEntry> prefs = new Dictionary<string, PreferenceEntry>()
        {
            { PrivateKeyPath, new PreferenceEntry(@"D:\plenicorp.com\id_rsa.pem") },
            { PathToVersionCs, new PreferenceEntry(@"D:\C#\Motio2\Motio.UICommon\VersionChecking\VersionChecker.cs") },
            { BuildOutputDir, new PreferenceEntry(@"D:\C#\Motio2\Motio2\bin\Release") },
        };
        public virtual Dictionary<string, PreferenceEntry> _Configs { get => prefs; set => prefs = value; }

        private static Preferences _instance;
        public static Preferences Instance => _instance ?? (_instance = new Preferences());

        public event Action PrefsChangeds;

        public static void Save() => Instance._Save();
        public static void Load() => Instance._Load();
        public static PreferenceEntry GetValue(string key) => Instance._GetValue(key);
        public static T GetValue<T>(string key) => (T)Instance._GetValue(key).value;
        public static T GetValueConvert<T>(string key) => (T)Convert.ChangeType(Instance._GetValue(key).value, typeof(T));
        public static void SetValue(string key, object value) => Instance._SetValue(key, value);
        public static void SetValue(PreferenceEntry entry, object value) => Instance._SetValue(entry, value);

        protected Preferences()
        {
            if (!File.Exists(PREFERENCES_PATH))
                _Save();
        }

        public void _Save()
        {
            StringBuilder builder = new StringBuilder();

            foreach (KeyValuePair<string, PreferenceEntry> pair in _Configs)
            {
                string valueStr = pair.Value.value.ToString();
                if (string.IsNullOrEmpty(valueStr))
                    valueStr = " ";
                builder.Append(pair.Key + ":" + valueStr + ";\n");
            }
            try
            {
                File.WriteAllText(PREFERENCES_PATH, builder.ToString());
            }
            catch (Exception e)
            {
                throw new Exception("error while saving config file:\n" + e.Message);
            }
            PrefsChangeds?.Invoke();
        }

        public void _Load()
        {
            string raw = File.ReadAllText(PREFERENCES_PATH);
            raw = raw.Replace("\n", "").Replace(Environment.NewLine, "");

            string[] entries = raw.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string entry in entries)
            {
                string[] pair = entry.Split(new char[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (pair.Length != 2)
                    throw new Exception("Configs: error reading configs, [" + entry + "] doesn't have 2 length after split on char ':'");
                _SetValue(pair[0], pair[1]);
            }
            _Save();
        }

        public PreferenceEntry _GetValue(string key)
        {
            _Configs.TryGetValue(key, out PreferenceEntry value);
            if (value == null)
            {
                throw new Exception("Configs: the key " + key + " doesn't exist in the configs");
            }
            return value;
        }

        public void _SetValue(string key, object value)
        {
            if (value == null)
                throw new Exception("Configs: can't put null in configs : " + key);
            if (_Configs.ContainsKey(key))
            {
                _SetValue(_Configs[key], value);
            }
            else
            {
                //TODO peut etre handle l'exception qui suit quand on est dans Load parceque si on update 
                //et qu'on a ajouté des trucs dans les configs ca va planter
                throw new Exception("Configs: tried to add an unknown key to configs");
            }
        }

        public void _SetValue(PreferenceEntry entry, object value)
        {
            if (value == null)
                throw new Exception("Configs: can't put null in configs : " + entry.longName);

            if (entry.value.GetType() != value.GetType())
            {
                //try to cast it, if you can't throw an error
                try
                {
                    //TODO if this is supposed to be a long the value is still cast as a int
                    value = Convert.ChangeType(value, entry.value.GetType());
                }
                catch
                {
                    throw new Exception("Configs: error while converting type for key " + entry.shortName);
                }
                if (value == null)
                    throw new Exception("Configs: error while converting type for key, got null :" + entry.shortName);
            }
            bool confirmation =
                entry.confirmation == null ?
                    true : entry.confirmation(value);
            if (confirmation)
                entry.value = value;
            else
                throw new Exception("Configs:" + entry.shortName
                    + ":" + value + "\ndidn't pass the confirmation test for key");

        }
    }
}
