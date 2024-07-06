using Motio.NodeCore.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Motio.NodeCore.Utils
{
    public class ErrorProneImpl : IErrorProne
    {
        private Dictionary<int, string> _errors = new Dictionary<int, string>();
        private Dictionary<int, string> _warnings = new Dictionary<int, string>();

        public IEnumerable<string> Errors => _errors.Values.ToList();
        public IEnumerable<string> Warnings => _warnings.Values.ToList();
        public bool HasErrors => _errors.Keys.Count != 0;
        public bool HasWarnings=> _errors.Keys.Count != 0;
        

        public void SetError(int id, string msg)
        {
            _errors[id] = msg;
        }

        public void SetWarning(int id, string msg)
        {
            _warnings[id] = msg;
        }

        public void ClearError(int id)
        {
            _errors.Remove(id);
        }

        public void ClearWarning(int id)
        {
            _warnings.Remove(id);
        }

        public bool HasError(int id)
        {
            return _errors.ContainsKey(id);
        }

        public bool HasWarning(int id)
        {
            return _warnings.ContainsKey(id);
        }
    }
}
