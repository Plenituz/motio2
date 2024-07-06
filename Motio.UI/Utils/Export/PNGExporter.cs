using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Motio.Configuration;
using Motio.UI.ViewModels;

namespace Motio.UI.Utils.Export
{
    public class PNGExporter : TimelineExporter
    {
        public static string ClassNameStatic = "PNG";

        public PNGExporter(MainControlViewModel mainViewModel) : base(mainViewModel)
        {
        }

        public override string Filter()
        {
            return "PNG File (*.png)|*.png";
        }

        public override IList<ConfigEntry> MakeOptions()
        {
            return new ConfigEntry[]
            {
                new ConfigEntry<bool>()
                {
                    LongName = "long name",
                    ShortName = "qsdsdfqsdsd",
                    Value = true
                },
                new ConfigEntry<string>()
                {
                    LongName = "long name",
                    ShortName = "qsdqsd",
                    Value = "sasa"
                }
            };
        }

        protected override void ExportCurrentFrame(string path)
        {
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(
                pixelWidth: mainViewModel.AnimationTimeline.ResolutionWidth,
                pixelHeight: mainViewModel.AnimationTimeline.ResolutionHeight,
                dpiX: 96,
                dpiY: 96,
                pixelFormat: PixelFormats.Pbgra32);
            renderTargetBitmap.Render(mainViewModel.RenderView.viewport);
            PngBitmapEncoder pngImage = new PngBitmapEncoder();
            pngImage.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

            try
            {
                using (Stream fileStream = File.Create(path))
                {
                    pngImage.Save(fileStream);
                }
            }
            catch (Exception ex)
            {
                TriggerError(ex);
            }
        }
    }
}
