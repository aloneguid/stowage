using Grey;
using NetBox.Performance;
using static Grey.App;

namespace Stowage.Desktop {
    class EntriesView {
        private readonly IFileStorage _fileStorage;
        private EntryView? _selectedEntry;
        private string? _listError;
        private bool _isListLoading = false;

        private static readonly string[] Columns = ["Name+", "LastMod", "Size"];

        private List<IOEntry> _entries = new List<IOEntry>();
        private IOPath _currentPath = IOPath.Root;
        private long _entriesLoadTimeMs;

        // status
        private int _totalCount;
        private string _statusTotalCount = "";
        private int _folderCount;
        private string _statusFolderCount = "";
        private int _fileCount;
        private string _statusFileCount = "";
        private long _totalSize;
        private string _statusTotalSize = "";
        private string _statusFiles = "";
        private string _statusLoadTime = "";

        public EntriesView(IFileStorage fileStorage) {
            _fileStorage = fileStorage;
        }

        async Task RefreshFolderAsync() {
            _listError = null;
            _isListLoading = true;
            using(var tm = new TimeMeasure()) {
                try {
                    _entries = (await _fileStorage.Ls(_currentPath))
                        .OrderBy(e => e.Path.IsFolder ? 0 : 1)
                        .ThenBy(e => e.Name)
                        .ToList();

                    _totalCount = _entries.Count;
                    _statusTotalCount = Icon.Functions + " " + _totalCount.ToString();
                    _folderCount = _entries.Count(e => e.Path.IsFolder);
                    _statusFolderCount = Icon.Folder + " " + _folderCount.ToString();
                    _fileCount = _entries.Count(e => !e.Path.IsFolder);
                    _statusFileCount = Icon.File_open + " " + _fileCount.ToString();
                    _totalSize = _entries.Where(e => !e.Path.IsFolder).Sum(e => e.Size ?? 0);
                    _statusTotalSize = Icon.Straighten + " " + _totalSize.ToFileSizeUiString();
                    _statusFiles = $"{Icon.File_open} {_fileCount} ({_totalSize.ToFileSizeUiString()})";

                    //await Task.Delay(TimeSpan.FromSeconds(2));
                } catch(Exception ex) {
                    _listError = ex.Message;
                } finally {
                    _isListLoading = false;
                    _entriesLoadTimeMs = tm.ElapsedMilliseconds;
                    _statusLoadTime = Icon.Timer + " " + _entriesLoadTimeMs + "ms";
                }
            }
        }

        public void RefreshFolder() {
            RefreshFolderAsync().Forget();
        }

        private void RenderToolbar() {
            if(Button(Icon.Arrow_upward)) {
                if(!_currentPath.IsRootPath) {
                    _currentPath = _currentPath.Parent;
                    RefreshFolder();
                }
            }
            SL();
            if(Button(Icon.Refresh)) {
                RefreshFolder();
            }

            SL();
            Label(_currentPath.ToString());
        }

        private void RenderEntriesTable() {
            Table("entries", Columns, (t) => {
                for(int i = 0; i < _entries.Count; i++) {
                    IOEntry entry = _entries[i];
                    t.BeginRow();

                    if(entry.Path.IsFolder) {
                        Label(Icon.Folder);
                        SL();

                        if(Hyperlink(entry.Name)) {
                            _currentPath = entry.Path;
                            RefreshFolder();
                        }
                    } else {
                        Label(Icon.File_open);
                        SL();
                        if(Hyperlink(entry.Name)) {
                            _selectedEntry = new EntryView(entry, _fileStorage);
                        }
                    }

                    t.NextColumn();
                    Label(entry.LastModificationTime?.ToString() ?? "");

                    t.NextColumn();
                    Label(entry.Size?.ToFileSizeUiString() ?? "");
                }
            }, 0, -44);
        }

        private void RenderSelectedObject() {
            if(Button($"{Icon.Close} close", Emphasis.Primary)) {
                _selectedEntry = null;
            }

            _selectedEntry?.RenderFrame();
        }

        private void RenderStatusBar() {

            Label(Icon.Blur_circular, Emphasis.Primary);

            if((_folderCount != 0 && _totalCount != _folderCount) || (_fileCount != 0 && _totalCount != _fileCount)) {
                SL(); Label("|", isEnabled: false);
                SL(); Label(_statusTotalCount, isEnabled: false);
            }

            // only show relevant status
            if(_folderCount > 0) {
                SL(); Label("|", isEnabled: false);
                SL(); Label(_statusFolderCount, isEnabled: false);
                TT("number of folders");
            }


            if(_fileCount > 0) {
                SL(); Label("|", isEnabled: false);
                SL(); Label(_statusFiles, isEnabled: false);
                TT("number of files and total size");
            }

            SL(); Label("|", isEnabled: false);
            SL(); Label(_statusLoadTime, isEnabled: false);
            TT("time it took to load entries");
        }

        public void RenderFrame() {

            if(_listError != null) {
                Label(_listError, Emphasis.Error);
            }

            if(_isListLoading) {
                SpinnerHboDots();
            } else {

                if(_selectedEntry != null) {
                    RenderSelectedObject();
                } else {
                    RenderToolbar();

                    RenderEntriesTable();

                    RenderStatusBar();
                }
            }
        }
    }
}
