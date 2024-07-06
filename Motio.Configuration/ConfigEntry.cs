using System;
using System.Collections.Generic;

namespace Motio.Configuration
{
    public abstract class ConfigEntry
    {
        public abstract string Category { get; set; }
        public abstract string ShortName { get; set; }
        public abstract string LongName { get; set; }
        public abstract object UntypedValue { get; }
        public abstract (bool passed, string errMsg) SetValue(object value);
    }

    public class ConfigEntry<T> : ConfigEntry
    {
        public override string Category { get; set; }
        public override string ShortName { get; set; }
        public override string LongName { get; set; }
        public T Value { get; set; }
        internal IList<Validator<T>> validations;

        public override object UntypedValue => Value;

        public override (bool passed, string errMsg) SetValue(object v)
        {
            T value;
            try
            {
                value = (T)Convert.ChangeType(v, typeof(T));
            }
            catch (Exception ex)
            {
                return (false, "Couldn't convert value of type " + v.GetType().Name + " to " + typeof(T).Name + ":\n" + ex);
            }

            if (validations != null)
            {
                foreach (Validator<T> validator in validations)
                {
                    (bool passed, string errMsg) = validator(value);
                    if (!passed)
                        return (false, errMsg);
                }
            }

            Value = value;
            return (true, null);
        }
    }
}
