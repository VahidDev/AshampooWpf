using System.Collections.ObjectModel;
using System.Windows.Input;
using ProjectApp.Domain;
using Project.Abstraction;
using ProjectApp.Commands;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using System;
using ProjectApp.Dialogs;
using Prism.Mvvm;
using ProjectApp.Abstraction;
using System.Collections.Concurrent;

namespace ProjectApp.ViewModels
{
    public class SearchDirectoryViewModel 
        : BindableBase
        , ISearchDirectoryViewModel
        , IDisposable

    {
        private Task _searchTask;
        private CancellationTokenSource _searchCancellationTokenSource;
        private ConcurrentDictionary<string,string> _processedDirectories;
        private readonly EnumerationOptions _fileSearchOptions;
        private readonly EnumerationOptions _directorySearchOptions;
        private readonly SemaphoreSlim _semaphore;

        public SearchDirectoryViewModel()
        {
            UiDirectories = new ObservableCollection<DirectoryInfoModel>();
            _processedDirectories = new ConcurrentDictionary<string, string>();

            _directorySearchOptions = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true
            };

            _fileSearchOptions = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = false
            };

            _semaphore = new SemaphoreSlim(1, 5);

            PopulateDrives();
        }

        private ObservableCollection<DirectoryInfoModel> _uiDirectories;
        public ObservableCollection<DirectoryInfoModel> UiDirectories
        {
            get { return _uiDirectories; }
            set
            {
                SetProperty(ref _uiDirectories, value);
            }
        }

        private string _searchPath;
        public string SearchPath
        {
            get { return _searchPath; }
            set
            {
                _searchPath = value;
                SetProperty(ref _searchPath, value);
            }
        }

        private ObservableCollection<string> _drives;
        public ObservableCollection<string> Drives
        {
            get { return _drives; }
            set
            {
                _drives = value;
                SetProperty(ref _drives, value);
                (_selectionChangedCommand as ICanExecuteChangeable)?.RaiseCanExecuteChanged();
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
                    SetProperty(ref _selectedDrive, value);
                    (_selectionChangedCommand as ICanExecuteChangeable)?.RaiseCanExecuteChanged();
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

                    SetProperty(ref _isSearching, value);
                    (_pauseCommand as ICanExecuteChangeable)?.RaiseCanExecuteChanged();
                    (_resumeCommand as ICanExecuteChangeable)?.RaiseCanExecuteChanged();
                    (_selectionChangedCommand as ICanExecuteChangeable)?.RaiseCanExecuteChanged();
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

                    SetProperty(ref _isPaused, value);
                    (_pauseCommand as ICanExecuteChangeable)?.RaiseCanExecuteChanged();
                }
            }
        }

        private ICommand _selectionChangedCommand;
        public ICommand SelectionChangedCommand
        {
            get
            {
                if (_selectionChangedCommand == null)
                {
                    _selectionChangedCommand = new DelegateCommand(async () => await InitializeSearchAsync(), CanSearch);
                }
                return _selectionChangedCommand;
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
                    _resumeCommand = new DelegateCommand(async () => await ResumeSearchAsync(), CanResumeSearch);
                }
                return _resumeCommand;
            }
        }

        private bool CanSearch()
        {
            return Drives != null && Drives.Count > 0;
        }

        private bool CanPauseSearch()
        {
            return IsSearching && !_isPaused;
        }

        private bool CanResumeSearch()
        {
            return !IsSearching && _isPaused;
        }

        private void PopulateDrives()
        {
            var drives = DriveInfo.GetDrives();
            Drives = new ObservableCollection<string>(drives.Select(drive => drive.Name));
        }

        private async Task SearchAsync()
        {
            IsSearching = true;
            IsPaused = false;

            try
            {
                await Task.Run(ProcessDirectoriesAsync);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await ShowModalAsync("Error", "An error occurred: " + ex.Message);
            }
            finally
            {
                IsSearching = false;
            }
        }

        private async Task ProcessDirectoriesAsync()
        {
            var tasks = new List<Task>();
            var directories = Directory.EnumerateDirectories(SelectedDrive, "*", _directorySearchOptions)
                                       .Where((directory) => !_processedDirectories.ContainsKey(directory));

            foreach (var directory in directories)
            {
                if (_searchCancellationTokenSource.Token.IsCancellationRequested)
                {
                    break;
                }

                await _semaphore.WaitAsync();
                tasks.Add(Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        if (_searchCancellationTokenSource.Token.IsCancellationRequested)
                        {
                            return;
                        }

                        await GetFilesInDirectoryAsync(directory);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        private async Task GetFilesInDirectoryAsync(string directory)
        {
            long totalSize = 0;
            int fileCount = 0;

            var filePaths = Directory.EnumerateFiles(directory, "*", _fileSearchOptions);
            
            foreach (var filePath in filePaths)
            {
                if (_searchCancellationTokenSource.Token.IsCancellationRequested)
                {
                    return;
                }

                var fileInfo = new FileInfo(filePath);
                if (IsFileSizeLargerThanMegabytes(10, fileInfo))
                {
                    Interlocked.Increment(ref fileCount);
                    Interlocked.Add(ref totalSize, fileInfo.Length);
                }
            }

            _processedDirectories.TryAdd(directory, directory);

            if (fileCount == 0)
            {
                return;
            }

            var directoryModel = new DirectoryInfoModel
            {
                DirectoryPath = directory,
                FileCount = fileCount,
                TotalSize = totalSize
            };

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                UiDirectories.Add(directoryModel);
            });
        }

        private async Task InitializeSearchAsync()
        {
            _searchCancellationTokenSource?.Cancel();

            if (_searchTask != null && !_searchTask.IsCompleted)
            {
                await _searchTask;
            }

            ResetSearch();

            await Application.Current.Dispatcher.InvokeAsync(UiDirectories.Clear);

            _searchTask = SearchAsync();

            await _searchTask;
        }

        private void ResetSearch()
        {
            UiDirectories.Clear();
            _processedDirectories.Clear();

            ResethCancellationToken();
        }

        private void ResethCancellationToken()
        {
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource?.Dispose();
            _searchCancellationTokenSource = new CancellationTokenSource();
        }

        private void PauseSearch()
        {
            IsPaused = true;
            _searchCancellationTokenSource?.Cancel();
        }

        private async Task ResumeSearchAsync()
        {
            ResethCancellationToken();
            await SearchAsync();
        }

        private async Task ShowModalAsync(string title, string message)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
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

        private bool IsFileSizeLargerThanMegabytes(int mg, FileInfo file)
        {
            return file.Length > mg * 1024 * 1024;
        }

        public void Dispose()
        {
            _searchCancellationTokenSource?.Dispose();
            _semaphore?.Release();
            _semaphore?.Dispose();
        }
    }
}
