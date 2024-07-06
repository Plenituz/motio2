using System;
using System.IO;

namespace Motio.Debuging
{
    public class EventRaisingStreamWriter : StreamWriter
    {
        public event Action<object, bool> StreamWritten;

        public EventRaisingStreamWriter(Stream stream) : base(stream)
        {
        }

        private void TriggerEvent(object o, bool newLine = true)
        {
            StreamWritten?.Invoke(o, newLine);
        }

        public override void Write(bool value)
        {
            base.Write(value);
            TriggerEvent(value, false);
        }

        public override void Write(int value)
        {
            base.Write(value);
            TriggerEvent(value, false);
        }

        public override void Write(uint value)
        {
            base.Write(value);
            TriggerEvent(value, false);
        }

        public override void Write(long value)
        {
            base.Write(value);
            TriggerEvent(value, false);
        }

        public override void Write(ulong value)
        {
            base.Write(value);
            TriggerEvent(value, false);
        }

        public override void Write(float value)
        {
            base.Write(value);
            TriggerEvent(value, false);
        }

        public override void Write(double value)
        {
            base.Write(value);
            TriggerEvent(value, false);
        }

        public override void Write(decimal value)
        {
            base.Write(value);
            TriggerEvent(value, false);
        }

        public override void Write(object value)
        {
            base.Write(value);
            TriggerEvent(value, false);
        }

        public override void Write(string format, object arg0)
        {
            base.Write(format, arg0);
            TriggerEvent(string.Format(format, arg0), false);
        }

        public override void Write(string format, object arg0, object arg1)
        {
            base.Write(format, arg0, arg1);
            TriggerEvent(string.Format(format, arg0, arg1), false);
        }

        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            base.Write(format, arg0, arg1, arg2);
            TriggerEvent(string.Format(format, arg0, arg1, arg2), false);
        }

        public override void Write(string format, params object[] arg)
        {
            base.Write(format, arg);
            TriggerEvent(string.Format(format, arg), false);
        }

        public override void Write(char value)
        {
            base.Write(value);
            TriggerEvent(value, false);
        }

        public override void Write(char[] buffer)
        {
            base.Write(buffer);
            TriggerEvent(buffer, false);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            base.Write(buffer, index, count);
            TriggerEvent(buffer, false);
        }

        public override void Write(string value)
        {
            base.Write(value);
            TriggerEvent(value, false);
        }

        public override void WriteLine()
        {
            base.WriteLine();
            TriggerEvent("");
        }

        public override void WriteLine(char value)
        {
            base.WriteLine(value);
            TriggerEvent(value);
        }

        public override void WriteLine(char[] buffer)
        {
            base.WriteLine(buffer);
            TriggerEvent(buffer);
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            base.WriteLine(buffer, index, count);
            TriggerEvent(buffer);
        }

        public override void WriteLine(bool value)
        {
            base.WriteLine(value);
            TriggerEvent(value);
        }

        public override void WriteLine(int value)
        {
            base.WriteLine(value);
            TriggerEvent(value);
        }

        public override void WriteLine(uint value)
        {
            base.WriteLine(value);
            TriggerEvent(value);
        }

        public override void WriteLine(long value)
        {
            base.WriteLine(value);
            TriggerEvent(value);
        }

        public override void WriteLine(ulong value)
        {
            base.WriteLine(value);
            TriggerEvent(value);
        }

        public override void WriteLine(float value)
        {
            base.WriteLine(value);
            TriggerEvent(value);
        }

        public override void WriteLine(double value)
        {
            base.WriteLine(value);
            TriggerEvent(value);
        }

        public override void WriteLine(decimal value)
        {
            base.WriteLine(value);
            TriggerEvent(value);
        }

        public override void WriteLine(string value)
        {
            base.WriteLine(value);
            TriggerEvent(value);
        }

        public override void WriteLine(object value)
        {
            base.WriteLine(value);
            TriggerEvent(value);
        }

        public override void WriteLine(string format, object arg0)
        {
            base.WriteLine(format, arg0);
            TriggerEvent(string.Format(format, arg0));
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            base.WriteLine(format, arg0, arg1);
            TriggerEvent(string.Format(format, arg0, arg1));
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            base.WriteLine(format, arg0, arg1, arg2);
            TriggerEvent(string.Format(format, arg0, arg1, arg2));
        }

        public override void WriteLine(string format, params object[] arg)
        {
            base.WriteLine(format, arg);
            TriggerEvent(string.Format(format, arg));
        }
    }
}
