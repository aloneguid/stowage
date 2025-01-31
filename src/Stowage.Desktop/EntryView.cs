using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grey;
using static Grey.App;

namespace Stowage.Desktop {
    class EntryView {
        private readonly IOEntry _entry;
        private string _entryPathDisplay;
        private readonly IFileStorage _fileStorage;
        private bool _isLoading = false;
        private bool? _isTooBig;
        private StringBuilder? _textContent;
        private string? _loadingError;

        public EntryView(IOEntry entry, IFileStorage fileStorage) {
            _entry = entry;
            _entryPathDisplay = entry.Path.Full;
            _fileStorage = fileStorage;

            LoadContentAsync();
        }

        public void RenderFrame() {

            Input(ref _entryPathDisplay, "path", is_readonly: true);

            if(Accordion("Info")) {
                if(_entry.Size != null) {
                    Label(_entry.Size.Value.ToFileSizeUiString());
                }
                if(_entry.MD5 != null) {
                    Label(_entry.MD5);
                }
                if(_entry.Metadata.Count > 0) {
                    Label("Metadata");
                }
                if(_entry.Properties.Count > 0) {
                    Label("Properties");
                    using(var tbl = new Table("##props", new[] { "name", "value+" }, outerHeight: 400)) {
                        foreach(KeyValuePair<string, object> kvp in _entry.Properties) {
                            tbl.BeginRow();

                            tbl.BeginCol();
                            Label(kvp.Key);

                            tbl.BeginCol();
                            Label(kvp.Value?.ToString() ?? "");
                        }
                    }
                }
            }

            if(_isLoading) {
                SpinnerHboDots();
            } else {
                if(_isTooBig != null && _isTooBig.Value) {
                    Label("object too big to display");
                } else {
                    if(_textContent != null) {
                        InputMultiline("##content", _textContent, useFixedFont: true);
                    }
                }
            }
        }

        private async Task LoadContentAsync() {
            _isTooBig = _entry.Size == null || _entry.Size > 1024 * 1024;
            _isLoading = true;
            try {
                string? text = await _fileStorage.ReadText(_entry.Path);
                if(text != null) {
                    _textContent = new StringBuilder(text, text.Length * 2);
                }
            } catch(Exception ex) {
                _loadingError = ex.Message;
            } finally {
                _isLoading = false;
            } 
        }
    }
}
