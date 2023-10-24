using AshampooApp.Abstraction;
using System.Windows;

namespace AshampooApp
{
    public partial class MainWindow : Window
    {
        public MainWindow(IMainWindowViewModel mainWindowViewModel)
        {
            InitializeComponent();
            DataContext = mainWindowViewModel;
        }
    }
}