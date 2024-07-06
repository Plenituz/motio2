using Microsoft.Win32;
using System.Windows;

namespace Motio.UI.Views.ConfigViews
{
    /// <summary>
    /// Interaction logic for FileConfig.xaml
    /// </summary>
    public partial class FileConfig : ConfigViewBase
    {
        public FileConfig()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog()
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false,
                Filter = "All files (*.*)|*.*"
            };
            if(openFile.ShowDialog() == true)
            {
                FilePath.Text = openFile.FileName;
            }
        }

        public override object GetUserInputedValue()
        {
            return FilePath.Text;
        }
    }
}
