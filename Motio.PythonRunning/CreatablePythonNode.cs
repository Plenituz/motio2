using IronPython.Runtime.Types;
using Microsoft.Scripting.Hosting;
using System;

namespace Motio.PythonRunning
{
    public class CreatablePythonNode
    {
        public PythonType PythonType { get; private set; }
        public string File { get; private set; }
        public string NameInFile { get; private set; }

        public CreatablePythonNode(string file, PythonType pythonType, string nameInFile)
        {
            this.File = file;
            this.PythonType = pythonType;
            this.NameInFile = nameInFile;
        }

        public PythonException Recompile()
        {
            PythonException compileEx = Python.CompileAndRun(File, out ScriptScope scope);
            if (compileEx != null)
                return compileEx;

            try
            {
                PythonType = scope.GetVariable(NameInFile);
            }
            catch (Exception e)
            {
                return new PythonException(e.Message, File);
            }

            return null;
        }

        public dynamic CreateIntance(params object[] args)
        {
            return Python.Engine.Operations.CreateInstance(PythonType, args);
        }
    }
}
