using Newtonsoft.Json.Linq;
using Motio.ObjectStoring;
using Motio.PythonRunning;
using System.IO;
using System.Reflection;

namespace Motio.NodeCommon.ObjectStoringImpl
{
    public class NodeCreatableProvider : ICreatableProvider
    {
        public void CallCustomLoader(object instance, JObject jobj)
        {
            dynamic customLoader = Python.Engine.Operations.GetMember(instance, "CustomLoader");
            customLoader(jobj);
        }

        public void CallOnAllLoaded(object instance, JObject jobj)
        {
            if (Python.Engine.Operations.ContainsMember(instance, "OnAllLoaded"))
            {
                dynamic onAllLoaded = Python.Engine.Operations.GetMember(instance, "OnAllLoaded");
                onAllLoaded(jobj);
            }
        }

        public void CallOnDoneLoading(object instance)
        {
            //for python objects, since IronPython doesn't support Attributes the method has to be named "OnDoneLoading"
            if (Python.Engine.Operations.ContainsMember(instance, "OnDoneLoading"))
            {
                dynamic onDoneLoading = Python.Engine.Operations.GetMember(instance, "OnDoneLoading");
                onDoneLoading();
                return;
            }

            //fallback to search tag
            MethodInfo customSaver = TimelineSaver.SearchMethodWithAttr<CustomSaverAttribute>(instance.GetType(),
                   BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (customSaver != null)
                customSaver.Invoke(instance, null);
        }

        public ICreatable CreateFromTypeString(string typeString)
        {
            string name = typeString.Split(new char[] { '.' }, 2)[1];
            CreatablePythonNode node = Python.FindFirstCreatableWithName(name);
            if(node == null)
            {
                throw new System.Exception("couldnt find node with name " + typeString);
            }
            return new PythonCreatableNodeWrapper(node);
            //return CreatableNode.CreateDynamic(FindFileWithName(typeString));
        }

        public bool HasCustomLoader(object instance)
        {
            return Python.Engine.Operations.ContainsMember(instance, "CustomLoader");
        }

        public bool IsCustomCreated(object instance)
        {
            return instance.GetType().ToString().Contains("IronPython");
        }

        public bool IsTypeStringValid(string typeString)
        {
            return typeString.Contains("IronPython");
        }


        /// <summary>
        /// search a fil in a directory
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="name"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        static bool SearchDirForFile(string dir, string name, out string file)
        {
            if (Directory.Exists(dir))
            {
                string[] files = Directory.GetFiles(dir);
                for (int i = 0; i < files.Length; i++)
                {
                    string n = Path.GetFileNameWithoutExtension(files[i]);
                    if (n.Equals(name))
                    {
                        file = files[i];
                        return true;
                    }
                }
            }
            file = "";
            return false;
        }
        /// <summary>
        /// find a python or c# file that has this name and returns it's full path
        /// or an empty string if not found
        /// The name should start with IronPython. which will be removed
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string FindFileWithName(string name)
        {
            //get everything after the first .
            name = name.Split(new char[] { '.' }, 2)[1];

            string[] dirsToSearch =
            {
                //Python.PythonGraphicsPath,
                //Python.PythonPropertyPath,
                //Python.CsharpGraphicsPath,
                //Python.CsharpPropertyPath
            };

            for (int i = 0; i < dirsToSearch.Length; i++)
            {
                if (SearchDirForFile(dirsToSearch[i], name, out string file))
                    return file;
            }

            throw new System.Exception("Couldn't find file associated with node named " + name);
        }
    }
}
