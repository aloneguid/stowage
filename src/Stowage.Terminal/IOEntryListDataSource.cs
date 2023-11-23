using System.Collections;
using Humanizer;
using Terminal.Gui;

namespace Stowage.Terminal {
    class IOEntryListDataSource : IListDataSource {

        private readonly List<IOEntry> _entries = new List<IOEntry>();
        public const int SizeColLength = 10;
        public const int ModColLength = 11;

        public IOEntryListDataSource() {

        }

        public void Rebind(IEnumerable<IOEntry> entries, bool isRoot) {
            _entries.Clear();
            _entries.AddRange(entries.OrderBy(e => e.Path.IsFolder ? 0 : 1).ThenBy(e => e.Name));
            if(!isRoot) {
                _entries.Insert(0, new IOEntry("../"));
            }
        }

        public void RemoveAt(int idx) {
            _entries.RemoveAt(idx);
        }

        public int Count => _entries.Count;

        // https://github.com/gui-cs/Terminal.Gui/blob/3a502cae3c70f4264dcb15d089a29e7c580d3430/Terminal.Gui/Views/ListView.cs#L841
        public int Length => _entries.Max(e => e.Name.Length);

        public bool IsMarked(int item) => false;

        private static string FormatSize(long? size) {
            if(size == null)
                return "";

            Humanizer.Bytes.ByteSize h = size.Value.Bytes();
            return string.Format("{0:0.##}{1}", h.LargestWholeNumberValue, h.LargestWholeNumberSymbol.Substring(0, 1));
        }   

        private static string FormatMod(DateTimeOffset? mod) {
            if(mod == null)
                return "";

            // we want this to be shortest possible
            return mod.Value.LocalDateTime.ToShortDateString();
        }

        // https://github.com/gui-cs/Terminal.Gui/blob/3a502cae3c70f4264dcb15d089a29e7c580d3430/Terminal.Gui/Views/ListView.cs#L878
        public void Render(ListView container, ConsoleDriver driver,
            bool selected, int item, int col, int line, int width, int start = 0) {
            Rect savedClip = container.ClipToBounds();

            IOEntry entry = _entries[item];
            bool isFolder = entry.Path.IsFolder;
            string label = entry.Name.Trim('/');

            int nameColLength = width - SizeColLength - ModColLength;

            // type marker
            driver.AddStr(isFolder ? "/ " : "  ");
            nameColLength -= 2;

            // the rest of name label
            NStack.ustring nameDisplay = TextFormatter.ClipAndJustify(label, nameColLength - 1, TextAlignment.Left);
            driver.AddStr(nameDisplay);
            nameColLength -= TextFormatter.GetTextWidth(nameDisplay);
            while(nameColLength-- > 1) {
                driver.AddRune(' ');
            }
            driver.AddRune(driver.VLine);

            // size
            NStack.ustring sizeDisplay = TextFormatter.ClipAndJustify(
                FormatSize(entry.Size),
                SizeColLength - 1, TextAlignment.Left);
            int colLength = SizeColLength - TextFormatter.GetTextWidth(sizeDisplay);
            while(colLength-- > 1) {
                driver.AddRune(' ');
            }
            driver.AddStr(sizeDisplay);
            driver.AddRune(driver.VLine);

            // mod time
            NStack.ustring modDisplay = TextFormatter.ClipAndJustify(
                FormatMod(entry.LastModificationTime),
                ModColLength - 1, TextAlignment.Left);
            driver.AddStr(modDisplay);
            colLength = ModColLength - TextFormatter.GetTextWidth(modDisplay);
            while(colLength-- > 1) {
                driver.AddRune(' ');
            }

            driver.Clip = savedClip;

        }
        public void SetMark(int item, bool value) {

        }
        public IList ToList() => _entries;
    }
}
