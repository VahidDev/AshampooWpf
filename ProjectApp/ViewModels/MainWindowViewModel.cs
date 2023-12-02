using Project.Domain.Constants;
using ProjectApp.Abstraction;
using ProjectApp.Views;
using Prism.Mvvm;
using Prism.Regions;

namespace ProjectApp.ViewModels
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