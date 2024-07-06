using Motio.Debuging;
using System;
using System.Collections.Generic;

namespace Motio.NodeCommon.Utils
{
    /// <summary>
    /// class to hold several, not to be edited be several thread at the same time
    /// </summary>
    public class DataFeed : ICloneable
    {
        public const string DATA_POINTS = "points";
        //TODO faire un datafeed avec type determiné avant, genre DatafeedItem

        //list of data channels with name, type and data
        //each dataChannel has a name (the key) and a value and a type
        private Dictionary<string, object> dataChannels = new Dictionary<string, object>();
        private Dictionary<string, Type> channelTypes = new Dictionary<string, Type>();

        /// <summary>
        /// Sets the data for the given channel, adds a channel if necessary
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="data"></param>
        public void SetChannelData(string channelName, object data)
        {
            if (data == null)
            {
                Logger.WriteLine("tried to put null in channel " + channelName);
                return;
            }
            if (!IsCloneableType(data.GetType()))
                Logger.WriteLine("WARNING you put an instance of " + data.GetType() 
                    + " in the datafeed, this type is not cloneable or a value type."
                    + " Objects in the datafeed must be cloneable");

            channelTypes.TryGetValue(channelName, out Type enforced);
            if (enforced?.IsAssignableFrom(data.GetType()) == false)
                throw new Exception("the channel " + channelName + " only accepts data of type " 
                    + enforced + ", you tried to put in " + data.GetType());

            if (dataChannels.ContainsKey(channelName))
                dataChannels[channelName] = data;
            else
                dataChannels.Add(channelName, data);
        }

        /// <summary>
        /// returns the content of the channel named <paramref name="channel"/> or null if the 
        /// channel doesn't exist
        /// </summary>
        /// <param name="channel">channel name</param>
        /// <returns></returns>
        public object GetChannelData(string channel)
        {
            object data = null;
            dataChannels.TryGetValue(channel, out data);
            return data;
        }

        public T GetChannelData<T>(string channel) => (T)GetChannelData(channel);
        public bool ChannelExists(string channel) => dataChannels.ContainsKey(channel);

        public void EnforceChannelType(string channel, Type type)
        {
            if (channelTypes.ContainsKey(channel))
                throw new Exception("channel '" + channel + "' already has an enforced type of " + type);
            channelTypes.Add(channel, type);
        }

        public bool TryGetChannelData(string channel, out object data)
        {
            return dataChannels.TryGetValue(channel, out data);
        }

        public bool TryGetChannelData<T>(string channel, out T data)
        {
            bool got = dataChannels.TryGetValue(channel, out object d);
            data = (T)d;
            return got;
        }

        public IEnumerable<string> ListChannels()
        {
            return dataChannels.Keys;
        }

        public IEnumerable<T> GetDataOfType<T>()
        {
            Type Ttype = typeof(T);
            foreach(object value in dataChannels.Values)
            {
                if (value is T t)
                    yield return t;
            }
        }

        public static bool IsCloneableType(Type type)
        {
            return typeof(ICloneable).IsAssignableFrom(type) || type.IsValueType;
        }

        public DataFeed Clone()
        {
            DataFeed copy = new DataFeed();
            foreach (string channel in dataChannels.Keys)
            {
                object data = GetChannelData(channel);                 

                object clone;
                if(data is ICloneable cloneable)
                {
                    clone = cloneable.Clone();
                }
                else if(data is ValueType valueType)
                {
                    clone = (ValueType)data;
                }
                else
                {
                    throw new Exception("can't clone " + data + " with the datafeed");
                }
                copy.SetChannelData(channel, clone);
            }
            return copy;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
