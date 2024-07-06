using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Shapes;

namespace Motio.ValueConverters
{
    /// <summary>
    /// converter strings like [[100%-25]] into the actual size
    /// </summary>
    public class DynamicSizing : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(values[0] is double width))
            {
                Console.WriteLine("error dynamic sizing: first value is not double");
                return "error";
            }
            if (!(values[1] is double height))
            {
                Console.WriteLine("error dynamic sizing: second value is not double");
                return "error";
            }
            if (!(values[2] is string code))
            {
                Console.WriteLine("error dynamic sizing: third value is not string");
                return "error";
            }
            //if you though this started pretty clean, don't worry, here is the shit:

            List<string> results = new List<string>();
            Regex operationRegex = new Regex(@"\[\[[^(\[\])]*\]\]");
            Regex numberRegex = new Regex(@"[\d\.]+%[xy]");
            var matches = operationRegex.Matches(code);
            int count = 0;
            code = operationRegex.Replace(code, m => "{" + count++ + "}" );
            
            foreach(Match match in matches)
            {
                //get ride of the [[  ]]
                List<string> numberResults = new List<string>();
                count = 0;
                string str = match.ToString().Substring(2);
                str = str.Substring(0, str.Length - 2);

                var numbers = numberRegex.Matches(str);
                str = numberRegex.Replace(str, m => "{" + count++ + "}");
                foreach(Match numberMatch in numbers)
                {
                    string glyph = numberMatch.ToString();
                    char direction = glyph[glyph.Length - 1];
                    double percent = System.Convert.ToDouble(glyph.Substring(0, glyph.Length - 2));

                    switch (direction)
                    {
                        case 'x':
                            percent *= width;
                            break;
                        case 'y':
                            percent *= height;
                            break;
                        default:
                            throw new Exception("wtf dude, how did you get here ?");
                    }
                    numberResults.Add(percent.ToString());
                }
                str = string.Format(str, numberResults.Cast<object>().ToArray());
                results.Add(str);
            }

            code = string.Format(code, results.Select(Evaluate).Cast<object>().ToArray());

            Stream s = GenerateStreamFromString("<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Data='" + code + "'/>");
            var path = XamlReader.Load(s) as System.Windows.Shapes.Path;
            return path.Data;
        }

        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private static DataTable evaluator = new DataTable();


        private string Evaluate(string code)
        {
            return evaluator.Compute(code, "").ToString();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
