using IronPython.Runtime.Types;
using Microsoft.Scripting.Hosting;
using Motio.Configuration;
using Motio.Debuging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Motio.PythonRunning
{
    public class Python
    {
        /*
         * notes on the python lib by iron python:
         * I added several files to the library:
         * 
         * for the AI/SVG/PDF import script:
         * lib/xml/parsers/expat.py from https://svn.code.sf.net/p/fepy/code/trunk/lib/pyexpat.py
         * lib/xml/dom/expatbuilder from the python standard lib 2.7
         * lib/xml/sax/expatreader.py from the python standard lib 2.7
         * 
         */

        public static Type DynamicType;
        public static Type ProxyType;

        private static string[] assemblies = new string[]
        {
            "Motio.NodeCore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Motio.NodeCommon, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Motio.NodeImpl, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "ObjectStoring, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Motio.Selecting, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Motio.UI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Motio.UICommon, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Motio.Meshing, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Motio.Graphics, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Motio.Geometry, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Debuging, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Motio.Configuration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Motio.Pathing, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Motio.ClickLogic, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Motio.Animation, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Motio.FontTesselation, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Motio.Boolean, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",

            "Poly2Tri, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Triangle, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",

            "PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
            "PresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
            "WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
        };

        private static Python _instance;
        public static Python Instance => _instance ?? (_instance = new Python());

        public ScriptEngine _pythonEngine;
        public static ScriptEngine Engine => Instance._pythonEngine;

        private EventRaisingStreamWriter pythonOutput;

        private ConcurrentDictionary<string, CreatablePythonNode> creatablePythonNodes = new ConcurrentDictionary<string, CreatablePythonNode>();
        public static IEnumerable<CreatablePythonNode> CreatableNodes => Instance.creatablePythonNodes.ToList().Select(p => p.Value);

        private ConcurrentDictionary<string, CreatablePythonNode> creatableProxys = new ConcurrentDictionary<string, CreatablePythonNode>();
        public static IEnumerable<CreatablePythonNode> CreatableProxys => Instance.creatableProxys.ToList().Select(p => p.Value);

        private ConcurrentDictionary<string, CreatablePythonNode> pythonPool = new ConcurrentDictionary<string, CreatablePythonNode>();
        public static ConcurrentDictionary<string, CreatablePythonNode> PythonPool => Instance.pythonPool;

        /// <summary>
        /// get the class of the object using ironpython, turns out this also work or plain old regular c# objects
        /// </summary>
        /// <param name="pythonObject"></param>
        /// <returns></returns>
        public static string GetClassName(object pythonObject)
        {
            var classMember = Engine.Operations.GetMember(pythonObject, "__class__");
            string name = Engine.Operations.GetMember(classMember, "__name__");
            return name;
        }

        public static dynamic GetBaseClass(object instance)
        {
            var classMember = Engine.Operations.GetMember(instance, "__class__");
            if (classMember == null)
                return null;
            var basesMember = Engine.Operations.GetMember(classMember, "__bases__");
            if (basesMember == null)
                return null;
            return basesMember[0];
        }

        private void LoadAssemblies()
        {
            _pythonEngine.Runtime.LoadAssembly(Assembly.GetExecutingAssembly());
            for (int i = 0; i < assemblies.Length; i++)
            {
                _pythonEngine.Runtime.LoadAssembly(Assembly.Load(assemblies[i]));
            }
        }

        /// <summary>
        /// redirect the IronPython output to the Motio console (and regular console)
        /// </summary>
        private void SetupOutput()
        {
            MemoryStream ms = new MemoryStream();

            pythonOutput = new EventRaisingStreamWriter(ms);
            pythonOutput.StreamWritten += PythonOutput_StreamWritten;

            _pythonEngine.Runtime.IO.SetOutput(ms, pythonOutput);
        }

        private void SetupPath()
        {
            //allow to import the base libs
            var paths = _pythonEngine.GetSearchPaths();
            //link to lib folder provided by IronPython and that get copied when building
            paths.Add(PrependCurrentDir("Lib"));//TODO make this a config entry
            paths.Add(Configs.GetValue<string>(Configs.AddonsPath));
            paths.Add(PrependCurrentDir("BuiltIn"));
            paths.Add(PrependCurrentDir("MotioLib"));
            paths.Add(PrependCurrentDir("Addons"));
            _pythonEngine.SetSearchPaths(paths);
        }

        private Python()
        {
            _pythonEngine = IronPython.Hosting.Python.CreateEngine(/*new Dictionary<string, object> { { "Debug", ScriptingRuntimeHelpers.True } }*/);
            //Debug.Assert(_pythonEngine.Runtime.Setup.DebugMode);

            LoadAssemblies();
            SetupOutput();
            SetupPath();
        }

        public static CreatablePythonNode FindFirstCreatableFromFile(string file) => Instance._FindFirstCreatableFromFile(file);
        public CreatablePythonNode _FindFirstCreatableFromFile(string file)
        {
            var node = creatablePythonNodes.Select(p => p.Value).ToList().Find(c => c.File.Equals(file));
            if (node != null)
                return node;
            return creatableProxys.ToList().Find(c => c.Value.File.Equals(file)).Value;
        }

        public static CreatablePythonNode FindFirstCreatableWithName(string name) => Instance._FindFirstCreatableWithName(name);
        public CreatablePythonNode _FindFirstCreatableWithName(string name)
        {
            var node = creatablePythonNodes.Select(p => p.Value).ToList().Find(c => c.NameInFile.Equals(name));
            if (node != null)
                return node;
            return creatableProxys.Select(p => p.Value).ToList().Find(c => c.NameInFile.Equals(name));
        }

        public static string PrependCurrentDir(string localPath)
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), localPath);
        }

        /// <summary>
        /// compile addons an register the nodes 
        /// </summary>
        public IEnumerable<PythonException> CompileAddons() 
        {
            //we have to give full path so we can retreive nodes from file name later on
            string pathAddons = PrependCurrentDir("Addons");
            string pathBuiltIn = PrependCurrentDir("BuiltIn");

            HashSet<string> allIgnored = new HashSet<string>();
            List<PythonException> errors = new List<PythonException>();

            (HashSet<string> ignoredBuiltIn, JArray knownBuiltIn) = ReadWeavingJson(pathBuiltIn);
            if (knownBuiltIn != null)
            {
                CompileKnownFiles(knownBuiltIn, out var compiledBuiltIn, out var weavBuiltInError);
                allIgnored.UnionWith(compiledBuiltIn);
                errors.AddRange(weavBuiltInError);
            }
            (HashSet<string> ignoredAddons, JArray knownAddons) = ReadWeavingJson(pathAddons);
            if (knownAddons != null)
            {
                CompileKnownFiles(knownAddons, out var compiledAddons, out var weavAddonsError);
                allIgnored.UnionWith(compiledAddons);
                errors.AddRange(weavAddonsError);
            }

            if (ignoredBuiltIn != null)
                allIgnored.UnionWith(ignoredBuiltIn);
            if (ignoredAddons != null)
                allIgnored.UnionWith(ignoredAddons);

            IEnumerable<PythonException> errAddonsAdd = CompileFolder(Configs.GetValue<string>(Configs.AddonsPath), allIgnored);
            IEnumerable<PythonException> errAddons = CompileFolder(pathAddons, allIgnored);
            IEnumerable<PythonException> errBuiltIn = CompileFolder(pathBuiltIn, allIgnored);

            errors.AddRange(errAddonsAdd);
            errors.AddRange(errAddons);
            errors.AddRange(errBuiltIn);
            return errors;
        }

        private (HashSet<string> ignoredFiles, JArray knownFiles) ReadWeavingJson(string folder)
        {
            string path = Path.Combine(folder, "weaving.json");
            if (!File.Exists(path))
                return (null, null);
            string content;
            try
            {
                content = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                Logger.WriteLine("couldn't read the content of " + path + "\n" + ex);
                return (null, null);
            }

            JObject jobj;
            try
            {
                jobj = JsonConvert.DeserializeObject<JObject>(content);
            }
            catch(Exception ex)
            {
                Logger.WriteLine("couldn't interpret json " + content + "\n" + ex);
                return (null, null);
            }

            JArray ignoredFilesJson = null;
            JArray knownFiles = null;
            if(jobj.TryGetValue("ignored", out JToken ignoredToken))
            {
                if (ignoredToken is JArray jarr)
                    ignoredFilesJson = jarr;
            }
            if(jobj.TryGetValue("known", out JToken knownToken))
            {
                if (knownToken is JArray jarr)
                    knownFiles = jarr;
            }

            HashSet<string> ignoredFiles = new HashSet<string>(
                ignoredFilesJson.Select(o => PrependCurrentDir(o.ToString())));

            return (ignoredFiles, knownFiles);
        }

        /// <summary>
        /// compile the files from the "known" section of the weaving.json and return a 
        /// hashset of the fullpaths of the compiled files
        /// </summary>
        /// <param name="knownFiles"></param>
        /// <returns></returns>
        private void CompileKnownFiles(JArray knownFiles, 
            out HashSet<string> compiledFiles, out IEnumerable<PythonException> exceptions)
        {
            compiledFiles = new HashSet<string>();
            List<PythonException> exs = new List<PythonException>();
            exceptions = exs;

            foreach (JObject entry in knownFiles)
            {
                PythonException ex = CompileJsonEntry(entry, out string path);
                compiledFiles.Add(path);
                if (ex != null)
                    exs.Add(ex);
            }
        }

        private PythonException CompileJsonEntry(JObject entry, out string path)
        {
            if (entry["path"] == null || entry["class_name"] == null)
            {
                path = "";
                return null;
            }

            path = PrependCurrentDir(entry["path"].ToString());
            string nameInFile = entry["class_name"].ToString();

            if (!File.Exists(path))
                return new PythonException(path + " doesn't exist ", path);

            PythonException ex = CompileAndRun(path, out ScriptScope scope);
            if (ex != null)
                return ex;

            if (!scope.ContainsVariable(nameInFile))
                return new PythonException("no class named " + nameInFile + " in " + path, path);

            PythonType type = scope.GetVariable(nameInFile) as PythonType;
            if (type == null)
                return new PythonException(nameInFile + " was not a PythonType in " + path, path);
            CreatablePythonNode creatableNode = new CreatablePythonNode(path, type, nameInFile);
            if(nameInFile.EndsWith("ViewModel"))
                creatableProxys.TryAdd(nameInFile, creatableNode);
            else
                creatablePythonNodes.TryAdd(nameInFile, creatableNode);
            return null;
        }

        private PythonException CompileFile(string file)
        {
            PythonException ex = CompileAndRun(file, out ScriptScope scope);
            if (ex != null)
                return ex;

            foreach(dynamic potentiallyAType in scope.GetItems())
            {
                //potentially super slow, exception throwing is slow
                //PERF
                try
                {
                    if(potentiallyAType is KeyValuePair<string, dynamic> pair && pair.Value is PythonType type)
                    {
                        string asm = PythonType.Get__clr_assembly__(type);
                        string classNameStatic = GetClassNameStatic(type);
                        if (DynamicType.IsAssignableFrom(type) && asm.Contains("IronPython.NewTypes") && classNameStatic != null)
                        {
                            //find node
                            CreatablePythonNode creatableNode = new CreatablePythonNode(file, type, pair.Key);
                            creatablePythonNodes.TryAdd(pair.Key, creatableNode);
                        }else if (PythonType.Get__name__(type).Contains("ViewModel") && pair.Key.Contains("ViewModel"))
                        {
                            //find proxys
                            CreatablePythonNode creatableNode = new CreatablePythonNode(file, type, pair.Key);
                            creatableProxys.TryAdd(pair.Key, creatableNode);
                        }
                        else
                        {
                            //otherwise dump it in the pool
                            CreatablePythonNode creatableNode = new CreatablePythonNode(file, type, pair.Key);
                            pythonPool.TryAdd(pair.Key, creatableNode);
                        }
                    }
                }
                catch
                { }//exception means it's not a type, just try the next one
            }
            return null;
        }

        public static PythonException CompileAndRun(string file, out ScriptScope scope, Dictionary<string, object> vars = null)
        {
            ScriptSource source = Engine.CreateScriptSourceFromFile(file);
            PythonCompileErrorListener listener = new PythonCompileErrorListener();
            scope = Engine.CreateScope();
            if(vars != null)
            {
                foreach(var pair in vars)
                {
                    scope.SetVariable(pair.Key, pair.Value);
                }
            }

            CompiledCode compiled;
            try
            {
                compiled = source.Compile(listener);
            }
            catch (Exception e)
            {
                return new PythonException(e.ToString(), file);
            }

            if (listener.HasErrors)
                return new PythonException(listener.ToString(), file);

            try
            {
                compiled.Execute(scope);
            }
            catch (Exception ex)
            {
                return new PythonException(FormatException(ex), file);
            }
            return null;
        }

        public static string FormatException(Exception e)
        {
            return Engine
                .GetService<ExceptionOperations>()
                .FormatException(e);
        }

        public static string GetClassNameStatic(PythonType type)
        {
            try
            {
                return (string)type.__getattribute__(null, "classNameStatic");
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// compile of the scripts in the given folder, and find the nodes
        /// on compile tout, meme les modules comme ca on choppe les compile error et 
        /// ca aide le user.        
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ignoredFiles">files in this set won't be compiled</param>
        /// <returns>List of python compile errors, all the returned Listeners have errors</returns>
        private IEnumerable<PythonException> CompileFolder(string path, HashSet<string> ignoredFiles)
        {
            if (!Directory.Exists(path))
                return new PythonException[0];
            string[] files = Directory.GetFiles(path, "*.py", SearchOption.AllDirectories);
            //List<PythonException> errors = new List<PythonException>();
            ConcurrentBag<PythonException> errors = new ConcurrentBag<PythonException>();

            //Parallel.ForEach(files, (file) =>
            //{
            //    if (ignoredFiles.Contains(file))
            //        return;

            //    PythonException ex = CompileFile(file);
            //    if (ex != null)
            //        errors.Add(ex);
            //});
            foreach(string file in files)
            {
                if (ignoredFiles.Contains(file))
                    continue;

                PythonException ex = CompileFile(file);
                if (ex != null)
                    errors.Add(ex);
            }

            return errors;
        }

        private void PythonOutput_StreamWritten(object data, bool newLine)
        {
            if (newLine)
            {
                Logger.WriteLine(data);
                Console.WriteLine(data);
            }
            else
            {
                Logger.Write(data);
                Console.Write(data);
            }
        }
    }
}
