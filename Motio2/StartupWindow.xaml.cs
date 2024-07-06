using System;
using System.Threading;
using System.Windows;

namespace Motio2
{
    /// <summary>
    /// Interaction logic for StartupWindow.xaml
    /// </summary>
    public partial class StartupWindow : Window
    {
        public AppStarter starter;

        public StartupWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            starter = new AppStarter();
            starter.Started += Starter_Started;
            starter.StatusUpdate += Starter_StatusUpdate;
            starter.Error += Starter_Error;

            new Thread(new ThreadStart(() => starter.Start())).Start();
        }

        private void Starter_Error(Exception ex)
        {
            Spinner.Visibility = Visibility.Collapsed;
            StatusText.Text = ex.Message;
            TryAgainBut.Visibility = Visibility.Visible;
        }

        private void Starter_StatusUpdate(string status)
        {
            StatusText.Text = status;
        }

        private void Starter_Started(AppStarter starter)
        {
            MainWindow mainWindow = new MainWindow(starter.mainViewModel);
            mainWindow.Loaded += MainWindow_Loaded;
            mainWindow.Show();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TryAgain_Click(object sender, RoutedEventArgs e)
        {
            Spinner.Visibility = Visibility.Visible;
            TryAgainBut.Visibility = Visibility.Collapsed;
            starter.RetryFinishingInstall();
        }
    }
}
