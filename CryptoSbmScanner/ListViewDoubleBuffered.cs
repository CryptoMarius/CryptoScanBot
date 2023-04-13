using System.Runtime.InteropServices;

namespace CryptoSbmScanner;

public class ListViewDoubleBuffered : ListView
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

    private const int WM_CHANGEUISTATE = 0x127;
    private const int UIS_SET = 1;
    private const int UISF_HIDEFOCUS = 0x1;

    public ListViewDoubleBuffered()
    {
        DoubleBuffered = true;
        View = View.Details;
        FullRowSelect = false;

        // removes the ugly dotted line around focused item
        SendMessage(this.Handle, WM_CHANGEUISTATE, MakeLong(UIS_SET, UISF_HIDEFOCUS), 0);
    }

    private static int MakeLong(int wLow, int wHigh)
    {
        int low = (int)IntLoWord(wLow);
        short high = IntLoWord(wHigh);
        int product = 0x10000 * (int)high;
        int mkLong = (int)(low | product);
        return mkLong;
    }

    private static short IntLoWord(int word) => (short)(word & short.MaxValue);


    [StructLayout(LayoutKind.Sequential)]
    public struct LVCOLUMN
    {
        public Int32 mask;
        public Int32 cx;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pszText;
        public IntPtr hbm;
        public Int32 cchTextMax;
        public Int32 fmt;
        public Int32 iSubItem;
        public Int32 iImage;
        public Int32 iOrder;
    }

    //const Int32 HDI_WIDTH = 0x0001;
    //const Int32 HDI_HEIGHT = HDI_WIDTH;
    //const Int32 HDI_TEXT = 0x0002;
    const Int32 HDI_FORMAT = 0x0004;
    //const Int32 HDI_LPARAM = 0x0008;
    //const Int32 HDI_BITMAP = 0x0010;
    //const Int32 HDI_IMAGE = 0x0020;
    //const Int32 HDI_DI_SETITEM = 0x0040;
    //const Int32 HDI_ORDER = 0x0080;
    //const Int32 HDI_FILTER = 0x0100;

    const Int32 HDF_LEFT = 0x0000;
    //const Int32 HDF_RIGHT = 0x0001;
    //const Int32 HDF_CENTER = 0x0002;
    //const Int32 HDF_JUSTIFYMASK = 0x0003;
    //const Int32 HDF_RTLREADING = 0x0004;
    //const Int32 HDF_OWNERDRAW = 0x8000;
    //const Int32 HDF_STRING = 0x4000;
    //const Int32 HDF_BITMAP = 0x2000;
    const Int32 HDF_BITMAP_ON_RIGHT = 0x1000;
    //const Int32 HDF_IMAGE = 0x0800;
    const Int32 HDF_SORTUP = 0x0400;
    const Int32 HDF_SORTDOWN = 0x0200;

    const Int32 LVM_FIRST = 0x1000;         // List messages
    const Int32 LVM_GETHEADER = LVM_FIRST + 31;
    const Int32 HDM_FIRST = 0x1200;         // Header messages
    //const Int32 HDM_SETIMAGELIST = HDM_FIRST + 8;
    //const Int32 HDM_GETIMAGELIST = HDM_FIRST + 9;
    const Int32 HDM_GETITEM = HDM_FIRST + 11;
    const Int32 HDM_SETITEM = HDM_FIRST + 12;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SendMessage")]
    private static extern IntPtr SendMessageLVCOLUMN(IntPtr hWnd, Int32 Msg, IntPtr wParam, ref LVCOLUMN lPLVCOLUMN);

    //This method used to set arrow icon
    public void SetSortIcon(int columnIndex, SortOrder order) //this ListView listView, 
    {
        IntPtr columnHeader = SendMessage(this.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);

        for (int columnNumber = 0; columnNumber <= this.Columns.Count - 1; columnNumber++)
        {
            IntPtr columnPtr = new(columnNumber);
            LVCOLUMN lvColumn = new()
            {
                mask = HDI_FORMAT
            };

            SendMessageLVCOLUMN(columnHeader, HDM_GETITEM, columnPtr, ref lvColumn);

            if (!(order == SortOrder.None) && columnNumber == columnIndex)
            {
                switch (order)
                {
                    case System.Windows.Forms.SortOrder.Ascending:
                        lvColumn.fmt &= ~HDF_SORTDOWN;
                        lvColumn.fmt |= HDF_SORTUP;
                        break;
                    case System.Windows.Forms.SortOrder.Descending:
                        lvColumn.fmt &= ~HDF_SORTUP;
                        lvColumn.fmt |= HDF_SORTDOWN;
                        break;
                }
                lvColumn.fmt |= (HDF_LEFT | HDF_BITMAP_ON_RIGHT);
            }
            else
            {
                lvColumn.fmt &= ~HDF_SORTDOWN & ~HDF_SORTUP & ~HDF_BITMAP_ON_RIGHT;
            }

            SendMessageLVCOLUMN(columnHeader, HDM_SETITEM, columnPtr, ref lvColumn);
        }
    }
}

