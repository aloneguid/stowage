using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace Stowage.Terminal {
    class FileCopyDialog {
        private readonly View _parent;
        private readonly FSView _from;
        private readonly FSView _to;

        private readonly Dialog _dialog;
        private readonly ProgressBar _progressTotal;
        private readonly ProgressBar _progressCurrent;

        public FileCopyDialog(View parent, FSView from, FSView to) {
            _parent = parent;
            _from = from;
            _to = to;

            _parent.GetCurrentWidth(out int width);
            _dialog = new Dialog("Copy files", width / 2, 10);

            var lbl = new Label("  total: ") { X = 0, Y = 0 };
            _dialog.Add(lbl);

            _progressTotal = new ProgressBar() {
                X = Pos.Right(lbl),
                Y = 0,
                Width = Dim.Fill(),
                Height = 1
            };
            _dialog.Add(_progressTotal);

            lbl = new Label("current: ") { X = 0, Y = Pos.Bottom(lbl) };
            _dialog.Add(lbl);
            _progressCurrent = new ProgressBar() {
                X = Pos.Right(lbl),
                Y = Pos.Bottom(_progressTotal),
                Width = Dim.Fill(),
                Height = 1
            };
            _dialog.Add(_progressCurrent);
        }

        public void Start() {

            _progressCurrent.Fraction = 0.2f;
            _progressTotal.Fraction = 0.5f;

            Application.Run(_dialog);
        }
    }
}
