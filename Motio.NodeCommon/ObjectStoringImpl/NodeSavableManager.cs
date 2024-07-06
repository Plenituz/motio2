using System;
using System.Collections.Generic;
using Motio.ObjectStoring;
using Motio.PythonRunning;
using System.Reflection;
using Motio.Debuging;
using System.Collections;
using Motio.NodeCommon.StandardInterfaces;

namespace Motio.NodeCommon.ObjectStoringImpl
{
    /// <summary>
    /// this has to manage all types of dynamicnode, not only python
    /// </summary>
    public class NodeSavableManager : ISavableManager
    {
        Type dynamicType = typeof(IDynamicNode);

        public Func<object> GetCustomSaver(object obj, bool asBase = false)
        {
            dynamic dyn = obj;
            if(asBase)
            {
                dynamic baseClass = Python.GetBaseClass(obj);
                if (baseClass != null && Python.Engine.Operations.ContainsMember(baseClass, "CustomSaver"))
                    return () => baseClass.CustomSaver();
                else
                    Logger.WriteLine("couldn't call base class's CustomSaver for " + obj);
            }
            if (Python.Engine.Operations.ContainsMember(dyn, "CustomSaver"))
                return () => dyn.CustomSaver();

            //fallback to search tag
            MethodInfo customSaver = TimelineSaver.SearchMethodWithAttr<CustomSaverAttribute>(obj.GetType(),
                   BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic, asBase ? 1 : 0);
            if (customSaver != null)
                return () => customSaver.Invoke(obj, null);
            
            //fallback to null
            return null;
        }

        public IEnumerable<SavableMember> GetSavableMembers(object obj)
        {
            //Add python AND csharp members to have inheritance
            var list = new List<SavableMember>();
            var listPython = ListSavableMembersPython(obj);
            var listCSharp = TimelineSaver.ListSavableMembersCSharp(obj);
            if (listPython != null)
                list.AddRange(listPython);
            if (listCSharp != null)
                list.AddRange(listCSharp);
            return list;
        }

        public string GetTypeString(object obj)
        {
            return "IronPython." + Python.GetClassName(obj);
        }

        public bool ShouldManageObject(object obj)
        {
            return dynamicType.IsAssignableFrom(obj.GetType()) || obj.GetType().ToString().Contains("IronPython");
        }

        private static IEnumerable<SavableMember> ListSavableMembersPython(object obj)
        {
            //if it's a python class there won't be any attributes
            //so check the "saveAttrs" class variable
            dynamic dyn = obj;
            if (Python.Engine.Operations.ContainsMember(dyn, "saveAttrs"))
            {
                var members = dyn.__dict__;
                IEnumerable attrsToSave = dyn.saveAttrs;
                List<SavableMember> savableMembers = new List<SavableMember>();
                foreach (string str in attrsToSave)
                {
                    savableMembers.Add(new SavableMember(str, members[str]));
                }
                return savableMembers;
            }
            return null;
        }
    }
}
