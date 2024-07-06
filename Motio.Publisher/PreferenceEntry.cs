using System;

namespace Motio.Configuration
{
    public class PreferenceEntry
    {
        public string shortName { get; private set; }
        public string longName { get; private set; }
        public object value { get; set; }
        public Predicate<object> confirmation;
        public string errorOnConfirmation;

        public PreferenceEntry(object value)
        {
            this.value = value;
        }

        public PreferenceEntry(string name, string shortName, object value)
        {
            longName = name;
            this.value = value;
            this.shortName = shortName;
        }

        public PreferenceEntry(string name, string shortName, object value, Predicate<object> confirmation, string error)
        {
            longName = name;
            this.value = value;
            this.confirmation = confirmation;
            this.errorOnConfirmation = error;
            this.shortName = shortName;
        }
    }
}
