using System.Collections.ObjectModel;
using System.Windows.Input;
using AshampooApp.Domain;
using Ashampoo.Abstraction;
using AshampooApp.Commands;
using Ashampoo.Abstraction.ViewModel;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using System;
using AshampooApp.Dialogs;

namespace AshampooApp.ViewModels
{
    public class SearchDirectoryViewModel
        : ViewModelBase

    {
        private string[] _allDirectoryPaths;
        private IDictionary<string, IEnumerable<FileInfo>> _directoryFiles;
        private CancellationTokenSource _searchCancellationTokenSource;
        private int _lastProcessedDirectoryIndex;

        public SearchDirectoryViewModel()
        {
            UiDirectories = new ObservableCollection<DirectoryInfoModel>();
        }

        private ObservableCollection<DirectoryInfoModel> _uiDirectories;
        public ObservableCollection<DirectoryInfoModel> UiDirectories
        {
            get { return _uiDirectories; }
            set
            {
                _uiDirectories = value;
                OnPropertyChanged(nameof(UiDirectories));
            }
        }

        private string _searchPath;
        public string SearchPath
        {
            get { return _searchPath; }
            set
            {
                _searchPath = value;
                OnPropertyChanged(nameof(SearchPath));
            }
        }

        private string _selectedDrive;
        public string SelectedDrive
        {
            get { return _selectedDrive; }
            set
            {
                if (_selectedDrive != value)
                {
                    _selectedDrive = value;
                    OnPropertyChanged(nameof(SelectedDrive));
                    (_searchCommand as ICanExecuteChangeable)?.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isSearching;
        public bool IsSearching
        {
            get { return _isSearching; }
            set
            {
                if (_isSearching != value)
                {
                    _isSearching = value;

                    OnPropertyChanged(nameof(IsSearching));
                    (_pauseCommand as ICanExecuteChangeable)?.RaiseCanExecuteChanged();
                    (_resumeCommand as ICanExecuteChangeable)?.RaiseCanExecuteChanged();
                    (_searchCommand as ICanExecuteChangeable)?.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isPaused;
        public bool IsPaused
        {
            get { return _isPaused; }
            set
            {
                if (_isPaused != value)
                {
                    _isPaused = value;

                    OnPropertyChanged(nameof(IsPaused));
                    (_pauseCommand as ICanExecuteChangeable)?.RaiseCanExecuteChanged();
                }
            }
        }

        private ICommand _searchCommand;
        public ICommand SearchCommand
        {
            get
            {
                if (_searchCommand == null)
                {
                    _searchCommand = new DelegateCommand(async () => await SearchAsync(true), CanSearch);
                }
                return _searchCommand;
            }
        }

        private ICommand _pauseCommand;
        public ICommand PauseCommand
        {
            get
            {
                if (_pauseCommand == null)
                {
                    _pauseCommand = new DelegateCommand(PauseSearch, CanPauseSearch);
                }
                return _pauseCommand;
            }
        }

        private ICommand _resumeCommand;

        public ICommand ResumeCommand
        {
            get
            {
                if (_resumeCommand == null)
                {
                    _resumeCommand = new DelegateCommand(async () => await ResumeSearch(), CanResumeSearch);
                }
                return _resumeCommand;
            }
        }

        private bool CanSearch()
        {
            return !IsSearching && !string.IsNullOrWhiteSpace(SelectedDrive);
        }

        private bool CanPauseSearch()
        {
            return IsSearching;
        }

        private bool CanResumeSearch()
        {
            return !IsSearching && _isPaused;
        }

        private async Task SearchAsync(bool searchFromStart)
        {
            IsSearching = true;
            IsPaused = false;

            if (searchFromStart)
            {
                ResetSearch();
            }

            _searchCancellationTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Run(() =>
                {
                    var enumerationOptions = new EnumerationOptions
                    {
                        IgnoreInaccessible = true,
                        RecurseSubdirectories = false
                    };

                    if (searchFromStart)
                    {
                        InitializeDirectoryCollections(enumerationOptions);
                    }

                    ProcessDirectories(enumerationOptions);
                });
            }
            catch (Exception ex)
            {
                ShowModal("Error", "An error occurred: " + ex.Message);
            }
            finally
            {
                IsSearching = false;
            }
        }

        private void ProcessDirectories(EnumerationOptions enumerationOptions)
        {
            for (int i = _lastProcessedDirectoryIndex; i < _allDirectoryPaths.Length; i++)
            {
                var directoryPath = _allDirectoryPaths[i];

                if (IsCancellationRequested())
                {
                    return;
                }

                var directoryInfo = new DirectoryInfo(directoryPath);

                _directoryFiles.TryAdd(directoryPath, directoryInfo.EnumerateFiles("*", enumerationOptions)
                                                                   .OrderBy(f => f.Name));
                var files = _directoryFiles[directoryPath];

                ProcessFiles(files, directoryInfo);

                Interlocked.Increment(ref _lastProcessedDirectoryIndex);
            }
        }

        private void ProcessFiles(IEnumerable<FileInfo> files, DirectoryInfo directoryInfo)
        {
            long totalSize = 0;
            int fileCount = 0;

            Parallel.ForEach(files, file =>
            {
                if (IsCancellationRequested())
                {
                    return;
                }

                if (IsFileSizeLargerThanMegabytes(10, file))
                {
                    Interlocked.Increment(ref fileCount);
                    Interlocked.Add(ref totalSize, file.Length);
                }
            });

            if (fileCount == 0)
            {
                return;
            }

            var directoryModel = new DirectoryInfoModel
            {
                DirectoryPath = directoryInfo.FullName,
                FileCount = fileCount,
                TotalSize = totalSize
            };

            Application.Current.Dispatcher.Invoke(() =>
            {
                UiDirectories.Add(directoryModel);
            });
        }

        private bool IsFileSizeLargerThanMegabytes(int mg, FileInfo file)
        {
            return file.Length > mg * 1024 * 1024;
        }

        private void InitializeDirectoryCollections(EnumerationOptions enumerationOptions)
        {
            _allDirectoryPaths = Directory.EnumerateDirectories(SelectedDrive, "*", enumerationOptions)
                                                    .OrderBy(d => d).ToArray();

            _directoryFiles = new Dictionary<string, IEnumerable<FileInfo>>();
        }

        private void ResetSearch()
        {
            UiDirectories.Clear();
            _lastProcessedDirectoryIndex = 0;
        }

        private void PauseSearch()
        {
            IsPaused = true;
            _searchCancellationTokenSource?.Cancel();
        }

        private async Task ResumeSearch()
        {
            await SearchAsync(false);
        }

        private bool IsCancellationRequested()
        {
            return _searchCancellationTokenSource.Token.IsCancellationRequested;
        }

        private void ShowModal(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var errorDialog = new ErrorDialog();

                var errorDialogViewModel = errorDialog.DataContext as ErrorDialogViewModel;
                errorDialogViewModel.Title = title;
                errorDialogViewModel.Message = message;

                var errorDialogWindow = new Window
                {
                    Content = errorDialog,
                    Title = title,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = ResizeMode.NoResize,
                };

                errorDialogWindow.ShowDialog();
            });
        }
    }
}
