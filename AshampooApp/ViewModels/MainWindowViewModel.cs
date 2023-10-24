using Ashampoo.Domain.Constants;
using AshampooApp.Abstraction;
using AshampooApp.Views;
using Prism.Mvvm;
using Prism.Regions;

namespace AshampooApp.ViewModels
{
    public class MainWindowViewModel 
        : BindableBase
        , IMainWindowViewModel
    {
        public MainWindowViewModel(IRegionManager regionManager)
        {
            regionManager.RequestNavigate(RegionNames.MainRegion, nameof(SearchDirectoryView));
        }
    }
}