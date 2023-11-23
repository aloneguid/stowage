using Humanizer;
using Terminal.Gui;

namespace Stowage.Terminal {
    class FSView : View {
        private readonly Label _pathLabel;
        private readonly ListView _entryList = new ListView();
        private readonly IOEntryListDataSource _entryListSource = new IOEntryListDataSource();

        public event Action<IFileStorage, IOEntry> SelectedEntryChanged;
        public FSView(IFileStorage fs, IOPath? startPath = null) {
            Fs = fs;
            CurrentPath = startPath ?? IOPath.Root;
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

            _entryList.Source = _entryListSource;
            _entryList.X = 0;
            _entryList.Y = 2;
            _entryList.Width = Dim.Fill();
            _entryList.Height = Dim.Fill() - 1;
            //_entryList.AllowsMultipleSelection = true;
            //_entryList.AllowsMarking = true;
            // vim-style navigation
            _entryList.AddKeyBinding(Key.j, Command.LineDown);
            _entryList.AddKeyBinding(Key.k, Command.LineUp);
            Ls();

            Add(_pathLabel);
            Add(_entryList);

            _entryList.OpenSelectedItem += _entryList_OpenSelectedItem;
            _entryList.SelectedItemChanged += _entryList_SelectedItemChanged;
            _entryList.KeyPress += _entryList_KeyPress;
        }

        private void _entryList_KeyPress(KeyEventEventArgs args) {

            switch(args.KeyEvent.Key) {
                case Key.DeleteChar:
                case Key.d:
                    args.Handled = true;
                    DeleteEntry();
                    break;
                case Key.Backspace:
                    args.Handled = true;
                    GoUp();
                    break;
            }
        }

        public IFileStorage Fs { get; init; }
        public IOEntry? SelectedEntry { get; private set; }
        public IOPath CurrentPath { get; private set; }

        private void _entryList_SelectedItemChanged(ListViewItemEventArgs args) {
            if(args.Value is not IOEntry entry)
                return;

            SelectedEntry = entry;

            SelectedEntryChanged?.Invoke(Fs, entry);
        }

        private void GoUp() {
            CurrentPath = CurrentPath.Parent;
            Ls();
        }

        private void _entryList_OpenSelectedItem(ListViewItemEventArgs args) {
            if(args.Value is not IOEntry entry)
                return;

            if(entry.Path.IsFolder) {
                // enter the folder
                if(entry.Name == "../") {
                    GoUp();
                } else {
                    CurrentPath = entry.Path;
                    Ls();
                }
            } else {
                // todo: open locally?
            }
        }

        public void Ls(bool keepSelection = false) {
            _pathLabel.Text = CurrentPath.Full;

            Task.Run(async () => {
                try {
                    IReadOnlyCollection<IOEntry> entries = await Fs.Ls(CurrentPath);

                    Application.MainLoop.Invoke(() => {
                        _entryListSource.Rebind(entries, CurrentPath.IsRootPath);
                        if(!keepSelection) {
                            _entryList.SelectedItem = 0;
                        }
                        _entryList.SetNeedsDisplay();
                    });
                } catch(Exception ex) {
                    Application.MainLoop.Invoke(() => {
                        MessageBox.ErrorQuery(60, 10, "Error", ex.ToString(), "Ok");
                    });
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

            if(SelectedEntry == null)
                return;

            var close = new Button("Close", true);
            close.Clicked += () => {
                Application.RequestStop();
            };

            var dialog = new Dialog("Entry Details", 70, 20, close);
            var props = new List<string>{
                "name", SelectedEntry.Name,
                "size", SelectedEntry.Size == null ? "" : SelectedEntry.Size.Value.Bytes().Humanize(),
                "created", SelectedEntry.CreatedTime == null ? "" : SelectedEntry.CreatedTime.Value.ToString(),
                "modified", SelectedEntry.LastModificationTime == null ? "" : SelectedEntry.LastModificationTime.Value.ToString(),
                "MD-5", SelectedEntry.MD5
            };

            if(SelectedEntry.Properties != null)
                props.AddRange(SelectedEntry.Properties.SelectMany(p => new[] { p.Key, p.Value?.ToString() }));

            // basic properties
            AddProperties(dialog, 20, props.ToArray());

            Application.Run(dialog);
        }

        public void ViewEntry() {
            if(SelectedEntry == null)
                return;

            var editor = new TextFileEditorWindow(Fs, SelectedEntry);
            Application.Run(editor);
        }

        public void DeleteEntry() {
            if(SelectedEntry == null)
                return;

            if(0 == MessageBox.Query("Delete", $"Delete '{SelectedEntry.Path.Full}'?", "Yes", "No")) {
                Task.Run(async () => {
                    try {
                        await Fs.Rm(SelectedEntry.Path);
                        int idx = _entryList.SelectedItem;
                        _entryListSource.RemoveAt(idx);
                        if(idx >= _entryListSource.Count)
                            _entryList.SelectedItem = idx - 1;
                        _entryList.SetNeedsDisplay();

                        //Ls();
                    } catch(Exception ex) {
                        MessageBox.ErrorQuery(60, 10, "Error", ex.ToString(), "Ok");
                    }
                });
            }
        }

    }
}
