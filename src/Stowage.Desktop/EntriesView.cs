using Grey;
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

        public EntriesView(IFileStorage fileStorage) {
            _fileStorage = fileStorage;
        }

        async Task RefreshFolderAsync() {
            _listError = null;
            _isListLoading = true;
            try {
                _entries = (await _fileStorage.Ls(_currentPath))
                    .OrderBy(e => e.Path.IsFolder ? 0 : 1)
                    .ThenBy(e => e.Name)
                    .ToList();

                //await Task.Delay(TimeSpan.FromSeconds(2));
            } catch(Exception ex) {
                _listError = ex.Message;
            } finally {
                _isListLoading = false;
            }
        }

        public void RefreshFolder() {
            RefreshFolderAsync();
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
            using(var t = new Table("entries", Columns)) {
                if(t) {
                    for(int i = 0; i < _entries.Count; i++) {
                        IOEntry entry = _entries[i];
                        t.BeginRow();

                        t.BeginCol();
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

                        t.BeginCol();
                        Label(entry.LastModificationTime?.ToString() ?? "");

                        t.BeginCol();
                        Label(entry.Size?.ToFileSizeUiString() ?? "");
                    }
                }
            }
        }

        private void RenderSelectedObject() {
            if(Button($"{Icon.Close} close", Emphasis.Primary)) {
                _selectedEntry = null;
            }

            _selectedEntry?.RenderFrame();
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
                }
            }
        }
    }
}
