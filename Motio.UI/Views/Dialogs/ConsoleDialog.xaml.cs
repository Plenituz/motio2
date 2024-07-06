using Motio.Debuging;
using System.Windows;

namespace Motio.UI.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ConsoleDialog.xaml
    /// </summary>  
    public partial class ConsoleDialog : Window
    {
        public ConsoleDialog()
        {
            InitializeComponent();
            consoleOut.Text = Logger.Instance.builder.ToString();
            Logger.Instance.NewLine += Instance_NewLine;
            scroller.ScrollToBottom();
        }

        private void Instance_NewLine(string obj)
        {
            Application.Current.Dispatcher.Invoke(() => 
            {
                consoleOut.Text += obj;
                scroller.ScrollToBottom();
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Logger.Instance.NewLine -= Instance_NewLine;
        }

        private void Window_Initialized(object sender, System.EventArgs e)
        {
            this.Left = 500;
            this.Top = 500;
        }
    }
}
