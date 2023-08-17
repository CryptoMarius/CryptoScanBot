namespace CryptoSbmScanner;

internal class ListViewHeaderContext : ListViewDoubleBuffered
{

    /// <summary>
    ///  The contextMenuStrip associated with this control. The contextMenuStrip
    ///  will be shown when the user right clicks the mouse on the control.
    ///  Note: if a context menu is also assigned, it will take precedence over this property.
    /// </summary>
    //This contains the Column Index and its corresponding Rectangle in screen coordinates.
    internal Dictionary<int, Rectangle> columnRectangles = new();

    public virtual ContextMenuStrip HeaderContextMenuStrip { get; set; }

    protected override void OnDrawItem(DrawListViewItemEventArgs e)
    {
        e.DrawDefault = true;
        base.OnDrawItem(e);
    }

    protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
    {
        e.DrawDefault = true;
        base.OnDrawSubItem(e);
    }

    protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
    {
        columnRectangles[e.ColumnIndex] = RectangleToScreen(e.Bounds);
        e.DrawDefault = true;
        base.OnDrawColumnHeader(e);
    }

    protected override void WndProc(ref Message m)
    {
        bool handled = false;
        if (m.Msg == 0x7b) //WM_CONTEXTMENU
        {
            if (HeaderContextMenuStrip != null)
            {
                int lp = m.LParam.ToInt32();
                int x = ((lp << 16) >> 16);
                int y = lp >> 16;
                foreach (KeyValuePair<int, Rectangle> p in columnRectangles)
                {
                    if (p.Value.Contains(new Point(x, y)))
                    {
                        //MessageBox.Show(Columns[p.Key].Text); <-- Try this to test if you want.
                        HeaderContextMenuStrip.Show(Control.MousePosition);
                        handled = true;
                        break;
                    }
                }
            }
        }
        if (!handled)
            base.WndProc(ref m);
    }

}
