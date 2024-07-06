using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Motio.Graphics
{
    public static class ImageStorage
    {
        public struct ImageInfo
        {
            /// <summary>
            /// absolute path to the file
            /// </summary>
            public string path;
            /// <summary>
            /// the height in pixel decoded from file
            /// </summary>
            public int pixelHeight;
            /// <summary>
            /// the width in pixel decoded from file
            /// </summary>
            public int pixelWidth;

            /// <summary>
            /// horizontal dot per inch
            /// </summary>
            public double dpiX;
            /// <summary>
            /// vertical dot per inch
            /// </summary>
            public double dpiY;
            /// <summary>
            /// height in device independant unit: 1/96th inch per unit
            /// </summary>
            public double deviceIndependantHeight;
            /// <summary>
            /// width in device independant unit: 1/96th inch per unit
            /// </summary>
            public double deviceIndependantWidth;
        }

        private static Dictionary<string, BitmapImage> images = new Dictionary<string, BitmapImage>();

        /// <summary>
        /// Get information about an image at the given path. 
        /// This will cache the image if not already cached
        /// </summary>
        /// <param name="path"></param>
        public static ImageInfo GetImageInfo(string path)
        {
            BitmapImage img = GetOrCreateCache(path);
            return new ImageInfo()
            {
                path = img.UriSource.AbsolutePath,
                pixelHeight = img.PixelHeight,
                pixelWidth = img.PixelWidth,
                dpiX = img.DpiX,
                dpiY = img.DpiY,
                deviceIndependantHeight = img.Height,
                deviceIndependantWidth = img.Width
            };
        }

        public static void Clear()
        {
            images.Clear();
        }

        public static void PutInCache(string path)
        {
            GetOrCreateCache(path);
        }

        public static BitmapImage GetOrCreateCache(string path)
        {
            if (images.TryGetValue(path, out BitmapImage img))
                return img;

            img = new BitmapImage(new Uri(path))
            {
                CacheOption = BitmapCacheOption.OnLoad
            };
            
            img.Freeze();
            images.Add(path, img);
            return img;
        }
    }
}
