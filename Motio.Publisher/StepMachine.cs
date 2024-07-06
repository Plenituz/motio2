using System;
using System.Collections.Generic;
using System.Threading;

namespace Motio.UICommon
{
    public class StepMachine
    {
        private List<Action<StepMachine>> steps = new List<Action<StepMachine>>();
        private List<string> stepNames = new List<string>();
        private int currentStep = 0;
        public bool HasNextStep => currentStep < steps.Count;
        public string CurrentStep => currentStep > 0 ? stepNames[currentStep - 1] : "";
        public event Action<Exception> Error;
        public event Action RunningStep;
        public int autoRetry = 0;
        public int autoRetryWaitMs = 500;

        public void AddStep(Action<StepMachine> step, string name)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));
            steps.Add(step);
            stepNames.Add(name);
        }

        public void FailCurrentStep(Exception ex)
        {
            Error?.Invoke(ex);
        }

        public void TryRunNextStep()
        {
            TryRunNextStep(out Exception e);
        }

        public bool TryRunNextStep(out Exception ex)
        {
            if (!HasNextStep)
                throw new IndexOutOfRangeException("No more steps to run");
            try
            {
                RunningStep?.Invoke();
                currentStep++;
                steps[currentStep - 1](this);

                ex = null;
                return true;
            }
            catch (Exception e)
            {
                currentStep--;
                if (autoRetry > 0)
                {
                    autoRetry--;
                    Thread.Sleep(autoRetryWaitMs);
                    return TryRunNextStep(out ex);
                }
                else
                {
                    ex = e;
                    Error?.Invoke(e);
                    return false;
                }
            }
        }
    }
}
