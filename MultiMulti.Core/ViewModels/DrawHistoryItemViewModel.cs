using System;
using Caliburn.Micro;

namespace MultiMulti.Core.ViewModels
{
    public class DrawHistoryItemViewModel : PropertyChangedBase
    {
        public Guid Id => _data.Id;
        public DateTime AddedDate => _data.Added;
        public string Pairs => string.Join(", ", _data.SelectedNumbers);

        private readonly ShellViewModel _parent;
        private readonly Data _data;

        public DrawHistoryItemViewModel(ShellViewModel parent, Data data)
        {
            _parent = parent;
            _data = data;
        }

        public void RemoveDrawHistoryItem()
        {
            _parent.RemoveDrawHistoryItem(Id);
        }
    }
}
