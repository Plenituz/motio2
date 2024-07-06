using Motio.UICommon;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Motio.UI.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for OperationQueueControl.xaml
    /// </summary>
    public partial class OperationQueueControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<string> ChoiceSelected;

        public string[] Choices { get; set; } = new string[] { "nothing", "nothing" };
        public string Question { get; set; } = "Question";
        public string Mode { get; set; } = "question";
        public string CurrentStep => machine?.CurrentStep;
        public string ErrorMsg { get; set; }
        public bool RunInOtherThread { get; set; } = false;
        public Control CustomQuestionControl { get; set; }

        private StepMachine machine;

        public OperationQueueControl()
        {
            DataContext = this;
            InitializeComponent();
        }

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ChoiceSelected?.Invoke((string)((Button)sender).Content);
        }

        public void LaunchSequence(StepMachine machine)
        {
            if (this.machine != null)
            {
                this.machine.Error -= Machine_Error;
                this.machine.RunningStep -= Machine_RunningStep;
            }

            this.machine = machine;
            this.machine.Error += Machine_Error;
            this.machine.RunningStep += Machine_RunningStep;
            StartStepMachine();
        }

        private void Machine_RunningStep()
        {
            OnPropertyChanged(nameof(CurrentStep));
        }

        private void Machine_Error(Exception ex)
        {
            ErrorMsg = ex.Message;
            Mode = "error";
        }

        private void TryAgain_Click(object sender, RoutedEventArgs e)
        {
            StartStepMachine();
        }

        private void StartStepMachine()
        {
            Mode = "stepRunning";
            if (RunInOtherThread)
                new Task(() => machine.TryRunNextStep()).Start();
            else
                machine.TryRunNextStep();
        }

        public void SetProgress(int percent)
        {
            progress.Value = percent;
        }
    }
}
