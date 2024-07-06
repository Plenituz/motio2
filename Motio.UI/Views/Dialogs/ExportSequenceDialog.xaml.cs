using Microsoft.Win32;
using Motio.Configuration;
using Motio.UI.Utils;
using Motio.UI.Utils.Export;
using Motio.UI.ViewModels;
using Motio.UI.Views.ConfigViews;
using Motio.UICommon;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Motio.UI.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ExportSequenceDialog.xaml
    /// </summary>
    public partial class ExportSequenceDialog : Window
    {
        private readonly MainControlViewModel mainViewModel;
        private readonly string exporterName;
        public TimelineExporter Exporter { get; private set; }
        bool canClose = true;

        public ExportSequenceDialog(MainControlViewModel mainViewModel, string exporterName)
        {
            this.mainViewModel = mainViewModel;
            this.exporterName = exporterName;
            CreateExporter();
            DataContext = this;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void ExportSequenceDialog_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = !canClose;
            if(!canClose)
            {
                Exporter.StopExport();
                UnlockWindow();
            }
        }

        private void LockWindow()
        {
            canClose = false;
            renderButton.Visibility = Visibility.Collapsed;
            progressBar.Visibility = Visibility.Visible;
        }

        private void UnlockWindow()
        {
            canClose = true;
            renderButton.Visibility = Visibility.Visible;
            progressBar.Visibility = Visibility.Collapsed;
        }

        private void Render_Click(object sender, RoutedEventArgs e)
        {
            if (!canClose)
                return;
            if(!int.TryParse(fromFrame.Text, out int from))
            {
                MessageBox.Show("the value for the starting frame is invalid");
                return;
            }
            if(!int.TryParse(toFrame.Text, out int to))
            {
                MessageBox.Show("the value for the ending frame is invalid");
                return;
            }
            if (string.IsNullOrWhiteSpace(path.Text) || string.IsNullOrEmpty(path.Text))
            {
                MessageBox.Show("the value for path is invalid");
                return;
            }

            CorrectPath();
            progressBar.Minimum = from;
            progressBar.Maximum = to;

            LockWindow();
            if (SetExporterOptions())
            {
                Exporter.path = path.Text;
                Exporter.StartExportRange(from, to);
            }
        }

        private bool SetExporterOptions()
        {
            Dictionary<string, ConfigEntry> options = new Dictionary<string, ConfigEntry>();
            for (int i = 0; i < OptionsList.Items.Count; i++)
            {
                ContentPresenter presenter = (ContentPresenter)OptionsList.ItemContainerGenerator.ContainerFromIndex(i);
                ConfigViewBase entryUi = ToolBox.FindVisualChild<ConfigViewBase>(presenter);
                ConfigEntry entry = (ConfigEntry)OptionsList.Items[i];

                bool passed;
                string error;
                try
                {
                    (passed, error) = entry.SetValue(entryUi.GetUserInputedValue());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error converting value for " + entry.ShortName +
                                    "\n" + ex.Message);
                    return false;
                }

                if (!passed)
                {
                    MessageBox.Show("Error setting config value for " + entry.ShortName + 
                                    "\n" + error);
                    return false;
                }
                options.Add(entry.ShortName, entry);
            }
            Exporter.Options = options;
            return true;
        }

        private void CreateExporter()
        {
            var creatable = TimelineExporter.exporters
                .Where(node => node.ClassNameStatic.Equals(exporterName))
                .FirstOrDefault();
            if(creatable == null)
            {
                MessageBox.Show("couldn't find and exporter with name " + exporterName);
                return;
            }

            Exporter = (TimelineExporter)creatable.CreateInstance(new object[] { mainViewModel });
            Exporter.Progress += Exporter_Progress;
            Exporter.Done += Exporter_Done;
            Exporter.Error += Exporter_Error;
        }

        private void Unsubscribe()
        {
            Exporter.Progress -= Exporter_Progress;
            Exporter.Done -= Exporter_Done;
            Exporter.Error -= Exporter_Error;
        }

        private void Exporter_Error(Exception ex)
        {
            Unsubscribe();
            UnlockWindow();
            MessageBox.Show("An error occured while exporting:\n" + ex);
        }

        private void Exporter_Done()
        {
            Unsubscribe();
            UnlockWindow();
            this.DialogResult = true;
        }

        private void Exporter_Progress(int i)
        {
            progressBar.Value = i;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFile = new SaveFileDialog()
            {
                CheckPathExists = true,
                Filter = Exporter.Filter(),
                AddExtension = true
            };
            if(saveFile.ShowDialog() == true)
            {
                path.Text = saveFile.FileName;
                CorrectPath();
            }
        }

        private void CorrectPath()
        {
            string strPath = path.Text;
            if (!strPath.Contains("#"))
            {
                string pathDir = Path.GetDirectoryName(strPath);
                string pathNoExt = Path.GetFileNameWithoutExtension(strPath);
                string extension = Path.GetExtension(strPath);
                strPath = pathDir + "\\" + pathNoExt + "#" + extension;
            }
            path.Text = strPath;
        }

    }
}
