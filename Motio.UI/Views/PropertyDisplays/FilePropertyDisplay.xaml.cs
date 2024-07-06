using Microsoft.Win32;
using Motio.NodeImpl.NodePropertyTypes;
using Motio.UI.ViewModels;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Motio.UI.Views.PropertyDisplays
{
    /// <summary>
    /// Interaction logic for FilePropertyDisplay.xaml
    /// </summary>
    public partial class FilePropertyDisplay : UserControl
    {
        FileNodePropertyViewModel property;

        public FilePropertyDisplay()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (property == null)
            {
                property = (FileNodePropertyViewModel)DataContext;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch (property.Action)
            {
                case FileNodeProperty.ActionType.Open:
                    {
                        OpenFileDialog openFile = new OpenFileDialog()
                        {
                            AddExtension = true,
                            CheckFileExists = true,
                            CheckPathExists = true,
                            Multiselect = false,
                            Title = property.Title,
                            Filter = property.Filter
                        };
                        if (openFile.ShowDialog() == true)
                        {
                            property.StaticValue = openFile.FileName;
                        }
                    }
                    break;
                case FileNodeProperty.ActionType.Save:
                    {
                        SaveFileDialog saveFile = new SaveFileDialog()
                        {
                            AddExtension = true,
                            CheckPathExists = true,
                            OverwritePrompt = true,
                            Filter = property.Filter
                        };
                        if(saveFile.ShowDialog() == true)
                        {
                            property.StaticValue = saveFile.FileName;
                        }
                    }
                    break;
            }


        }
    }
}
