using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Motio.UICommon
{
    public class ToolBox
    {
        public static string DisplayFloat(float value)
        {
            //return value.ToString();
            return string.Format(CultureInfo.InvariantCulture, "{0:#.##;-#.##;0}", value);
        }

        public static bool IsAltHeld()
        {
            return (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
        }

        public static bool IsShiftHeld()
        {
            return (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        }

        public static bool IsCtrlHeld()
        {
            return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        }

        public static void RunInUIThread(Action d)
        {
            Application.Current.Dispatcher.Invoke(d);
        }

        public static void RunInUIThread(Action<object> d, object data)
        {
            Application.Current.Dispatcher.Invoke(() => d(data));
        }

        public static void RunInThread(Action d)
        {
            ThreadPool.QueueUserWorkItem(_ => d());
        }

        public static void RunInThread(Action<object> d, object data)
        {
            ThreadPool.QueueUserWorkItem(_ => d(data));
        }

        public static Size MeasureTextBlock(TextBlock textBlock)
        {
            var formattedText = new FormattedText(
                textBlock.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
                textBlock.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                TextFormattingMode.Display);

            return new Size(formattedText.Width, formattedText.Height);
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj)
            where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static childItem FindVisualChild<childItem>(DependencyObject obj)
            where childItem : DependencyObject
        {
            foreach (childItem child in FindVisualChildren<childItem>(obj))
            {
                return child;
            }

            return null;
        }
    }
}
