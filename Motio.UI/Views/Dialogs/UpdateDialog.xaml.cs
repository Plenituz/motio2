using Motio.Configuration;
using Motio.UICommon;
using Motio.UICommon.VersionChecking;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Windows;

namespace Motio.UI.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for UpdateDialog.xaml
    /// </summary>
    public partial class UpdateDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Mode { get; set; } = "machine";
        //Directory.GetDirectory sucks, it give the console directory not the executable directory
        public string FilePath => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "latestBuild.zip");

        private UICommon.VersionChecking.Version serverVersion;
        private string updateChannel;
        private string[] changeLog;
        private StepMachine machine;
        private bool loaded = false;
        private string[] choices = new string[]
        {
            "Just download",
            "Download and install",
            "Ignore for now",
            "Ignore this version"
        };

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public UpdateDialog(string updateChannel, UICommon.VersionChecking.Version serverVersion, string[] changeLog)
        {
            this.updateChannel = updateChannel;
            this.serverVersion = serverVersion;
            this.changeLog = changeLog;
            DataContext = this;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!loaded)
            {
                loaded = true;
                Activate();
                operationQueue.Choices = choices;
                operationQueue.Question = $"A new update is available in the '{updateChannel}' channel";
                operationQueue.CustomQuestionControl = new ChangeLogView(changeLog);
                operationQueue.ChoiceSelected += OperationQueue_ChoiceSelected;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = machine != null;
        }

        private void OperationQueue_ChoiceSelected(string choice)
        {
            switch(choice)
            {
                case "Just download":
                    DL_Click();
                    break;
                case "Download and install":
                    DL_INS_Click();
                    break;
                case "Ignore for now":
                    IGN_Click();
                    break;
                case "Ignore this version":
                    IGN_VER_Click();
                    break;
            }
        }

        private void DL_Click()
        {
            machine = new StepMachine();
            machine.AddStep(StartDownload, "Downloading");
            machine.AddStep(DownloadComplete, "Complete");
            operationQueue.LaunchSequence(machine);
        }

        private void DL_INS_Click()
        {
            machine = new StepMachine();
            machine.AddStep(StartDownload, "Downloading");
            machine.AddStep(SetupDirectory, "Directory setup");
            machine.AddStep(Unzip, "Extracting");
            machine.AddStep(BackupAppSkin, "Backing up appskin.xaml");
            machine.AddStep(BackupPrefs, "Backing up prefs");
            machine.AddStep(BackupConfig, "Backing up configs");
            machine.AddStep(BackupScripts, "Backing up scripts");
            machine.AddStep(RebootWithArgs, "Rebooting");
            operationQueue.RunInOtherThread = true;
            operationQueue.LaunchSequence(machine);
        }

        private void IGN_Click()
        {
            Close();
        }

        private void IGN_VER_Click()
        {
            Preferences.SetValue(Preferences.IgnoreVersion, serverVersion.ToString());
            Preferences.Save();
            Close();
        }

        private void SetupDirectory(StepMachine machine)
        {
            if (Directory.Exists("new"))
                new DirectoryInfo("new").Delete(true);
            Directory.CreateDirectory("new");
            machine.TryRunNextStep();
        }

        private void Unzip(StepMachine machine)
        {
            ZipFile.ExtractToDirectory("latestBuild.zip", "new");
            machine.TryRunNextStep();
        }

        private void BackupConfig(StepMachine machine)
        {
            if (!File.Exists("configs"))
            {
                machine.TryRunNextStep();
                return;
            }
            File.Copy("configs", Path.Combine("new", "configs"));
            machine.TryRunNextStep();
        }

        private void BackupPrefs(StepMachine machine)
        {
            if (!File.Exists("prefs"))
            {
                machine.TryRunNextStep();
                return;
            }
            File.Copy("prefs", Path.Combine("new", "prefs"));
            machine.TryRunNextStep();
        }

        private void BackupAppSkin(StepMachine machine)
        {
            if (!File.Exists("appskin.xaml"))
            {
                machine.TryRunNextStep();
                return;
            }
            File.Copy("appskin.xaml", Path.Combine("new", "appskin.xaml"));
            machine.TryRunNextStep();
        }

        private void BackupScripts(StepMachine machine)
        {
            //this assumes there is a "new" folder containing the new install of motio
            //copy all the scripts that are in the old install and not in the new one
            if (!Directory.Exists("Addons"))
            {
                machine.TryRunNextStep();
                return;
            }
            string[] files = Directory.GetFiles("Addons", "*.*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string newPath = Path.Combine("new", file);
                if (File.Exists(newPath))
                    continue;
                Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                File.Copy(file, newPath);
            }
            machine.TryRunNextStep();
        }

        private void RebootWithArgs(StepMachine machine)
        {
            Process.Start(Path.Combine("new", "Motio2.exe"), "--finish-install").WaitForInputIdle();
            Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
        }

        private void StartDownload(StepMachine machine)
        {
            FileDownloader downloader = new FileDownloader(serverVersion.DownloadUrl(), "latestBuild.zip");
            downloader.DownloadProgress += Downloader_DownloadProgress;
            downloader.DownloadCompleted += Downloader_DownloadCompleted;
            downloader.Error += Downloader_Error;
            downloader.StartDownload();
        }

        private void DownloadComplete(StepMachine machine)
        {
            Mode = "dldone";
            this.machine = null;
        }

        private void Downloader_DownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => operationQueue.SetProgress(e.ProgressPercentage));
        }
        private void Downloader_DownloadCompleted() => machine.TryRunNextStep();
        private void Downloader_Error(Exception ex) => machine.FailCurrentStep(ex);
    }
}
