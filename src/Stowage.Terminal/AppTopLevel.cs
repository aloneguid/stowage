using Humanizer;
using Terminal.Gui;

namespace Stowage.Terminal {
    /// <summary>
    /// Modeled after https://github.com/gui-cs/Terminal.Gui/blob/develop/UICatalog/UICatalog.cs
    /// </summary>
    class AppTopLevel : Toplevel {

        private readonly IFileStorage _fs;
        private readonly FSView _fsView1;
        private readonly FSView _fsView2;

        public AppTopLevel(IFileStorage fs) {
            _fs = fs;
            //ColorScheme = Colors.Base;

            //MenuBar = new MenuBar(new MenuBarItem[] {
            //    new MenuBarItem("_File", new MenuItem[] {
            //        new MenuItem("_Quit", "", () => Application.RequestStop()),
            //    }),
            //    new MenuBarItem("_Help", new MenuItem[] {
            //        new MenuItem("_About", "", () => MessageBox.Query(50, 7, "About", "Stowage.Terminal", "Ok")),
            //    }),
            //});
            //MenuBar.Visible = true;

            _fsView1 = new FSView(_fs) {
                X = 0,
                Y = 0,
                Width = Dim.Percent(50),
                Height = Dim.Fill() - 1
            };

            string cp = Environment.CurrentDirectory;

            IFileStorage fs2 = CreateLocalDiskStorage(out IOPath currentPath);
            _fsView2 = new FSView(fs2, currentPath) {
                X = Pos.Right(_fsView1) + 2,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };


            StatusBar = new StatusBar { Visible = true };
            StatusBar.Items = new StatusItem[] {
                new StatusItem(Key.F1, "~F1~ Info", () => {
                    GetFocusedView().ShowEntryDetails();
                }),
                new StatusItem(Key.F4, "~F4~ Edit", () => {
                    GetFocusedView().ViewEntry();
                }),
                new StatusItem(Key.F5, "~F5~ Copy", CopyFiles),
                new StatusItem(Key.R | Key.CtrlMask, "~Ctrl-R~ Rescan", () => {
                    GetFocusedView().Ls();
                }),
                new StatusItem(Key.Delete, "~Del~ Delete", () => {
                    GetFocusedView().DeleteEntry();
                }),
                new StatusItem(Key.F10, "~F10~ Quit", RequestStop)

            };

            AddEventHandlers(_fsView1);
            AddEventHandlers(_fsView2);

            // add in order

            Add(MenuBar);
            Add(_fsView1);
            Application.Top.Add(new LineView(global::Terminal.Gui.Graphs.Orientation.Vertical) {
                X = Pos.Right(_fsView1),
                Y = 1,
                Height = Dim.Fill()
            });
            Add(_fsView2);
            Add(StatusBar);
        }

        private void CopyFiles() {
            FSView from = GetFocusedView();
            FSView to = GetUnfocusedView();

            var dialog = new FileCopyDialog(this, from, to);
            dialog.Start();
        }

        private IFileStorage CreateLocalDiskStorage(out IOPath currnetPath) {
            currnetPath = IOPath.CurrenLocalDiskPathIfMappedToRoot;
            return Files.Of.EntireLocalDisk();
        }

        private void AddEventHandlers(FSView fSView) {
            fSView.SelectedEntryChanged += (fs, entry) => {
            };
        }

        private FSView GetFocusedView() {
            if(_fsView1.HasFocus)
                return _fsView1;

            return _fsView2;
        }

        private FSView GetUnfocusedView() {
            if(_fsView1.HasFocus)
                return _fsView2;

            return _fsView1;
        }




    }

}
