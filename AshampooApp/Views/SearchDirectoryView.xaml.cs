using AshampooApp.ViewModels;
using System.Windows.Controls;

namespace AshampooApp.Views
{
    public partial class SearchDirectoryView : UserControl
    {
        public SearchDirectoryView()
        {
            InitializeComponent();
            DataContext = new SearchDirectoryViewModel();
        }
    }
}
