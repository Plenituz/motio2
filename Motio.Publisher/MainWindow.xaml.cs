using Motio.Configuration;
using Motio.UICommon;
using PropertyChanged;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;

namespace Motio.Publisher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public const string CURRENT_JSON_URL = "https://plenicorp.com/build/current.json";

        public ServerData ServerData { get; set; }
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int Version { get; set; }
        public string ChangeLog { get; set; }

        SftpClient client;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            PropertyChanged += MainWindow_PropertyChanged;
            Preferences.Load();
            InitializeComponent();
        }

        private void MainWindow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(new List<string>(new[] { nameof(Day), nameof(Month), nameof(Year), nameof(Version), nameof(ServerData) }).Contains(e.PropertyName))
                ChangeLog = string.Join(Environment.NewLine, ServerData.GetOrMakeChangeLog(MakeChangeLogId()).Entries);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string currentJson = HttpGet(CURRENT_JSON_URL);
            ServerData = new ServerData(currentJson);
            ParseCurrentSource();

            operationQueue.Choices = new string[]
            {
                "Go"
            };
            operationQueue.Question = "";
            operationQueue.ChoiceSelected += OperationQueue_ChoiceSelected;
            operationQueue.RunInOtherThread = true;

            PrivateKeyPath.Text = Preferences.GetValue<string>(Preferences.PrivateKeyPath);
            PathToVersionCs.Text = Preferences.GetValue<string>(Preferences.PathToVersionCs);
            BuildOutputDir.Text = Preferences.GetValue<string>(Preferences.BuildOutputDir);
            ChangeLog = string.Join(Environment.NewLine, ServerData.GetOrMakeChangeLog(MakeChangeLogId()).Entries);

            DataContext = this;
        }

        private void ParseCurrentSource()
        {
            string versionSource = File.ReadAllText(Preferences.GetValue<string>(Preferences.PathToVersionCs));
            Regex regex = new Regex(@"public static Version version = new Version\(new DateTime\((\d\d\d\d), (\d+?), (\d+?)\), (\d+)\);");
            Match match = regex.Match(versionSource);
            Year = int.Parse(match.Groups[1].Value);
            Month = int.Parse(match.Groups[2].Value);
            Day = int.Parse(match.Groups[3].Value);
            Version = int.Parse(match.Groups[4].Value);
        }

        private void OperationQueue_ChoiceSelected(string step)
        {
            Preferences.SetValue(Preferences.PrivateKeyPath, PrivateKeyPath.Text);
            Preferences.SetValue(Preferences.PathToVersionCs, PathToVersionCs.Text);
            Preferences.SetValue(Preferences.BuildOutputDir, BuildOutputDir.Text);
            Preferences.Save();

            ServerData.GetOrMakeChangeLog(MakeChangeLogId()).Entries = new System.Collections.ObjectModel.ObservableCollection<string>(ChangeLog.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

            StepMachine machine = new StepMachine();
            machine.AddStep(ModifyVersionSource, "Modifying source");
            machine.AddStep(ClearDir, "Clearing directory");
            machine.AddStep(WaitForBuild, "Waiting for build");
            machine.AddStep(ZipFiles, "Zipping files");
            machine.AddStep(SendFile, "Sending archive");
            machine.AddStep(UpdateCurrentJson, "Updating json");
            machine.AddStep(DeleteZip, "Deleting zip");
            machine.AddStep(Stopping, "Stopping");
            operationQueue.LaunchSequence(machine);
        }

        private void ModifyVersionSource(StepMachine machine)
        {
            string pathToVersionSrc = Preferences.GetValue<string>(Preferences.PathToVersionCs);
            string versionSource = File.ReadAllText(pathToVersionSrc);
            Regex regex = new Regex(@"public static Version version = new Version\(new DateTime\((\d\d\d\d), (\d+?), (\d+?)\), (\d+)\);");
            Match match = regex.Match(versionSource);
            versionSource = versionSource.Replace(match.Value, $"public static Version version = new Version(new DateTime({Year}, {Month}, {Day}), {Version});");
            File.WriteAllText(pathToVersionSrc, versionSource);

            machine.TryRunNextStep();
        }

        private void ClearDir(StepMachine machine)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(Preferences.GetValue<string>(Preferences.BuildOutputDir));
            foreach (FileInfo file in di.EnumerateFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.EnumerateDirectories())
            {
                dir.Delete(true);
            }
            machine.TryRunNextStep();
        }

        private void WaitForBuild(StepMachine machine)
        {
            MessageBox.Show("Do the build");
            machine.TryRunNextStep();
        }

        private void ZipFiles(StepMachine machine)
        {
            string buildOutputDir = Preferences.GetValue<string>(Preferences.BuildOutputDir);
            string zipPath = Path.Combine(buildOutputDir, "..", MakeArchiveName());
            if (File.Exists(zipPath))
                File.Delete(zipPath);
            ZipFile.CreateFromDirectory(buildOutputDir, zipPath, CompressionLevel.Optimal, false);

            machine.TryRunNextStep();
        }

        private void SendFile(StepMachine machine)
        {
            string zipPath = Path.Combine(Preferences.GetValue<string>(Preferences.BuildOutputDir), "..", MakeArchiveName());
            ConnectionInfo connectionInfo = new ConnectionInfo(
                "137.74.160.172",
                "notadmin",
                new PrivateKeyAuthenticationMethod("notadmin", new PrivateKeyFile(Preferences.GetValue<string>(Preferences.PrivateKeyPath))));


            client = new SftpClient(connectionInfo);
            client.Connect();

            using (var stream = new FileStream(zipPath, FileMode.Open))
            {
                FileInfo info = new FileInfo(zipPath);
                ulong size = (ulong)info.Length;
                client.UploadFile(stream, "/home/notadmin/motio/public/build/" + MakeArchiveName(),
                val =>
                {
                    float progress = val / (float)size * 100;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        operationQueue.SetProgress((int)progress);
                    });
                });
            }
            machine.TryRunNextStep();
        }

        private void UpdateCurrentJson(StepMachine machine)
        {
            string newJson = ServerData.ToJson();
            using (Stream stream = GenerateStreamFromString(newJson))
            using(client)
            {
                ulong size = (ulong)stream.Length;
                client.UploadFile(stream, "/home/notadmin/motio/public/build/current.json",
                val =>
                {
                    float progress = val / (float)size * 100;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        operationQueue.SetProgress((int)progress);
                    });
                });
            }
            machine.TryRunNextStep();
        }

        private void DeleteZip(StepMachine machine)
        {
            string buildOutputDir = Preferences.GetValue<string>(Preferences.BuildOutputDir);
            string zipPath = Path.Combine(buildOutputDir, "..", MakeArchiveName());
            File.Delete(zipPath);
            machine.TryRunNextStep();
        }

        private void Stopping(StepMachine machine)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.Shutdown();
            });
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private static string HttpGet(string url)
        {
            string html = string.Empty;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
            }
            return html;
        }

        private string MakeArchiveName()
        {
            return $"motio_build_{Month}_{Day}_{Year}_{Version}.zip";
        }

        private string MakeChangeLogId()
        {
            return $"{Month}/{Day}/{Year}/{Version}";
        }

        private void SetToToday_Click(object sender, RoutedEventArgs e)
        {
            DateTime today = DateTime.Now;
            Day = today.Day;
            Month = today.Month;
            Year = today.Year;
        }

        private void IncrementVersion_Click(object sender, RoutedEventArgs e)
        {
            Version++;
        }

        private void SetToCurrent_Click(object sender, RoutedEventArgs e)
        {
            Channel channel = (Channel)((FrameworkElement)sender).DataContext;
            channel.VersionDay = Day;
            channel.VersionMonth = Month;
            channel.VersionYear = Year;
            channel.VersionNumber = Version;
        }

        private void SetTo0_Click(object sender, RoutedEventArgs e)
        {
            Version = 0;
        }
    }
}
