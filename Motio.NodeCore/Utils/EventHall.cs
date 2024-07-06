using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Motio.NodeCore.Utils
{
    public class EventHall
    {
        public delegate void EventHandler(object triggerer, string propertyName, object data = null);

        /// <summary>
        /// PERF making the key of the inner dict the propertyname may be faster be force you to
        /// subscribe to all the propertynames
        /// </summary>
        private static ConcurrentDictionary<string, ConcurrentDictionary<EventHandler, EventHandler>> eventLibrary =
            new ConcurrentDictionary<string, ConcurrentDictionary<EventHandler, EventHandler>>();

        public static void Subscribe(string eventName, EventHandler handler)
        {
            ConcurrentDictionary<EventHandler, EventHandler> handlerList;
            if (!eventLibrary.TryGetValue(eventName, out handlerList))
            {
                handlerList = new ConcurrentDictionary<EventHandler, EventHandler>();
                eventLibrary[eventName] = handlerList;
            }
            handlerList[handler] = handler;
        }

        public static void Unsubscribe(string eventName, EventHandler handler)
        {
            if(eventLibrary.TryGetValue(eventName, out var handlerList))
            {
                handlerList.TryRemove(handler, out _);
            }
        }

        public static void Trigger(object triggerer, string eventName, string propertyName, object data = null)
        {
            ConcurrentDictionary<EventHandler, EventHandler> handlerList;
            if(triggerer is GraphicsNode gnode && !eventName.Equals("AnimationTimeline.GraphicsNodes"))
            {
                if(eventLibrary.TryGetValue("AnimationTimeline.GraphicsNodes", out handlerList))
                {
                    foreach (var handler in handlerList)
                    {
                        handler.Key(triggerer, propertyName, data);
                    }
                }
            }

            if(eventLibrary.TryGetValue(eventName, out handlerList))
            {
                foreach (var handler in handlerList)
                {
                    handler.Key(triggerer, propertyName, data);
                }
            }
        }

        public static void Reset()
        {
            //fresh instance to reset size of backend array
            eventLibrary = new ConcurrentDictionary<string, ConcurrentDictionary<EventHandler, EventHandler>>();
        }
    }
}
