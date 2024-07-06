using Motio.Debuging;
using Motio.ObjectStoring;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace Motio.Configuration
{
    public delegate (bool passed, string errMsg) Validator<T>(T value);

    public class Configs
    {
        public event Action ConfigsChanged;
        public virtual string CONFIGS_PATH => Preferences.PrependCurrentDir("configs");

        public const string CATEGORY_UI = "UI";
        public const string CATEGORY_STARTUP = "Startup";
        public const string CATEGORY_PERF = "Performance";
        public const string CATEGORY_ADVANCED = "Advanced";

        public const string MinTimelineViewSize = "MinTimelineViewSize";
        public const string NbNodeInPropertyPanel = "NbNodeInPropertyPanel";
        public const string DefaultFile = "DefaultFile";
        public const string DefaultEditorFile = "DefaultEditorFile";
        public const string ConsoleOnStart = "ConsoleOnStart";
        public const string NoGCStartAlloc = "NoGCStartAlloc";
        public const string MoveGizmoSize = "MoveGizmoSize";
        public const string FrameBatchSize = "FrameBatchSize";
        public const string CacheDisplayTimer = "CacheDisplayTimer";
        public const string FullScreenOnStart = "FullScreenOnStart";
        public const string UpdateChannel = "UpdateChannel";
        public const string StartupScript = "StartupScript";
        public const string AddonsPath = "AddonsPath";
        public const string DebugMode = "DebugMode";


        private static Configs _instance;
        public static Configs Instance => _instance ?? (_instance = new Configs());

        public static void SaveSafe() => Instance._SaveSafe();
        public static void LoadSafe() => Instance._LoadSafe();
        public static ConfigEntry GetEntry(string key) => Instance._GetValue(key);
        public static object GetValue(string key) => Instance._GetValue(key).UntypedValue;
        public static T GetValue<T>(string key) => (T)Instance._GetValue(key).UntypedValue;

        private static Validator<int> GreaterThan(int min)
        {
            return (int value) => value < min ? (false, "The value must be greater than " + min) : (true, null);
        }

        private static Validator<int> LessThan(int max)
        {
            return (int value) => value > max ? (false, "The value must be less than " + max) : (true, null);
        }

        private static Validator<string> NotEmpty()
        {
            return (string value) => string.IsNullOrEmpty(value) ? (false, "The given string can't be empty") : (true, null);
        }

        private static Validator<string> FileExistsOrEmptyStr()
        {
            return (string value) =>
            {
                if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
                    return (true, null);
                if (!File.Exists(value))
                    return (false, "The file " + value + " doesn't exist");
                return (true, null);
            };
        }

        private static Validator<string> DirectoryExistsOrEmptyStr()
        {
            return (string value) =>
            {
                if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
                    return (true, null);
                if (!Directory.Exists(value))
                    return (false, "The directory " + value + " doesn't exist");
                return (true, null);
            };
        }

        private static Validator<long> GreaterThan(long min)
        {
            return (long value) => value < min ? (false, "The value must be greater than " + min) : (true, null);
        }

        private static Validator<double> GreaterThan(double min)
        {
            return (double value) => value < min ? (false, "The value must be greater than " + min) : (true, null);
        }

        private static Validator<UpdateChannels> HasAccess()
        {
            return (UpdateChannels value) =>
            {
                if(value == UpdateChannels.Test)
                {
                    string pass = Preferences.GetValue<string>(Preferences.TestChannelPass);
                    if(!pass.Equals("can I have access to the test channel please?"))
                    {
                        return (false, "You can't access the Test channel");
                    }
                }
                return (true, null);
            };
        }

        public IEnumerable<ConfigEntry> Entries
        {
            get
            {
                foreach(var pair in entries)
                {
                    yield return pair.Value;
                }
            }
        }
        private Dictionary<string, ConfigEntry> entries = new Dictionary<string, ConfigEntry>
        {
            {
                MinTimelineViewSize,
                new ConfigEntry<int>()
                {
                    Category = CATEGORY_UI,
                    LongName = "Minimum size of timeline window (in frames)",
                    ShortName = "Min Timeline Width",
                    Value = 20,
                    validations = new []{ GreaterThan(9), LessThan(41) }
                }
            },
            {
                NbNodeInPropertyPanel,
                new ConfigEntry<int>()
                {
                    Category = CATEGORY_UI,
                    LongName = "Maximum number of nodes in the property panel",
                    ShortName = "Max size property panel",
                    Value = 10,
                    validations = new []{ GreaterThan(0) }
                }
            },
            {
                DefaultFile,
                new ConfigEntry<string>()
                {
                    Category = CATEGORY_STARTUP,
                    LongName = "Default file to load on startup",
                    ShortName = "Default file",
                    Value = "",
                    validations = new []{ FileExistsOrEmptyStr() }
                }
            },
            {
                DefaultEditorFile,
                new ConfigEntry<string>
                {
                    Category = CATEGORY_STARTUP,
                    LongName = "Default file to load on the editor",
                    ShortName = "Default Editor File",
                    Value = "",
                    validations = new []{ FileExistsOrEmptyStr() }
                }
            },
            {
                ConsoleOnStart,
                new ConfigEntry<bool>
                {
                    Category = CATEGORY_STARTUP,
                    LongName = "Open Console On Start",
                    ShortName = "Console On Start",
                    Value = false
                }
            },
            {
                DebugMode,
                new ConfigEntry<bool>
                {
                    Category = CATEGORY_ADVANCED,
                    LongName = "Enable Debug Mode",
                    ShortName = "Debug Mode",
                    Value = false
                }
            },
            {
                NoGCStartAlloc,
                new ConfigEntry<long>
                {
                    Category = CATEGORY_PERF,
                    LongName = "Size of we tell the garbage collector to brace for at the beginning of a play",
                    ShortName = "GC Start Alloc",
                    Value = 200000000,
                    validations = new []{ GreaterThan(0L) }
                }
            },
            {
                MoveGizmoSize,
                new ConfigEntry<double>
                {
                    Category = CATEGORY_UI,
                    LongName = "Size of the move gizmo",
                    ShortName = "Move Gizmo size",
                    Value = 1,
                    validations = new []{ GreaterThan(0d) }
                }
            },
            {
                FrameBatchSize,
                new ConfigEntry<int>
                {
                    Category = CATEGORY_PERF,
                    LongName = "How many frames are calculated at once",
                    ShortName = "Frame Batch Size",
                    Value = 10,
                    validations = new []{ GreaterThan(0) }
                }
            },
            {
                CacheDisplayTimer,
                new ConfigEntry<double>
                {
                    Category = CATEGORY_UI,
                    LongName = "How often do we update cache display",
                    ShortName = "Cache Display Timer",
                    Value = 0.5,
                    validations = new []{ GreaterThan(0.1) }
                }
            },
            {
                FullScreenOnStart,
                new ConfigEntry<bool>
                {
                    Category = CATEGORY_STARTUP,
                    LongName = "Is the the software set to fullscreen on start ?",
                    ShortName = "Start as fullscreen",
                    Value = true
                }
            },
            {
                UpdateChannel,
                new ConfigEntry<UpdateChannels>
                {
                    Category = CATEGORY_ADVANCED,
                    LongName = "Channel to use when checking updates",
                    ShortName = "Update Channel",
                    Value = UpdateChannels.Main,
                    validations = new []{ HasAccess() }
                }
            },
            {
                StartupScript,
                new ConfigEntry<string>
                {
                    Category = CATEGORY_STARTUP,
                    LongName = "Path to script to run on startup",
                    ShortName = "Startup script",
                    Value = "",
                    validations = new []{ FileExistsOrEmptyStr() }
                }
            },
            {
                AddonsPath,
                new ConfigEntry<string>
                {
                    Category = CATEGORY_STARTUP,
                    LongName = "Additionnal folder to scan for nodes at startup",
                    ShortName = "Addons path",
                    Value = "",
                    validations = new []{ DirectoryExistsOrEmptyStr() }
                }
            }
        };

        protected Configs()
        {
            if (!File.Exists(CONFIGS_PATH))
                _Save();
        }

        /// <summary>
        /// execute save and catch all error and display them in a messagebox
        /// </summary>
        public void _SaveSafe()
        {
            try
            {
                _Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while saving configs:\n" + ex);
            }
        }

        /// <summary>
        /// save current configs to file but might throw errors
        /// </summary>
        public void _Save()
        {
            Dictionary<string, object> toJson = entries.ToDictionary(p => p.Key, p => p.Value.UntypedValue);

            string json;
            try
            {
                json = JsonConvert.SerializeObject(toJson);
            }
            catch (Exception ex)
            {
                throw new Exception("Error converting to json:\n" + ex);
            }

            try
            {
                File.WriteAllText(CONFIGS_PATH, json);
            }
            catch (Exception ex)
            {
                throw new Exception("Error writing config to file:\n" + ex);
            }

            try
            {
                ConfigsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                throw new Exception("Error calling ConfigsChanged event:\n" + ex);
            }
        }

        /// <summary>
        /// execute load and catch all error and display them in a messagebox
        /// </summary>
        public void _LoadSafe()
        {
            try
            {
                _Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading configs from file:\n" + ex);
            }
        }

        /// <summary>
        /// load configs from files but might throw errors
        /// </summary>
        public void _Load()
        {
            string raw;
            try
            {
                raw = File.ReadAllText(CONFIGS_PATH);
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading file:\n" + ex);
            }

            JObject jobj;
            try
            {
                jobj = JsonConvert.DeserializeObject<JObject>(raw);
            }
            catch (Exception ex)
            {
                throw new Exception("Error interpreting json:\n" + ex);
            }

            foreach(JProperty jprop in jobj.Properties())
            {
                if(!entries.ContainsKey(jprop.Name))
                {
                    Logger.WriteLine("Couldn't find a config entry with name " + jprop.Name);
                    continue;
                }
                ConfigEntry entry = entries[jprop.Name];
                (bool passed, string msg) = entry.SetValue(jprop.Value);
                if (!passed)
                    throw new Exception("Error validating value for " + jprop.Name + ":\n" + msg);
            }

            _Save();
        }

        public ConfigEntry _GetValue(string key)
        {
            if(!entries.TryGetValue(key, out ConfigEntry entry))
            {
                throw new Exception("The key " + key + " doesn't exist in the configs");
            }
            else
            {
                return entry;
            }
        }
    }
}
