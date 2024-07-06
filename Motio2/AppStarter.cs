using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Motio.Configuration;
using Motio.NodeCommon.StandardInterfaces;
using Motio.NodeImpl;
using Motio.PythonRunning;
using Motio.UI.Utils.Export;
using Motio.UI.ViewModels;
using Motio.UI.Views.Dialogs;
using Motio.UICommon.VersionChecking;
using Motio.Undoing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Xml;

namespace Motio2
{
    public class AppStarter
    {
        public MainControlViewModel mainViewModel;
        ResourceDictionary colorDict;
        public event Action<AppStarter> Started;
        public event Action<string> StatusUpdate;
        public event Action<Exception> Error;
        InstallFinisher finisher;


        public AppStarter()
        {
        }

        public void RetryFinishingInstall()
        {
            finisher.Retry();
        }

        public void Start()
        {
            //the configs are needed by the updater
            UpdateStatus("Reading configs...");
            InitConfigs();
            UpdateStatus("Finishing install...");
            if (DoFinishInstall())
                return;
            UpdateStatus("Loading skin...");
            LoadColorResources();

            Init();
            UpdateStatus("Running startup script...");
            RunStartupScripts();
            UpdateStatus("Starting version checker...");
            StartVersionChecker();
            UpdateStatus("Done!");
            Application.Current.Dispatcher.Invoke(() => Started?.Invoke(this));
        }

        private void UpdateStatus(string status)
        {
            Application.Current.Dispatcher.Invoke(() => StatusUpdate?.Invoke(status));
        }

        private void RunStartupScripts()
        {
            string[] args = Environment.GetCommandLineArgs();
            int indexScript = Array.IndexOf(args, "--script");
            if (indexScript != -1 && args.Length > indexScript + 1)
            {
                string path = args[indexScript + 1];
                Python.CompileAndRun(path, out _, new Dictionary<string, object>
                    {
                        { "timeline", mainViewModel.AnimationTimeline }
                    });
            }

            string startupScript = Configs.GetValue<string>(Configs.StartupScript);
            if (!string.IsNullOrEmpty(startupScript)
                && !string.IsNullOrWhiteSpace(startupScript)
                && File.Exists(startupScript))
            {
                Python.CompileAndRun(startupScript, out _, new Dictionary<string, object>
                    {
                        { "timeline", mainViewModel.AnimationTimeline }
                    });
            }
        }

        void Init()
        {
            //TODO multi thread some of the things here ?
            //call the Instance to init the engine
            Python.DynamicType = typeof(IDynamicNode);
            UpdateStatus("Breeding pythons...");
            var python = Python.Instance;

            UndoStack.Clear();

            RegisterPythonHighlighting();
            string scanErrors = NodeScanner.ScanDynamicNodes();
            if (!string.IsNullOrEmpty(scanErrors))
            {
                MessageBox.Show(scanErrors, "Node scanning error");
            }
            UpdateStatus("Importing exporter...");
            TimelineExporter.PopulateExporters();

            AnimationTimelineViewModel timeline = null;

            UpdateStatus("Creating timeline...");
            string[] args = Environment.GetCommandLineArgs();
            string defaultFile;
            if (args.Length == 2)
                defaultFile = args[1];
            else
                defaultFile = Configs.GetValue<string>(Configs.DefaultFile);

            if (!string.IsNullOrWhiteSpace(defaultFile))
            {
                timeline = AnimationTimelineViewModel.CreateFromFile(defaultFile);
            }
            if (timeline == null)
            {
                timeline = AnimationTimelineViewModel.Create();
            }
            UpdateStatus("Creating view-model...");
            mainViewModel = new MainControlViewModel(timeline);
        }

        void RegisterPythonHighlighting()
        {
            IHighlightingDefinition pythonHighlight = null;
            try
            {
                //to load from GetManifestResourceStream, you must put the namespace before the file name
                using (Stream s = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("Motio2.ICSharpCode.PythonBinding.Resources.Python.xshd"))
                {
                    using (XmlTextReader reader = new XmlTextReader(s))
                    {
                        pythonHighlight = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error setting syntax highlighting to Python:\n" + e
                    + "\nThis should not impact the node's behaviour", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            if (pythonHighlight != null)
                HighlightingManager.Instance.RegisterHighlighting("Python", new string[] { ".py" }, pythonHighlight);
        }


        private void StartVersionChecker()
        {
            new Thread(new ThreadStart(VersionCheckerJob)).Start();
        }

        private void VersionCheckerJob()
        {
            //try to delete the "new" directory in case we just updated
            try
            {
                if (Directory.Exists("new"))
                    new DirectoryInfo("new").Delete(true);
            }
            catch (Exception) { }
            UpdateChannels updateChannel = Configs.GetValue<UpdateChannels>(Configs.UpdateChannel);
            bool shouldUpdate = VersionChecker.ShouldUpdate(
                updateChannel.ToString(),
                out Motio.UICommon.VersionChecking.Version serverVersion,
                out string[] changeLog);
            if (shouldUpdate)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateDialog updateDialog = new UpdateDialog(updateChannel.ToString(), serverVersion, changeLog);
                    //try
                    //{
                    //    updateDialog.Owner = mainWindow;
                    //}
                    //catch (Exception) { }
                    updateDialog.ShowDialog();
                });
            }
        }

        private ResourceDictionary GetColorDict()
        {
            try
            {
                return new ResourceDictionary
                {
                    Source = new Uri("pack://siteoforigin:,,,/appskin.xaml", UriKind.RelativeOrAbsolute)
                };
            }
            catch (Exception e)
            {
                //if this crashes the app will crash anyway so we let it be an unhandled exception

                MessageBox.Show("couldn't load appskin.xaml, using default skin:\n" + e.Message);
                return new ResourceDictionary
                {
                    Source = new Uri(@"pack://application:,,,/Motio2;component/appskin.xaml")
                };
            }
        }

        private void CreateColorResources()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "Motio2.appskin.xaml";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    File.WriteAllText("appskin.xaml", result);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("couldn't create appskin.xaml file:\n" + e.Message);
            }
        }

        private void LoadColorResources()
        {
            if (!File.Exists("appskin.xaml"))
                CreateColorResources();
            if (colorDict != null)
            {
                //for live reloading
                //ResourceDictionary updatedDict = GetColorDict();
                //foreach (var key in updatedDict.Keys)
                //{
                //    mainWindow.Resources[key] = updatedDict[key];
                //}
            }
            else
            {
                colorDict = GetColorDict();
                Application.Current.Resources.MergedDictionaries.Add(colorDict);
            }
        }

        private bool DoFinishInstall()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length == 2 && args[1].Equals("--finish-install"))
            {
                //FinishingInstallDialog finishDialog = new FinishingInstallDialog();
                //finishDialog.ShowDialog();
                finisher = new InstallFinisher();
                finisher.StatusUpdate += UpdateStatus;
                finisher.Error += Finisher_Error;
                finisher.FinishInstall();
                return true;
            }
            return false;
        }

        private void Finisher_Error(Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() => Error?.Invoke(ex));
        }

        void InitConfigs()
        {
            try
            {
                Preferences.Load();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error while loading configs type: " + e.GetType() +
                    Environment.NewLine + "message: " + Environment.NewLine + e.Message
                    + Environment.NewLine + Environment.NewLine + "Configs may not have been loaded properly",
                    "Error loading configs", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            Configs.LoadSafe();
        }

    }
}
