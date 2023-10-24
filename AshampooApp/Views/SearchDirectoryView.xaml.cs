using AshampooApp.Abstraction;
using System.Windows.Controls;

namespace AshampooApp.Views
{
    public partial class SearchDirectoryView : UserControl
    {
        public SearchDirectoryView(ISearchDirectoryViewModel searchDirectoryViewModel)
        {
            InitializeComponent();
            DataContext = searchDirectoryViewModel;
        }
    }
}
