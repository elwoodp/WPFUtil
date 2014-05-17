using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows.Interop;

namespace PathMaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            System.Diagnostics.Trace.WriteLine("public MainWindow()");
            InitializeComponent();

            System.Diagnostics.Trace.WriteLine("if (DesignerProperties.GetIsInDesignMode(this))");

            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            DataContext = new PathMakerViewModel();

            LoadSettings(Settings);

            if (((PathMaker.App)App.Current).Args.Length > 0)
            {
                try
                {
                    var args = ((PathMaker.App)App.Current).Args;
                    if (System.IO.File.Exists(args[0]))
                    {
                        ViewModel.LoadFile(args[0]);
                    }
                    else
                    {
                        ViewModel.PathText = args[0];
                    }

                    ViewModel.UpdatePath();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(App.Current.MainWindow, "Error loading path: " + ex.Message, "PathMaker",
                                                   System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
        }

        public PathMakerViewModel ViewModel
        {
            get { return DataContext as PathMakerViewModel; }
        }

        internal PathMaker.Properties.Settings Settings
        {
            get { return PathMaker.Properties.Settings.Default; }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SaveSettings(Settings);
        }

        private void LoadSettings(PathMaker.Properties.Settings settings)
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                Height = settings.MainWndHeight;
                Width = settings.MainWndWidth;
                WindowState = settings.MainWndState;
                RowTopPanel.Height = Settings.TopPanelHeight;

                ViewModel.LoadSettings(settings);
            }
        }

        private void SaveSettings(PathMaker.Properties.Settings settings)
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                //  Don't start out Minimized when we restart. 
                if (WindowState == WindowState.Minimized)
                    Settings.MainWndState = WindowState.Normal;
                else
                    Settings.MainWndState = WindowState;

                if (WindowState != WindowState.Normal)
                    WindowState = WindowState.Normal;

                Settings.MainWndHeight = Height;
                Settings.MainWndWidth = Width;
                Settings.TopPanelHeight = RowTopPanel.Height;

                ViewModel.SaveSettings(Settings);
                Properties.Settings.Default.Save();
            }
        }


        private static Regex _reNines = new Regex(@"^0\.9+$");

        private void ScaleText_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            try
            {
                var tb = (sender as TextBox);

                if (null == tb)
                {
                    throw new Exception("Sender is not TextBox");
                }

                double dValue = 0;
                //  Text/scaling value: Negative dValue -> 0.n, positive dValue -> > 1
                double scaleValue = 1;

                Double.TryParse(tb.Text, out scaleValue);
                dValue = ScaleToLinearDouble(scaleValue);
             
                int change = e.Delta / 120;

                dValue += change;

                scaleValue = LinearDoubleToScale(dValue);

                tb.Text = scaleValue.ToString();
            }
            catch ( Exception ex )
            {
                StatusBar.Items.Clear();
                StatusBar.Items.Add(ex.Message);
            }
        }

        public static double ScaleToLinearDouble(double scaleValue)
        {
            double dValue = 0;
            if (scaleValue >= 1.0)
            {
                dValue = scaleValue - 1;
            }
            else
            {
                //  0.5 -> -1
                //  0.33 -> -2
                //  0.25 -> -3

                //  1 / .5      == 2
                //  1 / .333    == 3
                //  1 / .25     == 4

                dValue = -((1 / scaleValue) - 1);
            }

            if (Math.Abs(((double)(int)Math.Abs(dValue)) - Math.Abs(dValue)) < 0.0001)
            {
                dValue = Math.Round(dValue);
            }

            //System.Diagnostics.Trace.WriteLine(String.Format("STLD {0} {1}", scaleValue, dValue));

            return dValue;
        }

        public static double LinearDoubleToScale(double dValue)
        {
            double scaleValue = 0;
            //  0 -> 1
            //  1 -> 2
            //  -1 -> 1 / 2
            //  -2 -> 1 / 3
            if (dValue >= 0)
            {
                scaleValue = dValue + 1;
            }
            else
            {
                scaleValue = 1 / Math.Abs(dValue - 1);
            }

            //System.Diagnostics.Trace.WriteLine(String.Format("LDTS {0} {1}", scaleValue, dValue));

            return scaleValue;
        }

        private void NumberText_GotFocus(object sender, RoutedEventArgs e)
        {
            //  Returns true but has no effect on receipt of mouse wheel messages
            //bool bCapture = (sender as UIElement).CaptureMouse();
        }

        private void UDoubleBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            try
            {
                var tb = (sender as TextBox);

                if (null == tb)
                {
                    throw new Exception("Sender is not TextBox");
                }

                double dValue = 0;

                Double.TryParse(tb.Text, out dValue);
                double change = e.Delta / 120;

                dValue += ( 0.5 * change );

                dValue = Math.Max(0, dValue);

                tb.Text = dValue.ToString();
                tb.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            }
            catch (Exception ex)
            {
                StatusBar.Items.Clear();
                StatusBar.Items.Add(ex.Message);
            }
        }

        private void UDoubleBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            try
            {
                var tb = (sender as TextBox);

                if (null == tb)
                {
                    throw new Exception("Sender is not TextBox");
                }

                double dValue = 0;

                if (Double.TryParse(tb.Text, out dValue))
                {
                    if (dValue < 0)
                    {
                        tb.Text = "1";
                    }
                }
                else
                {
                    tb.Text = "1";
                }
            }
            catch (Exception ex)
            {
                StatusBar.Items.Clear();
                StatusBar.Items.Add(ex.Message);
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var fileName = ((string[])e.Data.GetData(DataFormats.FileDrop)).FirstOrDefault();

                ViewModel.LoadFile(fileName);
            }
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var fileName = ((string[])e.Data.GetData(DataFormats.FileDrop)).FirstOrDefault();

                if (System.IO.File.Exists(fileName))
                    e.Effects = DragDropEffects.Copy;
            }
        }

        private void CopyImage_Click(object sender, RoutedEventArgs e)
        {
            //var image = new Image();
            var interopbitmap = Clipboard.GetImage() as InteropBitmap;
            //_img.Source = BitmapHandling.ImageFromClipboardDib();
            //PathContainer.Content = image;

            //int w = 0;

            //image.Measure(new Size(image.Width, image.Height));
            //image.Arrange(new Rect(new Size(image.Width, image.Height)));
            //image.UpdateLayout();
            //PathContainer.Measure(new Size(PathContainer.Width, PathContainer.Height));
            //PathContainer.Arrange(new Rect(new Size(PathContainer.Width, PathContainer.Height)));
            //PathContainer.UpdateLayout();
            //PathContainer.Content = BitmapHandling.GetBitmapSourceFromClipboard();


            //ImageSource icon = new ImageSource();

            /*
            var tfm = PathContainer.RenderTransform;
            var img = ViewModel.CreateImage(tfm);

            Clipboard.SetImage(img);
//            */

            /*
            var rcBounds = ViewModel.Path.Data.GetRenderBounds(ViewModel.GetPen());

            var tfm = PathContainer.RenderTransform;

            if (null != tfm)
            {
                rcBounds = tfm.TransformBounds(rcBounds);
            }

            int pxWidth = (int)Math.Round (rcBounds.Width);
            int pxHeight = (int)Math.Round(rcBounds.Height);

            RenderTargetBitmap rtb = new RenderTargetBitmap(pxWidth, pxHeight, 96, 96, PixelFormats.Pbgra32);

            PathContainer.Background = Brushes.Transparent;

            rtb.Render(PathContainer);

            Clipboard.SetImage(BitmapFrame.Create(rtb));
//            */

            /*
            var path = ViewModel.CreatePath();

            var tfm = PathContainer.RenderTransform;
            var rcUnscaled = path.Data.GetRenderBounds(ViewModel.GetPen());
            var rcScaled = tfm.TransformBounds(rcUnscaled);

            int pxWidth = (int)Math.Round(rcScaled.Width);
            int pxHeight = (int)Math.Round(rcScaled.Height);

            Canvas canvas = new Canvas();

            canvas.Width = pxWidth;
            canvas.Height = pxHeight;
            canvas.Margin = new Thickness(0);
            canvas.Background = Brushes.Transparent;

            //canvas.Measure(new Size(canvas.Width, canvas.Height));
            //canvas.Arrange(new Rect(new Size(canvas.Width, canvas.Height)));

            canvas.Children.Add(path);

            RenderTargetBitmap rtb = new RenderTargetBitmap(pxWidth, pxHeight, 96, 96, PixelFormats.Pbgra32);

            rtb.Render(canvas);

            var image = new PngBitmapEncoder();

            image.Frames.Add(BitmapFrame.Create(rtb));

            int w = 0;
            */
        }

        Point _ptDragTargetOffs;
        PathFigure _pfDragBox;
        PathFigure _pfDragFigure;
        Point? _ptDragViewPort;

        private void PathContentControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var ptMouse = e.GetPosition(PathCanvas);
                PathFigure pfDragged = null;

                if (null != _pfDragBox)
                {
                    pfDragged = _pfDragBox;
                }
                else if (null != _pfDragFigure)
                {
                    pfDragged = _pfDragFigure;
                }

                if (null != pfDragged)
                {
                    var pen = new Pen(Brushes.Black, 0);
                    var tmpg = new PathGeometry(new[] { pfDragged });
                    var rcBox = tmpg.GetRenderBounds(pen);

                    //  X,Y coordinates. Mouse movements are scaled by the transform. 
                    double xDest = ptMouse.X - _ptDragTargetOffs.X;
                    double yDest = ptMouse.Y - _ptDragTargetOffs.Y;

                    double xOffs = xDest - pfDragged.StartPoint.X;
                    double yOffs = yDest - pfDragged.StartPoint.Y;

                    if (pfDragged == _pfDragBox)
                    {
                        //  It knows what to do with this
                        pfDragged.StartPoint = new Point(xDest, yDest);
                    }
                    else
                    {
                        //pfDragged.StartPoint = ViewModel.Snap(pfDragged.StartPoint, xOffs, yOffs);
                        pfDragged.Offset(xOffs, yOffs);
                    }
                }
                else if (_ptDragViewPort.HasValue)
                {
                    double xOffs = ptMouse.X - _ptDragViewPort.Value.X;
                    double yOffs = ptMouse.Y - _ptDragViewPort.Value.Y;

                    ViewModel.RequiredOffset = new Point(ViewModel.RequiredOffset.X + xOffs, ViewModel.RequiredOffset.Y + yOffs);
                }
            }
        }

        private void PathContentControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var pgBoxes = ViewModel.OverlayBoxes as PathGeometry;
            var pgLines = ViewModel.Geometry as PathGeometry;
            var penBox = new Pen(Brushes.Black, 0);
            var penSegment = ViewModel.GetPen();

            var figures = pgBoxes.Figures.Union(pgLines.Figures);

            int ctBoxes = pgBoxes.Figures.Count;

            foreach (PathFigure pf in figures)
            {
                bool bIsHit = false;
                bool bIsBox = ctBoxes-- > 0;
                var pen = bIsBox ? penBox : penSegment;

                var ptMouse = e.GetPosition(PathCanvas);
                var tmpg = new PathGeometry(new[] { pf });

                bIsHit = tmpg.FillContains(ptMouse) || tmpg.StrokeContains(pen, ptMouse);

                if (!bIsBox)
                {
                    var p = new System.Windows.Shapes.Path() { Data = tmpg };
                    //p.RenderTransform = PathContentControl.RenderTransform;
                    var ht = VisualTreeHelper.HitTest(p, ptMouse);
                    var rcSegment = tmpg.GetRenderBounds(penSegment);
                }

                if (bIsHit)
                {
                    var rcBox = tmpg.GetRenderBounds(pen);
                    _ptDragTargetOffs = new Point(ptMouse.X - rcBox.Left, ptMouse.Y - rcBox.Top);

                    if (bIsBox)
                    {
                        _pfDragBox = pf;
                    }
                    else
                    {
                        _pfDragFigure = pf;
                    }

                    PathCanvas.CaptureMouse();

                    return;
                }
            }

            _ptDragViewPort = e.GetPosition(PathCanvas);
            PathCanvas.CaptureMouse();
        }

        private void PathContentControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_pfDragBox != null || _pfDragFigure != null)
            {
                _pfDragBox = null;
                _pfDragFigure = null;
                ViewModel.UpdatePath();
            }

            if (PathCanvas.IsMouseCaptured)
            {
                PathCanvas.ReleaseMouseCapture();
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double cxOtherRows = 
                RowMenuPanel.ActualHeight
                + RowGridSplitter.ActualHeight
                + RowTextBoxCell.MinHeight
                + RowGraphicsControls.MinHeight
                + RowStatusBar.MinHeight;

            RECT rcClient;
            GetClientRect(new WindowInteropHelper(this).Handle, out rcClient);

            double maxHeight = rcClient.Height - cxOtherRows;

            RowTopPanel.MaxHeight = maxHeight;

            //  XXX This changes the height of RowTextBoxCell to its MinHeight when the window is resized

            //if (maxHeight < RowTopPanel.Height.Value)
            {
                RowTopPanel.Height = new GridLength(RowTopPanel.MaxHeight);
            }

            /*
            System.Diagnostics.Trace.WriteLine("----------");

            System.Diagnostics.Trace.WriteLine("MainGrid.RenderSize.Height    == " + MainGrid.RenderSize.Height);
            System.Diagnostics.Trace.WriteLine("RowGraphicsControls.MinHeight == " + RowGraphicsControls.MinHeight);
            System.Diagnostics.Trace.WriteLine("maxHeight                     == " + maxHeight);
            System.Diagnostics.Trace.WriteLine("RowTopPanel.Height            == " + RowTopPanel.Height);
            System.Diagnostics.Trace.WriteLine("RowTopPanel.MaxHeight         == " + RowTopPanel.MaxHeight);
            */
        }

        #region Native Methods
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
 
        [Serializable, System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct RECT
        {
          public int Left;
          public int Top;
          public int Right;
          public int Bottom;
 
          public RECT(int left_, int top_, int right_, int bottom_)
          {
            Left = left_;
            Top = top_;
            Right = right_;
            Bottom = bottom_;
          }
 
          public int Height { get { return Bottom - Top; } }
          public int Width { get { return Right - Left; } }
          public Size Size { get { return new Size(Width, Height); } }
 
          public Point Location { get { return new Point(Left, Top); } }
 
          // Handy method for converting to a System.Drawing.Rectangle
          public Rect ToRectangle()
          { return new Rect(Left, Top, Right, Bottom); }
 
          public static RECT FromRectangle(Rect rectangle)
          {
            return new Rect(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
          }
 
          public override int GetHashCode()
          {
            return Left ^ ((Top << 13) | (Top >> 0x13))
              ^ ((Width << 0x1a) | (Width >> 6))
              ^ ((Height << 7) | (Height >> 0x19));
          }
 
          #region Operator overloads
 
          public static implicit operator Rect(RECT rect)
          {
            return rect.ToRectangle();
          }
 
          public static implicit operator RECT(Rect rect)
          {
            return FromRectangle(rect);
          }
 
          #endregion
        }
 
        public static RECT GetClientRect(IntPtr hWnd)
        {
          RECT result = new RECT();
          GetClientRect(hWnd, out result);
          return result;
        }
        #endregion

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SetUpTest();
        }
    }
}
