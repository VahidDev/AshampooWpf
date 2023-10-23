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
    public class SearchDirectoryViewModel : ViewModelBase

    {
        private string[] _allDirectoryPaths;
        private IDictionary<string, IEnumerable<FileInfo>> _directoryFiles;
        private CancellationTokenSource _searchCancellationTokenSource;
        private int _lastProcessedDirectoryIndex;

        public SearchDirectoryViewModel()
        {
            UiDirectories = new ObservableCollection<DirectoryInfoModel>();
            PopulateDrives();
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

        private ObservableCollection<string> _drives;
        public ObservableCollection<string> Drives
        {
            get { return _drives; }
            set
            {
                _drives = value;
                OnPropertyChanged(nameof(Drives));
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
                    OnPropertyChanged(nameof(SelectedDrive));
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

                    OnPropertyChanged(nameof(IsSearching));
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

                    OnPropertyChanged(nameof(IsPaused));
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
                    _selectionChangedCommand = new DelegateCommand(async () => await SearchAsync(true), CanSearch);
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
                    _resumeCommand = new DelegateCommand(async () => await ResumeSearch(), CanResumeSearch);
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
                        RecurseSubdirectories = true
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
            enumerationOptions.RecurseSubdirectories = false;

            for (int i = _lastProcessedDirectoryIndex; i < _allDirectoryPaths.Length; i++)
            {
                var directoryPath = _allDirectoryPaths[i];

                if (IsCancellationRequested())
                {
                    return;
                }

                var directoryInfo = new DirectoryInfo(directoryPath);

                if (!_directoryFiles.ContainsKey(directoryPath))
                {
                    var filesToAddToCollection = directoryInfo.EnumerateFiles("*", enumerationOptions)
                                                              .OrderBy(f => f.Name);

                    _directoryFiles.Add(directoryPath, filesToAddToCollection);
                }

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
                                          .Order()
                                          .ToArray();

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
