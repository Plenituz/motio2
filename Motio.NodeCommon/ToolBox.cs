using Motio.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Motio.NodeCommon
{
    public class ToolBox
    {
        /// <summary>
        /// this is a method to be used to convert any type to double, 
        /// it uses the invariant culture to make sure . and , are accepted
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static float ConvertToFloat(object obj)
        {
            return Convert.ToSingle(obj, CultureInfo.InvariantCulture);
        }

        public static double ConvertToDouble(object obj)
        {
            return Convert.ToDouble(obj, CultureInfo.InvariantCulture);
        }

        public static int ConvertToInt(object obj)
        {
            return Convert.ToInt32(obj, CultureInfo.InvariantCulture);
        }

        public static long ConvertToLong(object obj)
        {
            return Convert.ToInt64(obj, CultureInfo.InvariantCulture);
        }

        public static string ToStringInvariantCulture(object obj)
        {
            return Convert.ToString(obj, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// prepend the directory of the executing assembly to the path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string PrependCurrentDir(string path)
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
        }

        /// <summary>
        /// create a transformation matrix with the given arguments
        /// </summary>
        /// <param name="translation"></param>
        /// <param name="rotation">in degrees</param>
        /// <param name="scale"></param>
        /// <param name="rotateFrom"></param>
        /// <param name="scaleFrom"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static Matrix CreateMatrix(Vector2 translation, Vector3 rotation, Vector2 scale, 
            Vector2 rotateFrom, Vector2 scaleFrom, IList<string> order)
        {
            Matrix matrix = Matrix.Identity;
            Matrix matrixTrans = Matrix.CreateTranslation(translation.X, translation.Y, 0f);
            Matrix matrixRot = Matrix.CreateFromYawPitchRoll(
                MathHelper.ToRadians(rotation.X),
                MathHelper.ToRadians(rotation.Y),
                MathHelper.ToRadians(rotation.Z));
            Matrix matrixScale = Matrix.CreateScale(scale.X, scale.Y, 1);
            Matrix tmpMatrix;

            for (int i = 0; i < order.Count; i++)
            {
                switch (order[i][0])
                {
                    case 'T':
                        matrix.Append(matrixTrans);
                        break;
                    case 'R':
                        {
                            //translate by -rotateAround
                            Vector3 tmpTrans = new Vector3(-rotateFrom, 0);
                            Matrix.CreateTranslation(ref tmpTrans, out tmpMatrix);
                            matrix.Append(tmpMatrix);
                            //apply rotation
                            matrix.Append(matrixRot);
                            //translate by rotateAround
                            tmpMatrix.Translation = -tmpTrans;
                            matrix.Append(tmpMatrix);
                        }
                        break;
                    case 'S':
                        {
                            //translate by -scaleFrom
                            Vector3 tmpTrans = new Vector3(-scaleFrom, 0);
                            Matrix.CreateTranslation(ref tmpTrans, out tmpMatrix);
                            matrix.Append(tmpMatrix);
                            //apply scale
                            matrix.Append(matrixScale);
                            //translate by scaleFrom
                            tmpMatrix.Translation = -tmpTrans;
                            matrix.Append(tmpMatrix);
                        }
                        break;
                }
            }
            return matrix;
        }
    }
}
