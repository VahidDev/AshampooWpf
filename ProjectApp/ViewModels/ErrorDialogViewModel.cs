using ProjectApp.Commands;
using Prism.Mvvm;
using System;
using System.Windows.Input;

namespace ProjectApp.ViewModels
{
    public class ErrorDialogViewModel : BindableBase
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
