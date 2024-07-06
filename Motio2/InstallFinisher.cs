using Motio.UICommon;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Motio2
{
    public class InstallFinisher
    {
        /// <summary>
        /// full path to current directory, equivalent to relative path "."
        /// </summary>
        private string dot;
        /// <summary>
        /// full path to directory parent of the current one, equivalent to relative path ".."
        /// </summary>
        private string dotdot;

        public event Action<Exception> Error;
        public event Action<string> StatusUpdate;
        StepMachine machine;
        

        public InstallFinisher()
        {
            dot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            dotdot = Directory.GetParent(dot).FullName;
        }

        public void Retry()
        {
            machine.TryRunNextStep();
        }

        public void FinishInstall()
        {
            machine = new StepMachine()
            {
                autoRetry = 3
            };
            machine.AddStep(CheckOldInstall, "Checking integrity");
            machine.AddStep(EmptyDotDot, "Removing old install");
            machine.AddStep(CopyInstallToDotDot, "Installing new version");
            machine.AddStep(RebootInFreshInstall, "Rebooting");
            machine.RunningStep += Machine_RunningStep;
            machine.Error += Machine_Error;

            machine.TryRunNextStep();

            //operationQueue.RunInOtherThread = true;
            //operationQueue.LaunchSequence(machine);
        }

        private void Machine_Error(Exception ex)
        {
            Error?.Invoke(ex);
        }

        private void Machine_RunningStep()
        {
            StatusUpdate?.Invoke(machine.CurrentStep);
        }

        private void CheckOldInstall(StepMachine machine)
        {
            DirectoryInfo oldInstall = new DirectoryInfo(dotdot);
            //make sure the ".." folder has a Motio2.exe so we don't delete some random files 
            if (!oldInstall.GetFiles().Select(f => f.Name).Contains("Motio2.exe"))
                throw new Exception("The directory " + dotdot + " doesn't contain an install of Water Motion");
            UpdateProgress(1 / 3 * 100);
            machine.TryRunNextStep();
        }

        private void EmptyDotDot(StepMachine machine)
        {

            EmptyDirectory(dotdot, "new");
            UpdateProgress(2 / 3 * 100);

            machine.TryRunNextStep();
        }

        private void CopyInstallToDotDot(StepMachine machine)
        {
            CopyDirectory(dot, dotdot);
            UpdateProgress(3 / 3 * 100);

            machine.TryRunNextStep();
        }

        private void RebootInFreshInstall(StepMachine machine)
        {
            Process.Start(Path.Combine(dotdot, "Motio2.exe")).WaitForInputIdle();
            Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
        }

        private void UpdateProgress(int val)
        {
            //Application.Current.Dispatcher.Invoke(() => operationQueue.SetProgress(val));
        }

        private static void CopyDirectory(string SourcePath, string DestinationPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(SourcePath, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(SourcePath, "*.*",
                SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath), true);
        }

        private static void EmptyDirectory(string path, params string[] except)
        {
            DirectoryInfo directory = new DirectoryInfo(path);
            foreach (FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in directory.GetDirectories())
            {
                if (!except.Contains(dir.Name))
                    dir.Delete(true);
            }
        }
    }
}
