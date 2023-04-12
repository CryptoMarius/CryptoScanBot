using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CryptoSbmScanner
{

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
                IntPtr columnPtr = new IntPtr(columnNumber);
                LVCOLUMN lvColumn = new LVCOLUMN()
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



    /// <summary>
    /// This class is an implementation of the 'IComparer' interface.
    /// </summary>
    public class ListViewColumnSorter : IComparer
    {

        /// <summary>
        /// Specifies the order in which to sort (i.e. 'Ascending').
        /// </summary>
        public SortOrder SortOrder = SortOrder.Descending;

        /// <summary>
        /// Case insensitive comparer object
        /// </summary>
        private readonly CaseInsensitiveComparer ObjectCompare;

        /// <summary>
        /// Class constructor. Initializes various elements
        /// </summary>
        public ListViewColumnSorter()
        {
            // Initialize the CaseInsensitiveComparer object
            ObjectCompare = new CaseInsensitiveComparer();
        }

        /// <summary>
        /// This method is inherited from the IComparer interface. It compares the two objects passed using a case insensitive comparison.
        /// </summary>
        /// <param name="x">First object to be compared</param>
        /// <param name="y">Second object to be compared</param>
        /// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
        public int Compare(object x, object y)
        {
            // Cast the objects to be compared to ListViewItem objects
            ListViewItem listviewA = (ListViewItem)x;
            ListViewItem listviewB = (ListViewItem)y;

            CryptoSignal signalA = (CryptoSignal)listviewA.Tag;
            CryptoSignal signalB = (CryptoSignal)listviewB.Tag;

            int compareResult = 0;
            if (SortColumn > 0)
            {
                switch (SortColumn)
                {
                    case 2:
                        compareResult = ObjectCompare.Compare(signalA.Interval.IntervalPeriod, signalB.Interval.IntervalPeriod);
                        break;
                    case 6:
                        compareResult = ObjectCompare.Compare(signalA.Price, signalB.Price);
                        break;
                    case 7:
                        compareResult = ObjectCompare.Compare(signalA.Volume, signalB.Volume);
                        break;
                    case 9:
                        compareResult = ObjectCompare.Compare(signalA.TrendPercentage, signalB.TrendPercentage);
                        break;
                    case 10:
                        compareResult = ObjectCompare.Compare(signalA.Last24Hours, signalB.Last24Hours);
                        break;
                    case 11:
                        compareResult = ObjectCompare.Compare(signalA.BollingerBandsPercentage, signalB.BollingerBandsPercentage);
                        break;
                    case 13:
                        compareResult = ObjectCompare.Compare(signalA.Rsi, signalB.Rsi);
                        break;
                    case 14:
                        compareResult = ObjectCompare.Compare(signalA.StochOscillator, signalB.StochOscillator);
                        break;
                    case 15:
                        compareResult = ObjectCompare.Compare(signalA.StochSignal, signalB.StochSignal);
                        break;
                    case 16:
                        compareResult = ObjectCompare.Compare(signalA.Sma200, signalB.Sma200);
                        break;
                    case 17:
                        compareResult = ObjectCompare.Compare(signalA.Sma50, signalB.Sma50);
                        break;
                    case 18:
                        compareResult = ObjectCompare.Compare(signalA.Sma20, signalB.Sma20);
                        break;
                    case 19:
                        compareResult = ObjectCompare.Compare(signalA.PSar, signalB.PSar);
                        break;
                    //#if DEBUG
                    //20 => ObjectCompare.Compare(signalA.PSarDave, signalB.PSarDave),
                    //21 => ObjectCompare.Compare(signalA.PSarJason, signalB.PSarJason),
                    //22 => ObjectCompare.Compare(signalA.PSarTulip, signalB.PSarTulip),
                    //#endif
                    default:
                        compareResult = ObjectCompare.Compare(listviewA.SubItems[SortColumn].Text, listviewB.SubItems[SortColumn].Text);// Compare via string
                        break;
                }

            }

            if (compareResult == 0)
            {
                compareResult = ObjectCompare.Compare(signalA.CloseDate, signalB.CloseDate);
                if (compareResult == 0)
                {
                    if (this.SortOrder == SortOrder.Ascending)
                        compareResult = ObjectCompare.Compare(signalA.Symbol.Name, signalB.Symbol.Name);
                    else
                        compareResult = ObjectCompare.Compare(signalB.Symbol.Name, signalA.Symbol.Name);
                }
                if (compareResult == 0)
                {
                    if (this.SortOrder == SortOrder.Ascending)
                        compareResult = ObjectCompare.Compare(signalA.Interval.IntervalPeriod, signalB.Interval.IntervalPeriod);
                    else
                        compareResult = ObjectCompare.Compare(signalB.Interval.IntervalPeriod, signalA.Interval.IntervalPeriod);
                }
            }


            // Calculate correct return value based on object comparison
            if (this.SortOrder == SortOrder.Ascending)
            {
                // Ascending sort is selected, return normal result of compare operation
                return compareResult;
            }
            else if (this.SortOrder == SortOrder.Descending)
            {
                // Descending sort is selected, return negative result of compare operation
                return (-compareResult);
            }
            else
            {
                // Return '0' to indicate they are equal
                return 0;
            }
        }

        /// <summary>
        /// Gets or sets the number of the column to which to apply the sorting operation (Defaults to '0').
        /// </summary>
        public int SortColumn { set; get; } = 0;

        /// <summary>
        /// Gets or sets the order of sorting to apply (for example, 'Ascending' or 'Descending').
        /// </summary>
        public SortOrder Order { set; get; }


    }
}