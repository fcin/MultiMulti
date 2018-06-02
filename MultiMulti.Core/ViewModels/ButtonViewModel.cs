using Caliburn.Micro;
using System.Windows.Media;

namespace MultiMulti.Core.ViewModels
{
    public class ButtonViewModel : PropertyChangedBase
    {
        private string _buttonText;
        public string ButtonText
        {
            get => _buttonText;
            set
            {
                _buttonText = value;
                NotifyOfPropertyChange(() => ButtonText);
            }
        }

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                SetColor();
                NotifyOfPropertyChange(() => IsSelected);
            }
        }

        private Brush _selectionBrush;
        public Brush SelectionBrush
        {
            get => _selectionBrush;
            set
            {
                _selectionBrush = value; 
                NotifyOfPropertyChange(() => SelectionBrush);
            }
        }

        private readonly ShellViewModel _vm;

        public ButtonViewModel(string buttonText, ShellViewModel vm)
        {
            ButtonText = buttonText;
            _vm = vm;

            SetColor();
        }

        public void OnButtonClick()
        {
            // Let it deselect.
            if (IsSelected || _vm.CanSelectButton())
            {
                IsSelected = !IsSelected;
                SetColor();
            }
        }

        private void SetColor()
        {
            SelectionBrush = IsSelected ? new SolidColorBrush(Colors.BlueViolet) : new SolidColorBrush(Colors.Beige);
        }
    }
}
