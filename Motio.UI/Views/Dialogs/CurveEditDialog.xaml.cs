using Motio.NodeImpl.NodePropertyTypes;
using Motio.UI.Renderers.PathRendering;
using Motio.UI.ViewModels;
using System;
using System.Windows;

namespace Motio.UI.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for CurveEditDialog.xaml
    /// </summary>
    public partial class CurveEditDialog : Window
    {
        CurveNodePropertyViewModel property;
        CanvasPathRenderer renderer;

        public CurveEditDialog(CurveNodePropertyViewModel property)
        {
            this.property = property;
            if (property.StaticValue == null)
                throw new Exception("you have to provide a Path in your CurveNodeProperty");
            property.Original.Deleted += Property_Deleted;
            Loaded += CurveEditDialog_Loaded;
            InitializeComponent();
        }

        private void CurveEditDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if(renderer == null)
            {
                renderer = new CanvasPathRenderer(property.Path, RenderCanvas)
                {
                    Bounds = new UICommon.SimpleRect()
                    {
                        Left = 0,
                        Right = 1,
                        Top = 1,
                        Bottom = 0
                    }
                };
                renderer.Show();
            }
        }

        private void Property_Deleted()
        {
            Close();
        }
    }
}
