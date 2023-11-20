using Humanizer;
using Terminal.Gui;

namespace Stowage.Terminal {
    class FSView : View {
        private readonly IFileStorage _fs;
        private IOPath _currentPath;
        private readonly Label _pathLabel;
        private readonly ListView _entryList = new ListView();
        private IOEntry? _activeEntry = null;

        public event Action<IFileStorage, IOEntry> SelectedEntryChanged;

        public FSView(IFileStorage fs, IOPath? startPath = null) {
            _fs = fs;
            _currentPath = startPath ?? IOPath.Root;
            _pathLabel = new Label() { X = 0, Y = 0 };

            Add(new Label("Name") { X = 2, Y = 1, ColorScheme = Colors.Menu });
            Add(new Label("Size") {
                X = Pos.AnchorEnd() - IOEntryListDataSource.SizeColLength - IOEntryListDataSource.ModColLength,
                Y = 1,
                ColorScheme = Colors.Menu });
            Add(new Label("Mod") {
                X = Pos.AnchorEnd() - IOEntryListDataSource.ModColLength,
                Y = 1,
                ColorScheme = Colors.Menu });

            _entryList.X = 0;
            _entryList.Y = 2;
            _entryList.Width = Dim.Fill();
            _entryList.Height = Dim.Fill() - 1;
            Ls();

            Add(_pathLabel);
            Add(_entryList);

            _entryList.OpenSelectedItem += _entryList_OpenSelectedItem;
            _entryList.SelectedItemChanged += _entryList_SelectedItemChanged;
        }

        private void _entryList_SelectedItemChanged(ListViewItemEventArgs args) {
            if(args.Value is not IOEntry entry)
                return;

            _activeEntry = entry;

            SelectedEntryChanged?.Invoke(_fs, entry);
        }

        private void _entryList_OpenSelectedItem(ListViewItemEventArgs args) {
            if(args.Value is not IOEntry entry)
                return;

            if(entry.Path.IsFolder) {
                // enter the folder
                if(entry.Name == "../")
                    _currentPath = _currentPath.Parent;
                else
                    _currentPath = entry.Path;
                Ls();
            } else {

            }
        }

        private void Ls() {
            Task.Run(async () => {
                try {
                    _pathLabel.Text = _currentPath.Full;
                    IReadOnlyCollection<IOEntry> entries = await _fs.Ls(_currentPath);

                    Application.MainLoop.Invoke(() => {
                        _entryList.Source = new IOEntryListDataSource(entries, _currentPath.IsRootPath);
                    });
                } catch(Exception ex) {
                    MessageBox.ErrorQuery(60, 10, "Error", ex.ToString(), "Ok");
                }
            });
        }

        private void AddProperties(View view, int firstColWidth, params string[] args) {
            Label? last = null;

            for(int i = 0; i < args.Length; i += 2) {
                string key = args[i] + ": ";
                string value = args[i + 1];

                if(string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value)) {
                    continue;
                }

                // key
                if(last == null) {
                    last = new Label(key) {
                        X = 0,
                        Y = 0,
                        Width = firstColWidth,
                        Height = 1,
                        TextAlignment = TextAlignment.Right
                    };
                } else {
                    last = new Label(key) {
                        X = 0,
                        Y = Pos.Bottom(last),
                        Width = Dim.Width(last),
                        Height = 1,
                        TextAlignment = TextAlignment.Right
                    };
                }
                view.Add(last);

                // value
                var valueLabel = new Label(value) {
                    X = Pos.Right(last) + 1,
                    Y = Pos.Top(last),
                    Width = Dim.Fill(),
                    Height = Dim.Height(last),
                    TextAlignment = TextAlignment.Left
                };
                view.Add(valueLabel);
            }
        }


        public void ShowEntryDetails() {

            if(_activeEntry == null)
                return;

            var close = new Button("Close", true);
            close.Clicked += () => {
                Application.RequestStop();
            };

            var dialog = new Dialog("Entry Details", 70, 20, close);
            var props = new List<string>{
                "name", _activeEntry.Name,
                "size", _activeEntry.Size == null ? "" : _activeEntry.Size.Value.Bytes().Humanize(),
                "created", _activeEntry.CreatedTime == null ? "" : _activeEntry.CreatedTime.Value.ToString(),
                "modified", _activeEntry.LastModificationTime == null ? "" : _activeEntry.LastModificationTime.Value.ToString(),
                "MD-5", _activeEntry.MD5
            };

            if(_activeEntry.Properties != null)
                props.AddRange(_activeEntry.Properties.SelectMany(p => new[] { p.Key, p.Value?.ToString() }));

            // basic properties
            AddProperties(dialog, 20, props.ToArray());

            Application.Run(dialog);
        }

        public void ViewEntry() {
            if(_activeEntry == null)
                return;

            var editor = new TextFileEditorWindow(_fs, _activeEntry);
            Application.Run(editor);


            //var viewer = new Dialog(_activeEntry.Name) {
            //    X = 2,
            //    Y = 2,
            //    Width = Dim.Fill() - 4,
            //    Height = Dim.Fill() - 4
            //};
            //var label = new Label("Loading...") {
            //    X = 0,
            //    Y = 0,
            //    Width = Dim.Fill(),
            //    Height = Dim.Fill()
            //};
            //viewer.Add(label);

            //Task.Run(async () => {
            //    try {
            //        string? content = await _fs.ReadText(_activeEntry.Path);
            //        Application.MainLoop.Invoke(() => {
            //            label.Text = content;
            //        });
            //    } catch(Exception ex) {
            //        Console.WriteLine(ex.ToString());
            //    }
            //});

            //Application.Run(viewer);
        }
    }
}
