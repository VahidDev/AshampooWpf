﻿using AshampooApp.ViewModels;
using System.Windows;
using System.Windows.Controls;


namespace AshampooApp.Dialogs
{
    public partial class ErrorDialog : UserControl
    {
        public ErrorDialog()
        {
            InitializeComponent();
            DataContext = new ErrorDialogViewModel(Close);
        }

        public void Close()
        {
            if (Parent is Window parentPanel)
            {
                parentPanel.Close();
            }
        }
    }
}
