using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Humanizer;
using Terminal.Gui;

namespace Stowage.Terminal {

    /// <summary>
    /// Minimalistic text file viewer/editor.
    /// Modeled after https://github.com/gui-cs/Terminal.Gui/blob/develop/UICatalog/Scenarios/Editor.cs
    /// </summary>
    class TextFileEditorWindow : Window {
        private readonly ScrollBarView _scrollBar;
        private readonly TextView _textView;
        private readonly IFileStorage _fs;
        private readonly IOEntry _entry;

        // status bar indicators
        private readonly StatusBar _statusBar;
        private readonly StatusItem _statusSize = new StatusItem(Key.Null, "", null);
        private readonly StatusItem _statusCursorPos = new StatusItem(Key.Null, "", null);

        public TextFileEditorWindow(IFileStorage fs, IOEntry entry) : base("Quick Edit", 0) {
            _fs = fs;
            _entry = entry;

            X = 0;
            Y = 0;
            Width = Dim.Fill();
            Height = Dim.Fill();

            _textView = new TextView() {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1
            };

            _textView.UnwrappedCursorPosition += (e) => {
                _statusCursorPos.Title = $"Ln {_textView.CursorPosition.Y + 1}, Col {_textView.CursorPosition.X + 1}";
                if(_statusBar != null) {
                    _statusBar.SetNeedsDisplay();
                }
            };

            _statusBar = new StatusBar(new StatusItem[] {
                new StatusItem(Key.F2, "~F2~ Save", SaveContent),
                _statusSize,
                _statusCursorPos
            });

            Add(_textView);
            Add(_statusBar);

            _scrollBar = new ScrollBarView(_textView, true);
            _textView.DrawContent += (e) => {
                _scrollBar.Size = _textView.Lines;
                _scrollBar.Position = _textView.TopRow;
                if(_scrollBar.OtherScrollBarView != null) {
                    // + 1 is needed to show the cursor at the end of a line.
                    _scrollBar.OtherScrollBarView.Size = _textView.Maxlength + 1;
                    _scrollBar.OtherScrollBarView.Position = _textView.LeftColumn;
                }
                _scrollBar.LayoutSubviews();
                _scrollBar.Refresh();
            };

            LoadContent();
        }
        private void LoadContent() {

            string sizeDisplay = _entry.Size.HasValue ? _entry.Size.Value.Bytes().ToString() : string.Empty; 
            _textView.Text = $"Loading '{_entry.Name}' ({sizeDisplay}) ...";
            _statusSize.Title = sizeDisplay;

            Task.Run(async () => {
                try {
                    string? content = await _fs.ReadText(_entry.Path);
                    Application.MainLoop.Invoke(() => {
                        _textView.Text = content;
                    });
                } catch(Exception ex) {
                    _textView.Text = $"Error loading '{_entry.Name}': {ex.Message}";
                }
            });

        }
        private void SaveContent() {
            if(0 == MessageBox.Query("Overwrite", "Are you sure you want to overwrite this file?", "Yes", "No")) {
                Task.Run(async () => {
                    try {
                        await _fs.WriteText(_entry.Path, _textView.Text.ToString());
                        Application.MainLoop.Invoke(() => {
                            MessageBox.Query("Saved", "File saved.", "Ok");
                        });
                    } catch(Exception ex) {
                        Application.MainLoop.Invoke(() => {
                            MessageBox.ErrorQuery("Error", $"Error saving file: {ex.Message}", "Ok");
                        });
                    }
                });
            }
        }
    }
}
