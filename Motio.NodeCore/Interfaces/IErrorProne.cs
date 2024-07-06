using System.Collections.Generic;

namespace Motio.NodeCore.Interfaces
{
    public interface IErrorProne
    {
        /// <summary>
        /// setting an error with an id already used replaces it
        /// </summary>
        /// <param name="id"></param>
        /// <param name="msg"></param>
        void SetError(int id, string msg);
        /// <summary>
        /// a warning is an error that's not fatal
        /// </summary>
        /// <param name="id"></param>
        /// <param name="msg"></param>
        void SetWarning(int id, string msg);
        bool HasError(int id);
        bool HasWarning(int id);
        void ClearError(int id);
        void ClearWarning(int id);

        IEnumerable<string> Errors { get; }
        IEnumerable<string> Warnings { get; }
        bool HasErrors { get; }
        bool HasWarnings { get; }
    }
}
