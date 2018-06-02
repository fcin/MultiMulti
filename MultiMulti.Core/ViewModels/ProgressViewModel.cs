using Caliburn.Micro;

namespace MultiMulti.Core.ViewModels
{
    public class ProgressViewModel : Screen
    {
        private string _message;

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                NotifyOfPropertyChange(() => Message);
            }
        }

        private double _currentProgress;
        public double CurrentProgress
        {
            get => _currentProgress;
            set
            {
                _currentProgress = value;
                NotifyOfPropertyChange(() => CurrentProgress);
            }
        }
    }
}
