using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using System.Collections.Generic;

namespace Motio.PythonRunning
{
    public class PythonCompileErrorListener : ErrorListener
    {
        public struct PythonCompileError
        {
            public ScriptSource source;
            public string message;
            public SourceSpan span;
            public int errorCode;
            public Severity severity;
        }
        public List<PythonCompileError> errors = new List<PythonCompileError>();
        public bool HasErrors => errors.Count != 0;

        public override void ErrorReported(ScriptSource source, string message, SourceSpan span, int errorCode, Severity severity)
        {
            errors.Add(new PythonCompileError()
            {
                source = source,
                message = message,
                span = span,
                errorCode = errorCode,
                severity = severity
            });
        }

        public override string ToString()
        {
            string str = "Errors compiling python code:\n";

            for (int i = 0; i < errors.Count; i++)
            {
                PythonCompileError error = errors[i];
                str += "error code " + error.errorCode + ": " + error.message + "\n";
                if (error.span.Start == error.span.End)
                    str += "at (line,char) " + error.span.Start;
                else
                    str += "from (line,char) " + error.span.Start + " to " + error.span.End + "\n";
            }

            return str;
        }
    }
}
