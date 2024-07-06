using Microsoft.Win32;
using Motio.NodeCommon.ObjectStoringImpl;
using Motio.PythonRunning;
using Motio.UI.ViewModels;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Motio.UI.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for FileEditingDialog.xaml
    /// </summary>
    public partial class FileEditingDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private AnimationTimelineViewModel animationTimeline;
        private FileSystemWatcher watcher;

        private string _filePath = "";
        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                LoadFileToEditor(_filePath);
            }
        }
        public Encoding Encoding { get; set; } = Encoding.Unicode;
        bool FileLoaded => !string.IsNullOrWhiteSpace(FilePath);

        public FileEditingDialog(AnimationTimelineViewModel animationTimeline)
        {
            this.animationTimeline = animationTimeline;
            DataContext = this;
            InitializeComponent();
        }

        public void SelectLine(int lineNb, int index)
        {
            lineNb++;
            int start = 0, lineCount = 0;
            StringReader reader = new StringReader(editor.Text);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if(lineCount == lineNb)
                {
                    break;
                }
                start += line.Length;
                lineCount++;
            }
            editor.Select(start, index);
        }

        private void OpenFile()
        {
            OpenFileDialog openFile = new OpenFileDialog()
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false,
                Filter = "Python or C# File|*.py;*.cs|All files (*.*)|*.*"
            };
            if (openFile.ShowDialog() == true)
            {
                FilePath = openFile.FileName;
                DisposeWatcher();
                CreateWatcher();
            }
        }

        private void CreateWatcher()
        {
            watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(FilePath),
                Filter = Path.GetFileName(FilePath),
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName
            };
            watcher.Changed += Watcher_Changed;
            watcher.Deleted += Watcher_Deleted;
            watcher.Renamed += Watcher_Renamed;
            watcher.Error += Watcher_Error;
            watcher.EnableRaisingEvents = true;
        }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            MessageBox.Show("an error occured with the editor:\n" + e.GetException().Message);
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                FilePath = "";
                editor.Text = "";
            });
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                FilePath = "";
                editor.Text = "";
            });
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LoadFileToEditor(FilePath);
            });
        }

        private void DisposeWatcher()
        {
            if (watcher == null)
                return;
            watcher.EnableRaisingEvents = false;
            watcher.Changed -= Watcher_Changed;
            watcher.Deleted -= Watcher_Deleted;
            watcher.Renamed -= Watcher_Renamed;
            watcher.Error -= Watcher_Error;
            watcher.Dispose();
            watcher = null;
        }

        private void SaveFile()
        {
            if (!FileLoaded)
                return;
            File.WriteAllText(FilePath, editor.Text);
        }

        private void LoadFileToEditor(string path)
        {
            try
            {
                string fileContent = File.ReadAllText(path);
                editor.Text = fileContent;
            }
            catch (Exception e)
            {
                MessageBox.Show("error reading file " + path + ":\n" + e.Message);
            }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (!FileLoaded)
            {
                MessageBox.Show("you need to open a file before that!");
                return;
            }
            CreatablePythonNode creatableNode = null;
            try
            {
                creatableNode = Python.FindFirstCreatableFromFile(FilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (creatableNode == null)
                return;
            try
            {
                animationTimeline.HotSwapNode(creatableNode);
            }
            catch (Exception ex)
            {
                MessageBox.Show("error hot swaping node " + creatableNode.NameInFile + ":\n" + ex.Message);
            }
        }

        private void Yes_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void Open_Executed(object sender, ExecutedRoutedEventArgs e) => OpenFile();
        private void Save_Executed(object sender, ExecutedRoutedEventArgs e) => SaveFile();
    }
}
