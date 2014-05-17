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
                TopPanel.Height = Settings.TopPanelHeight;

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
                Settings.TopPanelHeight = TopPanel.Height;

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

        Size _szDragTargetOffs;
        PathFigure _pfDragBox;

        private static int _pathDecimals = 1;

        private void OverlayBoxes_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (null != _pfDragBox)
                {
                    var pen = new Pen(Brushes.Black, 0);
                    var ptMouse = e.GetPosition(PathCanvas);
                    var tmpg = new PathGeometry(new[] { _pfDragBox });
                    var rcBox = tmpg.GetRenderBounds(pen);

                    ptMouse = new Point(Math.Round(ptMouse.X, _pathDecimals), Math.Round(ptMouse.Y, _pathDecimals));

                    //  X,Y coordinates. Mouse movements are scaled by the transform. 
                    //  Which is nice, until you scale it by 6 or 11 or something that doesn't go evenly
                    //  into 1, and everything goes weird. 
                    double xDest = ptMouse.X - _szDragTargetOffs.Width;
                    double yDest = ptMouse.Y - _szDragTargetOffs.Height;

                    double xOffs = xDest - _pfDragBox.StartPoint.X;
                    double yOffs = yDest - _pfDragBox.StartPoint.Y;

                    _pfDragBox.StartPoint = new Point(Math.Round(xDest, _pathDecimals), Math.Round(yDest, _pathDecimals));

                    double scale = ViewModel.Scale;

                    foreach (PolyLineSegment seg in _pfDragBox.Segments)
                    {
                        for (int i = 0; i < seg.Points.Count; ++i)
                        {
                            var pt = seg.Points[i];
                            pt.X = Math.Round(pt.X + xOffs, _pathDecimals);
                            pt.Y = Math.Round(pt.Y + yOffs, _pathDecimals);
                            seg.Points[i] = pt;
                        }
                    }
                }
            }
        }

        private void OverlayBoxes_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var pg = ViewModel.OverlayBoxes as PathGeometry;
            var pen = new Pen(Brushes.Black, 0);

            foreach (PathFigure pf in pg.Figures)
            {
                var ptMouse = e.GetPosition(PathCanvas);
                var tmpg = new PathGeometry(new[] { pf });
                var rcBox = tmpg.GetRenderBounds(pen);

                if (rcBox.Contains(ptMouse))
                {
                    _szDragTargetOffs = new Size(Math.Round(ptMouse.X - rcBox.Left, _pathDecimals), Math.Round(ptMouse.Y - rcBox.Top, _pathDecimals));

                    _pfDragBox = pf;

                    PathCanvas.CaptureMouse();

                    return;
                }
            }
        }

        private void OverlayBoxes_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _pfDragBox = null;
            PathCanvas.ReleaseMouseCapture();
        }
    }
}
