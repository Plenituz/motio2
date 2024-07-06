using System;
using System.Text;

namespace Motio.Debuging
{
    public class Logger
    {
        private static Logger _instance;
        public static Logger Instance => _instance ?? (_instance = new Logger());

        public StringBuilder builder = new StringBuilder();
        public event Action<string> NewLine; 

        private Logger(){}

        public static void WriteLine(string str) => Instance._WriteLine(str);
        public static void WriteLine(object obj) => Instance._WriteLine(obj);
        public static void Write(string str) => Instance._Write(str);
        public static void Write(object obj) => Instance._Write(obj);

        public void _Write(string str)
        {
            builder.Append(str);
            NewLine?.Invoke(str);
            Console.WriteLine(str);
        }

        public void _WriteLine(string str)
        {
            str += "\n";
            _Write(str);
        }

        public void _Write(object obj) => _Write(obj.ToString());
        public void _WriteLine(object obj) => _WriteLine(obj.ToString());
    }
}
