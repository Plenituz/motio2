using Newtonsoft.Json.Linq;
using Motio.ObjectStoring;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.Specialized;
using System;

namespace Motio.Animation
{
    public class KeyframeHolder : INotifyPropertyChanged, INotifyCollectionChanged
    {
        /// <summary>
        /// list of contained keyframes
        /// using a List :
        /// - the keyframes need to be sorted 
        /// - we need to access the elements by index (SortedSet can't do that)
        /// 
        /// NEVER put this public because of sorting and stuff
        /// 
        /// </summary>
        protected List<KeyframeFloat> keyframes { get; set; } = new List<KeyframeFloat>();
        public int Count => keyframes.Count;
        private DoubleInterpolator interpolator = new DoubleInterpolator();

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        [CustomSaver]
        object OnSave()
        {
            return new Dictionary<string, object>
            {
                { "keyframes", TimelineSaver.SaveCollectionToJson(keyframes) }
            };
        }

        [CustomLoader]
        void OnLoad(JObject jobj)
        {
            IList keys = (IList)TimelineLoader.LoadArrayFromJson(
                (JArray)jobj.Property("keyframes").Value, 
                typeof(List<object>), 
                this);
            foreach(KeyframeFloat k in keys)
            {
                AddKeyframeNoSort(k);
            }
            keyframes.Sort();
        }

        public KeyframeFloat KeyframeAt(int index)
        {
            return keyframes[index];
        }

        public int IndexOf(KeyframeFloat keyframe)
        {
            int index = keyframes.BinarySearch(keyframe);
            if (index < 0)
                return -1;
            else
                return index;
        }

        private void AddKeyframeNoSort(KeyframeFloat keyframe)
        {
            if (keyframe.Holder != null)
                throw new System.Exception("can't add a keyframe that already has a holder");
            keyframes.Add(keyframe);
            keyframe.Holder = this;

            //watching keyframe property chaneg to resort the list when it's time changes
            keyframe.PropertyChanged += Keyframe_PropertyChanged;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(keyframes)));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, keyframe));
        }

        /// <summary>
        /// keyframes are sorted, you can insert a keyframe with any time it will have the right place
        /// </summary>
        /// <param name="keyframe"></param>
        public void AddKeyframe(KeyframeFloat keyframe)
        {
            AddKeyframeNoSort(keyframe);
            keyframes.Sort();
        }

        public void RemoveKeyframe(KeyframeFloat keyframe)
        {
            if(Contains(keyframe))
            {
                if (keyframe.Holder == null)
                    throw new System.Exception("holder is null on the keyframe you tried to remove");
                keyframes.Remove(keyframe);
                keyframe.Holder = null;

                //don't need to sort on remove 
                //keyframes.Sort();
                //removing keyframe property change so the keyframe can go into another holder seemlessly
                keyframe.PropertyChanged -= Keyframe_PropertyChanged;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(keyframes)));
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, keyframe));
            }
        }

        /// <summary>
        /// this doesn't use the basic sortedset.contains fonction
        /// because the comparer kind of fucks up 
        /// </summary>
        /// <param name="keyframe"></param>
        /// <returns></returns>
        public bool Contains(KeyframeFloat keyframe)
        {
            bool contains = keyframes.BinarySearch(keyframe) >= 0;
            return contains;
        }

        /// <summary>
        /// this returns the keyframe at the given time if there is one
        /// otherwise returns null
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public KeyframeFloat GetKeyframeAtTime(int time)
        {
            int index = keyframes.BinarySearch(new KeyframeFloat(time, 0));
            if (index >= 0)
                return keyframes[index];
            else
                return null;
        }

        /// <summary>
        /// returns true if the keyframe actually exist (time is exactly the time of the keyframe
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public bool GetNextClosestKeyframe(int time, out KeyframeFloat keyframe, out int index)
        {
            bool exact = false;
            index = keyframes.BinarySearch(new KeyframeFloat(time, 0));
            if (index < 0)
            {
                index = ~index;
                if (index == keyframes.Count)
                    index--;
            }
            else
            {
                exact = true;
            }
            
            keyframe = keyframes[index];
            return exact;
        }

        private void Keyframe_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(KeyframeFloat.Time)))
            {
                //only resort the list when the time value changed
                keyframes.Sort();
            }
            PropertyChanged?.Invoke(sender, e);
        }

        public IEnumerator<KeyframeFloat> Enumerator()
        {
            return keyframes.GetEnumerator();
        }

        public float? GetValueAtTime(int time)
        {
            if (keyframes.Count == 0)
                return null;
            if (time < keyframes[0].Time)
                return keyframes[0].Value;
            if (time > keyframes[keyframes.Count - 1].Time)
                return keyframes[keyframes.Count - 1].Value;

            bool exactTime = GetNextClosestKeyframe(time, out KeyframeFloat next, out int indexNext);
            if (next == null)
                return null;
            //if the time given is the exact time of a keyframe, no need to interpolate
            if (exactTime)
                return next.Value;

            KeyframeFloat prev;
            if (indexNext == 0)
                prev = keyframes[indexNext];
            else
                prev = keyframes[indexNext - 1];

            return interpolator.InterpolateBetween(prev, next, time);
        }
    }
}
