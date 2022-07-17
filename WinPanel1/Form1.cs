using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using IDataObject_Com = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace WinPanel1;

public partial class Form1 : Form
{

    #region SendToBackground
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    IntPtr desktopHandle = (IntPtr)FindWindow("Progman", null);
    //WallForm wallWindow = new WallForm();//WinForms Form

    bool isBackground = false;
    #endregion

    #region StartDrag
    public const int WM_NCLBUTTONDOWN = 0xA1;
    public const int HT_CAPTION = 0x2;

    [DllImportAttribute("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd,
                     int Msg, int wParam, int lParam);
    [DllImportAttribute("user32.dll")]
    public static extern bool ReleaseCapture();
    #endregion

    public Form1()
    {
        InitializeComponent();

        #region OnResize
        //this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.ResizeRedraw, true);
        #endregion

        #region Transparent
        //SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        //this.BackColor = Color.Black;
        //this.BackColor = Color.FromArgb(128, 0, 0, 0); ;
        //this.BackColor = Color.LimeGreen;
        //this.TransparencyKey = Color.LimeGreen;
        //Opacity = 0.5;
        #endregion


        //bitmap.MakeTransparent();
        //bitmap.SetPixel(0, 0, Color.FromArgb(128, 0, 0, 0));
        //BackgroundImage = bitmap;


        InitNotifyApp();
        LoadBgColor();

        DragDrop += Form1_DragDrop;
        DragEnter += Form1_DragEnter;
        DragOver += Form1_DragOver;
        DragLeave += Form1_DragLeave;

        MouseDown += Form1_MouseDown;
        flowLayoutPanel1.MouseDown += Form1_MouseDown;

        //appIconContextMenu.Items.Add("Delete", null, OnDeleteDockItem_MenuStrinClick);
        var m1 = new MyToolStripMenuItem { Text = "Delete" };
        m1.Click += OnDeleteDockItem_MenuStrinClick;
        appIconContextMenu.Items.Add(m1);

        var m2 = new MyToolStripMenuItem { Text = "Change image" };
        m2.Click += OnChangeImageDockItem_MenuStrinClick;
        appIconContextMenu.Items.Add(m2);

        flowLayoutPanel1.BackColor = Color.Transparent;

        InitListDragDrop();

        //disallow minimize
        Resize += Form1_Resize;

    }

    private void Form1_Resize(object? sender, EventArgs e)
    {
        //this.WindowState = FormWindowState.Normal;

    }

    ContextMenuStrip appIconContextMenu = new ContextMenuStrip();


    void InitNotifyApp()
    {
        ShowInTaskbar = false;
        var ni = new NotifyIcon() { Visible = true, };
        ni.Text = this.Text;
        ni.Icon = this.Icon;

        var m = new MyToolStripMenuItem { Text = "Exit" };
        m.Click += (s, e) => { btnClose_Click(s, e); };
        appTrayIconContextMenu.Items.Add(m);

        ni.ContextMenuStrip = appTrayIconContextMenu;
        //ni.ShowBalloonTip(0,"sss","sdsdsdd", ToolTipIcon.Info);

        FormClosing += (s, e) => { ni.Visible = false; };

        ni.Click += Ni_Click;
    }

    private void Ni_Click(object? sender, EventArgs e)
    {
        this.Activate();
    }

    ContextMenuStrip appTrayIconContextMenu = new ContextMenuStrip();


    private void Form1_Load(object sender, EventArgs e)
    {
        #region SavePosition
        string rectStr = Properties.Settings.Default.WindowPosition;
        RectangleConverter r = new RectangleConverter();
        Rectangle rect = new Rectangle();
        if (r.ConvertFromString(rectStr) is not null)
            rect = (Rectangle)r.ConvertFromString(rectStr)!;



        if (Properties.Settings.Default.IsMaximized)
            WindowState = FormWindowState.Maximized;
        else if (Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(rect)))
        {
            StartPosition = FormStartPosition.Manual;
            DesktopBounds = rect;
            WindowState = FormWindowState.Normal;
        }
        #endregion

        MyInit();

    }

    //Bitmap bitmap = new Bitmap(1, 1);


    void SwitchParent()
    {
        //wallWindow.Show();
        SetParent(Handle, desktopHandle);
        //wallWindow.SendToBack();
    }

    void SwitchFront()
    {
        SetParent(Handle, IntPtr.Zero);
    }


    private void btnToggleArrange_Click(object sender, EventArgs e)
    {
        isBackground = !isBackground;
        if (isBackground) SwitchParent(); else SwitchFront();
    }



    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        #region SavePosition
        RectangleConverter r = new RectangleConverter();
        var rectangleAsString = r.ConvertToString(this.DesktopBounds);

        Properties.Settings.Default.IsMaximized = WindowState == FormWindowState.Maximized;
        Properties.Settings.Default.WindowPosition = rectangleAsString;
        Properties.Settings.Default.Save();
        #endregion

        MyFormClose();
    }

    private void btnClose_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }

    #region OnResize
    private const int cGrip = 16;      // Grip size
    private const int cCaption = 32;   // Caption bar height;

    protected override void OnPaint(PaintEventArgs e)
    {
        Rectangle rc = new Rectangle(this.ClientSize.Width - cGrip, this.ClientSize.Height - cGrip, cGrip, cGrip);
        ControlPaint.DrawSizeGrip(e.Graphics, this.BackColor, rc);
        rc = new Rectangle(0, 0, this.ClientSize.Width, cCaption);
        //e.Graphics.FillRectangle(Brushes.DarkBlue, rc);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x84)
        {  // Trap WM_NCHITTEST
            Point pos = new Point(m.LParam.ToInt32());
            pos = this.PointToClient(pos);
            if (pos.Y < cCaption)
            {
                m.Result = (IntPtr)2;  // HTCAPTION
                return;
            }
            if (pos.X >= this.ClientSize.Width - cGrip && pos.Y >= this.ClientSize.Height - cGrip)
            {
                m.Result = (IntPtr)17; // HTBOTTOMRIGHT
                return;
            }
        }
        base.WndProc(ref m);
    }
    #endregion

    void LoadBgColor()
    {
        BackColor = Properties.Settings.Default.BgColor;
    }

    void SaveBgColor()
    {
        Properties.Settings.Default.BgColor = BackColor;
        Properties.Settings.Default.Save();

    }

    private void btnBg_Click(object sender, EventArgs e)
    {
        ColorDialog MyDialog = new ColorDialog();
        // Keeps the user from selecting a custom color.
        MyDialog.AllowFullOpen = true;
        // Allows the user to get help. (The default is false.)
        MyDialog.ShowHelp = true;
        //MyDialog
        // Sets the initial color select to the current text color.
        //MyDialog.Color = textBox1.ForeColor;

        // Update the text box color if the user clicks OK 
        if (MyDialog.ShowDialog() == DialogResult.OK)
        {
            //textBox1.ForeColor = MyDialog.Color;
            BackColor = MyDialog.Color;
            SaveBgColor();
        }
    }

    private void Form1_MouseDown(object sender,
        System.Windows.Forms.MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }
    }

    #region DragAndDrop
    private IDropTargetHelper ddHelper = (IDropTargetHelper)new DragDropHelper();
    private void Form1_DragDrop(object sender, DragEventArgs e)
    {
        e.Effect = DragDropEffects.Copy;
        Point p = Cursor.Position;
        Win32Point wp;
        wp.x = p.X;
        wp.y = p.Y;

        ddHelper.Drop(e.Data as IDataObject_Com, ref wp, (int)e.Effect);

        if (e.Data is not null)
        {
            var dropped = ((string[])e.Data.GetData(DataFormats.FileDrop));

            bool isNotFile = dropped == null;

            if (isNotFile) return;

            var files = dropped.ToList();

            if (!files.Any())
                return;

            //foreach (string drop in dropped)
            //    if (Directory.Exists(drop))
            //        files.AddRange(Directory.GetFiles(drop, "*.dwg", SearchOption.AllDirectories));

            //foreach (string file in files)
            //{
            //    if (!fileList.Contains(file) && file.ToLower().EndsWith(".dwg"))
            //        fileList.Add(file);
            //}

            string path = files.First();
            OnApp__DropApp(path);

            //////PictureBox pbox = pictureBox1;
            //////pbox.SizeMode = PictureBoxSizeMode.Zoom;
            ////////pbox.SizeMode = PictureBoxSizeMode.AutoSize;

            //////{
            //////    string path = files.First();

            //////    FileInfo fileInfo = new FileInfo(path);

            //////    //if (fileInfo.Extension != ".exe" && false)
            //////    {


            //////        //Icon? iconForFile = System.Drawing.Icon.ExtractAssociatedIcon(path);
            //////        //var bmp = iconForFile?.ToBitmap();
            //////        //var bmp = iconForFile.ExtractVistaIcon();

            //////        var size = ExtractIconExHelper3.IconSizeEnum.ExtraLargeIcon;
            //////        Bitmap? image = ExtractIconExHelper3.GetFileImageFromPath(path, size);

            //////        //if (iconForFile is not null)
            //////        {
            //////            //pictureBox1.Image = Bitmap.FromHicon(new Icon(iconForFile, new Size(48, 48)).Handle);
            //////            //pictureBox1.Image = Bitmap.FromHicon(new Icon(iconForFile, iconForFile.Size).Handle);
            //////            //pbox.Image = Image.FromHbitmap(bmp.GetHbitmap(pbox.BackColor));
            //////            //pbox.Image = bmp;
            //////            pbox.Image = image;
            //////            //pbox.DrawToBitmap(image, pbox.ClientRectangle);
            //////            //pbox.DrawToBitmap(image, new Rectangle(0,0, image.Size.Width, image.Size.Height));
            //////            //pbox.Refresh();
            //////        }
            //////    }
            //////    //else
            //////    //{
            //////    //    Icon? iconForFile = ExtractIconExHelper.ExtractIconFromExe(path, true);

            //////    //    if (iconForFile is not null)
            //////    //    {
            //////    //        pbox.Image = Bitmap.FromHicon(new Icon(iconForFile, pbox.Size).Handle);
            //////    //    }
            //////    //}
            //////}
        }
    }

    private void Form1_DragEnter(object sender, DragEventArgs e)
    {
        e.Effect = DragDropEffects.Copy;
        Point p = Cursor.Position;
        Win32Point wp;
        wp.x = p.X;
        wp.y = p.Y;

        //ddHelper.DragEnter(this.Handle, e.Data as IDataObject_Com, ref wp, (int)e.Effect);
    }

    private void Form1_DragLeave(object sender, EventArgs e)
    {
        ddHelper.DragLeave();
    }

    private void Form1_DragOver(object sender, DragEventArgs e)
    {
        e.Effect = DragDropEffects.Copy;
        Point p = Cursor.Position;
        Win32Point wp;
        wp.x = p.X;
        wp.y = p.Y;
        ddHelper.DragOver(ref wp, (int)e.Effect);
    }
    #endregion

    private void flowLayoutPanel1_Resize(object sender, EventArgs e)
    {
        FlowLayoutPanel panel = (sender as FlowLayoutPanel)!;

        var childs = panel.Controls.OfType<PictureBox>();

        int h = panel.Height - panel.Padding.Top - panel.Padding.Bottom - panel.Margin.Top - panel.Margin.Bottom;

        foreach (var child in childs)
        {
            child.Height = h;
            child.Width = h;
        }


    }

    ///------------------------
    /// Dock
    /// 
    SaveFile saveFile;
    List<DockApp> Apps = new();

    void MyInit()
    {
        flowLayoutPanel1.Controls.Clear();
        saveFile = SaveFile.LoadUserData();
        Apps = saveFile.Apps ?? new();
        labelDragAndDropHere.Visible = Apps.Count == 0;
        panel1.Visible = false;
        RefreshPanel();

        var bg = DesktopManagement.GetCurrentDesktopWallpaper();
        wll = resizeImage(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, bg);

        Move += Form1_Move;
    }

    private void Form1_Move(object? sender, EventArgs e)
    {
        Color color;
        try
        {
            int x = Left + Width / 2;
            int y = Top + Height / 2;
            color = wll.GetPixel(x, y);
        }
        catch (Exception)
        {
            color = wll.GetPixel(0, 0);
        }
        BackColor = color;
        SaveBgColor();
    }

    Bitmap wll;

    //public Image resizeImage(int newWidth, int newHeight, string stPhotoPath)
    public Bitmap resizeImage(int newWidth, int newHeight, string stPhotoPath)
    {
        Image imgPhoto = Image.FromFile(stPhotoPath);

        int sourceWidth = imgPhoto.Width;
        int sourceHeight = imgPhoto.Height;

        //Consider vertical pics
        if (sourceWidth < sourceHeight)
        {
            int buff = newWidth;

            newWidth = newHeight;
            newHeight = buff;
        }

        int sourceX = 0, sourceY = 0, destX = 0, destY = 0;
        float nPercent = 0, nPercentW = 0, nPercentH = 0;

        nPercentW = ((float)newWidth / (float)sourceWidth);
        nPercentH = ((float)newHeight / (float)sourceHeight);
        if (nPercentH < nPercentW)
        {
            nPercent = nPercentH;
            destX = System.Convert.ToInt16((newWidth -
                      (sourceWidth * nPercent)) / 2);
        }
        else
        {
            nPercent = nPercentW;
            destY = System.Convert.ToInt16((newHeight -
                      (sourceHeight * nPercent)) / 2);
        }

        int destWidth = (int)(sourceWidth * nPercent);
        int destHeight = (int)(sourceHeight * nPercent);


        Bitmap bmPhoto = new Bitmap(newWidth, newHeight,
                      PixelFormat.Format24bppRgb);

        bmPhoto.SetResolution(imgPhoto.HorizontalResolution,
                     imgPhoto.VerticalResolution);

        Graphics grPhoto = Graphics.FromImage(bmPhoto);
        grPhoto.Clear(Color.White);
        grPhoto.InterpolationMode =
            System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        grPhoto.DrawImage(imgPhoto,
            new Rectangle(destX, destY, destWidth, destHeight),
            new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
            GraphicsUnit.Pixel);

        grPhoto.Dispose();
        imgPhoto.Dispose();

        return bmPhoto;
    }

    void MyFormClose()
    {
        //SAVE();
    }

    void SAVE()
    {
        saveFile.Apps = this.Apps;
        saveFile.Save();
    }

    void OnApp__DropApp(string path)
    {
        var app = new DockApp { Id = Guid.NewGuid(), Filename = path };
        Apps.Add(app);
        PictureBox pb = CreatePictureBox(app);
        flowLayoutPanel1.Controls.Add(pb);
        flowLayoutPanel1_Resize(flowLayoutPanel1, null);
        labelDragAndDropHere.Visible = Apps.Count == 0;
        SAVE();
    }

    PictureBox CreatePictureBox(DockApp app)
    {
        var size = ExtractIconExHelper3.IconSizeEnum.ExtraLargeIcon;
        Image? image;
        bool extract = string.IsNullOrEmpty(app.IconImage);
        if (!extract)
        {
            image = Image.FromFile(app.IconImage);
        }
        else
        {
            image = ExtractIconExHelper3.GetFileImageFromPath(app.Filename, size);
        }
        PictureBox pb = new PictureBox();
        pb.SizeMode = PictureBoxSizeMode.Zoom;
        pb.BackColor = Color.Transparent;
        pb.Image = image;
        pb.ContextMenuStrip = appIconContextMenu;
        pb.Click += DockAppPictureBox_Click;
        pb.MouseEnter += Pb_MouseEnter;
        pb.MouseLeave += Pb_MouseLeave;


        pb.MouseDown += Pb_MouseDown;
        pb.AllowDrop = true;
        pb.DragOver += pbox_DragOver;
        pb.DragDrop += Pb_DragDrop;

        return pb;
    }

    private void Pb_DragDrop(object? sender, DragEventArgs e)
    {
        int z = 1;
        //draggint
        PictureBox pb = e.Data.GetData(typeof(PictureBox)) as PictureBox;

        //target
        PictureBox pb2 = (PictureBox)sender;

        if (pb != null && pb2 != null && pb != pb2)
        {
            FlowLayoutPanel f = flowLayoutPanel1;
            //int myIndex = f.Controls.GetChildIndex(pb);
            MoveBefore(pb2, pb);
        }

    }

    void MoveBefore(PictureBox target, PictureBox dragging)
    {
        FlowLayoutPanel f = flowLayoutPanel1;

        int i1 = f.Controls.GetChildIndex(target);
        int i2 = f.Controls.GetChildIndex(dragging);

        int gi = f.Controls.GetChildIndex(target);
        //int ni = gi -1 <= 1 ? 1 : gi - 1;
        f.Controls.SetChildIndex(dragging, gi);
        f.Controls.SetChildIndex(target, gi + 1);

        MoveListItem(Apps, i2, gi);
        SAVE();
    }

    public static void MoveListItem<T>(List<T> list, int oldIndex, int newIndex)
    {
        var item = list[oldIndex];

        list.RemoveAt(oldIndex);

        if (newIndex > oldIndex) newIndex--;
        // the actual index could have shifted due to the removal

        list.Insert(newIndex, item);
    }

    System.Timers.Timer t = new();

    private void Pb_MouseDown(object? sender, MouseEventArgs e)
    {
        PictureBox q = (PictureBox)sender;
        draggingPb = q;

        whenDrag = () =>
        {
            Debug.WriteLine(">>drag", q.Name);
            Invoke(() =>
            {
                q.DoDragDrop(sender, DragDropEffects.All);
            });
        };
        onDownMousePosition = MousePosition;
        t.Interval = 100;
        t.Start();
        //flowLayoutPanel1.DoDragDrop(sender, DragDropEffects.All);
    }

    Action whenDrag;
    Point onDownMousePosition;

    private void T_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        //Debug.WriteLine(">>timer");
        t.Stop();
        int maxDistance = Math.Max(Math.Abs(onDownMousePosition.X - MousePosition.X), Math.Abs(onDownMousePosition.Y - MousePosition.Y));
        if (maxDistance > 2) whenDrag?.Invoke();

        //<!--
    }

    private void Pb_MouseLeave(object? sender, EventArgs e)
    {
        PictureBox pb = sender as PictureBox;
        pb.BackColor = Color.Transparent;
    }

    private void Pb_MouseEnter(object? sender, EventArgs e)
    {
        PictureBox pb = sender as PictureBox;
        pb.BackColor = Color.FromArgb(128, Color.White);
    }

    void RefreshPanel()
    {
        int i = 0;
        flowLayoutPanel1.Controls.Clear();
        foreach (var app in this.Apps)
        {
            var pb = CreatePictureBox(app);
            pb.Name = "pb" + i++;
            flowLayoutPanel1.Controls.Add(pb);
        }
        flowLayoutPanel1_Resize(flowLayoutPanel1, null);
    }

    void OnDeleteDockItem_MenuStrinClick(object sender, EventArgs e)
    {
        var s = sender as MyToolStripMenuItem;
        ContextMenuStrip z = s.GetCurrentParent() as ContextMenuStrip;
        PictureBox pb = z.SourceControl as PictureBox;

        int index = flowLayoutPanel1.Controls.IndexOf(pb);
        DockApp d = Apps.ElementAt(index);

        flowLayoutPanel1.Controls.Remove(pb);
        Apps.Remove(d);
        SAVE();
    }

    void OnChangeImageDockItem_MenuStrinClick(object sender, EventArgs e)
    {
        var s = sender as MyToolStripMenuItem;
        ContextMenuStrip z = s.GetCurrentParent() as ContextMenuStrip;
        PictureBox pb = z.SourceControl as PictureBox;

        int index = flowLayoutPanel1.Controls.IndexOf(pb);
        DockApp d = Apps.ElementAt(index);

        //flowLayoutPanel1.Controls.Remove(pb);
        //Apps.Remove(d);

        var filePath = string.Empty;

        using (OpenFileDialog openFileDialog = new OpenFileDialog())
        {
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            openFileDialog.Filter = GetImageFilter();
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                //Get the path of specified file
                filePath = openFileDialog.FileName;

                pb.Image = Image.FromFile(filePath);
                d.IconImage = filePath;

            }
        }

        SAVE();
    }

    private static string GetImageFilter()
    {
        return
            "All Files (*.*)|*.*" +
            "|All Pictures (*.emf;*.wmf;*.jpg;*.jpeg;*.jfif;*.jpe;*.png;*.bmp;*.dib;*.rle;*.gif;*.emz;*.wmz;*.tif;*.tiff;*.svg;*.ico)" +
                "|*.emf;*.wmf;*.jpg;*.jpeg;*.jfif;*.jpe;*.png;*.bmp;*.dib;*.rle;*.gif;*.emz;*.wmz;*.tif;*.tiff;*.svg;*.ico" +
            "|Windows Enhanced Metafile (*.emf)|*.emf" +
            "|Windows Metafile (*.wmf)|*.wmf" +
            "|JPEG File Interchange Format (*.jpg;*.jpeg;*.jfif;*.jpe)|*.jpg;*.jpeg;*.jfif;*.jpe" +
            "|Portable Network Graphics (*.png)|*.png" +
            "|Bitmap Image File (*.bmp;*.dib;*.rle)|*.bmp;*.dib;*.rle" +
            "|Compressed Windows Enhanced Metafile (*.emz)|*.emz" +
            "|Compressed Windows MetaFile (*.wmz)|*.wmz" +
            "|Tag Image File Format (*.tif;*.tiff)|*.tif;*.tiff" +
            "|Scalable Vector Graphics (*.svg)|*.svg" +
            "|Icon (*.ico)|*.ico";
    }

    class MyToolStripMenuItem : ToolStripMenuItem
    {

    }

    private void DockAppPictureBox_Click(object? sender, EventArgs e)
    {
        PictureBox pb = (sender as PictureBox)!;
        int index = flowLayoutPanel1.Controls.IndexOf(pb);
        DockApp d = Apps.ElementAt(index);

        string path = d.Filename;

        System.Diagnostics.Process.Start("explorer.exe", path);

    }

    #region SNAP to EdgeScreen
    private const int SnapDist = 100;
    private bool DoSnap(int pos, int edge)
    {
        int delta = pos - edge;
        return delta > 0 && delta <= SnapDist;
    }
    protected override void OnResizeEnd(EventArgs e)
    {
        base.OnResizeEnd(e);
        Screen scn = Screen.FromPoint(this.Location);
        if (DoSnap(this.Left, scn.WorkingArea.Left)) this.Left = scn.WorkingArea.Left;
        if (DoSnap(this.Top, scn.WorkingArea.Top)) this.Top = scn.WorkingArea.Top;
        if (DoSnap(scn.WorkingArea.Right, this.Right)) this.Left = scn.WorkingArea.Right - this.Width;
        if (DoSnap(scn.WorkingArea.Bottom, this.Bottom)) this.Top = scn.WorkingArea.Bottom - this.Height;
    }
    #endregion

    private static Color GetReadableForeColor(Color c)
    {
        return (((c.R + c.B + c.G) / 3) > 128) ? Color.Black : Color.White;
    }

    class DesktopManagement
    {
        private const UInt32 SPI_GETDESKWALLPAPER = 0x73;
        private const int MAX_PATH = 260;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SystemParametersInfo(UInt32 uAction, int uParam, string lpvParam, int fuWinIni);


        public static string GetCurrentDesktopWallpaper()
        {
            string currentWallpaper = new string('\0', MAX_PATH);
            SystemParametersInfo(SPI_GETDESKWALLPAPER, currentWallpaper.Length, currentWallpaper, 0);
            return currentWallpaper.Substring(0, currentWallpaper.IndexOf('\0'));
        }
    }

    //hide from ALT+tab
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            // turn on WS_EX_TOOLWINDOW style bit
            cp.ExStyle |= 0x80;
            return cp;
        }
    }

    //---dragDropListItems
    //flowLayoutPanel1

    void InitListDragDrop()
    {
        //flowLayoutPanel1.MouseMove += OnCstMouseMove;
        //flowLayoutPanel1.AllowDrop = true;
        //flowLayoutPanel1.DragOver += pbox_DragOver;
        t.Elapsed += T_Elapsed;
    }



    void pbox_DragOver(object sender, DragEventArgs e)
    {
        base.OnDragOver(e);
        return;
        // is another dragable
        PictureBox pb = e.Data.GetData(typeof(PictureBox)) as PictureBox;
        //if (e.Data.GetData(typeof(PictureBox)) != null)
        if (draggingPb != null && pb != null && draggingPb != pb)
        {

            //FlowLayoutPanel p = (FlowLayoutPanel)(sender as PictureBox).Parent;
            FlowLayoutPanel p = flowLayoutPanel1;
            //Current Position             
            //int myIndex = p.Controls.GetChildIndex((sender as PictureBox));
            int myIndex = p.Controls.GetChildIndex(pb);

            if (myIndex != 0) return;

            //Dragged to control to location of next picturebox
            PictureBox q = (PictureBox)e.Data.GetData(typeof(PictureBox));

            var w = e;

            p.Controls.SetChildIndex(q, myIndex);
        }
    }

    PictureBox? draggingPb = null;

    private void OnCstMouseMove(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Control cst = sender as Control;
            cst.DoDragDrop(cst.Parent, DragDropEffects.Move);
        }
    }
}

