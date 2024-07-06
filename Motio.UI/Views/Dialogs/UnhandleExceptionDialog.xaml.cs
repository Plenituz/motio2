using Motio.UICommon;
using Motio.UICommon.VersionChecking;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace Motio.UI.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for UnhandleExceptionDialog.xaml
    /// </summary>
    public partial class UnhandleExceptionDialog : Window
    {
        private Exception unhandledException;
        private Window mainWindow;
        private bool loaded = false;
        private string[] choices = new string[]
        {
            "Try to save",
            "Send report and close",
            "Close"
        };

        public UnhandleExceptionDialog(Exception exception, Window mainWindow)
        {
            this.mainWindow = mainWindow;
            this.unhandledException = exception;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!loaded)
            {
                loaded = true;
                operationQueue.Question = "An unexpected " 
                    + unhandledException.GetType() + " occured:\n" 
                    + unhandledException.Message;
                operationQueue.Choices = choices;
                operationQueue.RunInOtherThread = true;
                operationQueue.ChoiceSelected += OperationQueue_ChoiceSelected;

                ExceptionStackView stack = new ExceptionStackView
                {
                    Text = unhandledException.ToString()
                };
                operationQueue.CustomQuestionControl = stack;
            }
        }

        private void OperationQueue_ChoiceSelected(string choice)
        {
            switch(choice)
            {
                case "Try to save":
                    Save_Click();
                    break;
                case "Send report and close":
                    Send_Click();
                    break;
                case "Close":
                    Close();
                    break;
            }
        }

        private void Save_Click()
        {
            dynamic window = mainWindow;
            try
            {
                window.ExecuteSave(true);
                MessageBox.Show("Sucessfully saved");
            }
            catch (Exception ex)
            {
                MessageBox.Show("couldn't save:\n", ex.Message);
            }
        }

        private void Send_Click()
        {
            StepMachine machine = new StepMachine();
            machine.AddStep(SendException, "Sending report");
            machine.AddStep(CloseWind, "Closing");
            operationQueue.LaunchSequence(machine);
        }

        private void CloseWind(StepMachine machine)
        {
            Application.Current.Shutdown();
            Application.Current.Dispatcher.Invoke(Close);
        }

        private void SendException(StepMachine machine)
        {
            HttpClient client = new HttpClient();
            var exceptionMini = new
            {
                ClassName = unhandledException.GetType(),
                unhandledException.Message,
                unhandledException.Data,
                unhandledException.StackTrace,
                unhandledException.Source
            };
            string exceptionJson = JsonConvert.SerializeObject(exceptionMini);
            var values = new Dictionary<string, string>
            {
               { "ex", exceptionJson }
            };

            var content = new FormUrlEncodedContent(values);
            Task<HttpResponseMessage> postTask = client.PostAsync(VersionChecker.URL_CRASH_REPORT, content);
            postTask.Wait();

            var response = postTask.Result;
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("Error sending report, server answered with " + response.StatusCode);
            response.Dispose();

            machine.TryRunNextStep();
        }
    }
}
