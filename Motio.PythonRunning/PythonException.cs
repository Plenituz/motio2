using System;

namespace Motio.PythonRunning
{
    public class PythonException : Exception
    {
        public string File { get; private set; }

        public PythonException(string message, string file) : base(message)
        {
            this.File = file;
        }
    }
}
