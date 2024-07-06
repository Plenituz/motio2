using Motio.Debuging;
using Motio.ObjectStoring;
using Motio.PythonRunning;
using System;
using System.Collections;

namespace Motio.UI.Utils
{
    public class ProxyStatic
    {
        /// <summary>
        /// really a dictionnary of <Proxy<object>, object>
        /// </summary>
        public static BiDictionary<object, object> original2proxy = new BiDictionary<object, object>();

        public static object GetProxyOf(object original)
        {
            return original2proxy.GetByFirst(original);
        }

        public static T GetProxyOf<T>(object original)
        {
            return (T)GetProxyOf(original);
        }

        public static ProxyType CreateProxy<ProxyType>(object original)
        {
            if (original2proxy.ContainsFirstKey(original))
            {
                return GetProxyOf<ProxyType>(original);
            }


            //try python viewmodels, that way users can override native viewmodels
            string pythonTypeStr = Python.GetClassName(original) + "ViewModel";
            CreatablePythonNode creatable = Python.FindFirstCreatableWithName(pythonTypeStr);
            if (creatable != null)
            {
                if (typeof(ProxyType).IsAssignableFrom(creatable.PythonType))
                {
                    try
                    {
                        return (ProxyType)creatable.CreateIntance(original);
                    }
                    catch(Exception ex)
                    {
                        throw new Exception("found python type " + pythonTypeStr
                            + " as view model for " + original.GetType().Name
                            + " but couldn't create an instance:\n" + ex);
                    }
                }
                else
                {
                    Logger.WriteLine("found python type " + pythonTypeStr
                            + " as view model for " + original.GetType().Name
                            + " but it's type is not assignable to " + typeof(ProxyType).Name);
                }
            }
            //try native viewmodels
            Type proxyType = null;
            Type originalType = original.GetType();
            do
            {
                string typeString = "Motio.UI.ViewModels." + originalType.Name + "ViewModel";
                proxyType = Type.GetType(typeString);
                if (proxyType != null)
                    break;
                originalType = originalType.BaseType;
            } while (originalType != null);

            if (proxyType != null)
                return (ProxyType)Activator.CreateInstance(proxyType, original);

            Logger.WriteLine("couldn't find proxy type for " + original.GetType());

            proxyType = typeof(ProxyType); 
            return (ProxyType)Activator.CreateInstance(proxyType, original);
        }
    }

    public abstract class Proxy<OriginalType> : IProxy<OriginalType>
    {
        public abstract OriginalType Original { get; }

        public Proxy(OriginalType original)
        {
            ProxyStatic.original2proxy.Add(original, this);
        }

        public virtual void Delete()
        {
            if (ProxyStatic.original2proxy.ContainsSecondKey(this))
                ProxyStatic.original2proxy.RemoveBySecond(this);
        }
    }
}
