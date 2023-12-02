using ProjectApp.Abstraction;
using System.Windows.Controls;

namespace ProjectApp.Views
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
