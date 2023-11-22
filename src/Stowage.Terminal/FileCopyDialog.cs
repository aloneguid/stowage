using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using Terminal.Gui;

namespace Stowage.Terminal {
    class FileCopyDialog {

        private const int DefaultCopyBufferSize = 81920;

        private readonly View _parent;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly FSView _from;
        private readonly FSView _to;

        private readonly Dialog _dialog;
        private readonly ProgressBar _progressTotal;
        private readonly ProgressBar _progressCurrent;
        private readonly Label _labelTotal;
        private readonly Label _labelCurrent;
        private long _copiedTotal = 0;
        private long _sizeTotal = 0;

        public FileCopyDialog(View parent, FSView from, FSView to) {
            _parent = parent;
            _from = from;
            _to = to;

            _parent.GetCurrentWidth(out int width);
            var cancel = new Button("Cancel");
            cancel.Clicked += () => {
                _cts.Cancel();
            };

            _dialog = new Dialog("Copy files", width / 2, 10, cancel);

            var lbl = new Label("  total: ") { X = 0, Y = 0 };
            _dialog.Add(lbl);

            _progressTotal = new ProgressBar() {
                X = Pos.Right(lbl),
                Y = 0,
                Width = Dim.Fill(),
                Height = 1
            };
            _dialog.Add(_progressTotal);

            _labelTotal = new Label("0 / 0") { X = 0, Y = Pos.Bottom(lbl) };
            _dialog.Add(_labelTotal);

            lbl = new Label("current: ") { X = 0, Y = Pos.Bottom(_labelTotal) };
            _dialog.Add(lbl);
            _progressCurrent = new ProgressBar() {
                X = Pos.Right(lbl),
                Y = Pos.Bottom(_progressTotal),
                Width = Dim.Fill(),
                Height = 1
            };
            _dialog.Add(_progressCurrent);

            _labelCurrent = new Label("0 / 0") { X = 0, Y = Pos.Bottom(lbl) };
            _dialog.Add(_labelCurrent);
        }

        public void Start() {

            RunCopy();

            Application.Run(_dialog);
        }

        private async Task<IReadOnlyCollection<IOEntry>> Explode(IFileStorage fs, IOEntry entry) {
            var result = new List<IOEntry>();

            if(entry.Path.IsFile) {
                result.Add(entry);
            } else {
                IReadOnlyCollection<IOEntry> entries = await fs.Ls(entry.Path, true, _cts.Token);
                result.AddRange(entries.Where(e => e.Path.IsFile));
            }

            return result;
        }

        private async Task Copy(IFileStorage fsFrom, IFileStorage fsTo, IOPath pathFrom, IOPath pathTo) {
            _progressCurrent.Fraction = 0.0f;

            await using(Stream? streamFrom = await fsFrom.OpenRead(pathFrom, _cts.Token)) {
                await using(Stream streamTo = await fsTo.OpenWrite(pathTo, _cts.Token)) {
                    if(streamFrom == null || streamTo == null)
                        return;

                    byte[] buffer = ArrayPool<byte>.Shared.Rent(DefaultCopyBufferSize);
                    try {
                        int bytesRead;
                        while((bytesRead = await streamFrom.ReadAsync(new Memory<byte>(buffer), _cts.Token)) != 0) {
                            await streamTo.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), _cts.Token);
                            float fraction = streamFrom.Position / (float)streamFrom.Length;

                            Application.MainLoop.Invoke(() => {
                                _progressCurrent.Fraction = fraction;
                                _copiedTotal += bytesRead;
                                _labelCurrent.Text = $"{streamFrom.Position.Bytes()} / {streamFrom.Length.Bytes()}";
                                _labelTotal.Text = $"{_copiedTotal.Bytes()} / {_sizeTotal.Bytes()}";
                            });
                        }

                    } finally {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
            }
        }

        private void RunCopy() {
            _progressCurrent.Fraction = 0.0f;
            _progressTotal.Fraction = 0.0f;

            Task.Run(async () => {
                Exception? ex = null;
                try {
                    IReadOnlyCollection<IOEntry> sourceEntries = await Explode(_from.Fs, _from.SelectedEntry!);
                    _sizeTotal = sourceEntries.Sum(e => e.Size!.Value);

                    int i = 0;
                    foreach(IOEntry entry in sourceEntries) {

                        await Copy(_from.Fs, _to.Fs, entry.Path, _to.CurrentPath.Combine(entry.Path.Name));

                        Application.MainLoop.Invoke(() => {
                            _progressTotal.Fraction = i++ * 100.0f / sourceEntries.Count;
                        });
                    }
                } catch(Exception ex1) {
                    ex = ex1;
                }   

                Application.MainLoop.Invoke(() => {
                    if(ex != null) {
                        MessageBox.ErrorQuery(60, 10, "Error", ex.ToString(), "Ok");
                    }
                    Application.RequestStop();
                });
            });
        }
    }
}
