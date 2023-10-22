using AshampooApp.Commands;
using System;
using System.Windows.Input;

namespace AshampooApp.ViewModels
{
    public class ErrorDialogViewModel
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public ICommand OKCommand { get; set; }

        private readonly Action _closeAction;

        public ErrorDialogViewModel(Action closeAction)
        {
            _closeAction = closeAction;
            OKCommand = new DelegateCommand(Close, null);
        }

        private void Close()
        {
            _closeAction?.Invoke();
        }
    }
}
