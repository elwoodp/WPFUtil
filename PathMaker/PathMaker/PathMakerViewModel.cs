﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PathMaker
{
    public class NamedColor : IDisposable
    {
        public NamedColor(string argb)
        {
            Name = argb;
            Color = (Color)ColorConverter.ConvertFromString(argb);
            Brush = new SolidColorBrush(Color);
        }
        public NamedColor(string name, Color color)
        {
            Name = name;
            Color = color;
            Brush = new SolidColorBrush(Color);
        }
        public String Name { get; protected set; }
        public Color Color { get; protected set; }
        public Brush Brush { get; protected set; }

        public void Dispose()
        {
            Brush = null;
        }
    
        public override string ToString()
        {
            return Name;
        }
    }

    public class PathMakerViewModel : INotifyPropertyChanged, IDisposable
    {
        public PathMakerViewModel()
        {
            Initialize(null);

            AppTitle = "PathMaker";

            //PathText = "M 2,0 L 0,2 L 2,5 L 0,8 L 2,10 L 5,8 L 8,10 L 10,8 L 8,5 L 10,2 L 8,0 L 5,2 Z";
#if DEBUG
            PathText = FatX_10x10;
#endif
        }

        public static readonly String FatX_8x8 = "M 2,0 L 0,2 L 2,4 L 0,6 L 2,8 L 4,6 L 6,8 L 8,6 L 6,4 L 8,2 L 6,0 L 4,2 Z";
        public static readonly String FatX_10x10 = "M 2,0 L 0,2 L 3,5 L 0,8 L 2,10 L 5,7 L 8,10 L 10,8 L 7,5 L 10,2 L 8,0 L 5,3 Z";
        public static readonly String ThinX_6x6 = "M 1,0 L 0,1 L 2,3 L 0,5 L 1,6 L 3,4 L 5,6 L 6,5 L 4,3 L 6,1 L 5,0 L 3,2 Z";
        public static readonly String ThinX_8x8 = "M 0,1 L 3,4 L 0,7 L 1,8 L 4,5 L 7,8 L 8,7 L 5,4 L 8,1 L 7,0 L 4,3 L 1,0 Z";

        internal PathMakerViewModel(PathMaker.Properties.Settings settings)
        {
            Initialize(settings);
        }

        #region Initialization
        internal void Initialize(PathMaker.Properties.Settings settings)
        {
            _namedColors = new List<NamedColor>();

            try
            {
                for (int i = 14; i > 0; --i)
                {
                    var argb = String.Format("#f{0:x}{0:x}{0:x}", i);
                    _namedColors.Add(new NamedColor(argb));
                }

                var props = typeof(System.Windows.Media.Colors).GetProperties();
                foreach (var prop in props)
                {
                    if (prop.PropertyType.Equals(typeof(Color)))
                    {
                        _namedColors.Add(new NamedColor(prop.Name, (Color)prop.GetValue(null)));
                    }
                }

                StrokeColor = NamedColors.Where(nc => nc.Color == Colors.Black).First();
                FillColor = NamedColors.Where(nc => nc.Color == Colors.Transparent).First();

                if (null != settings)
                {
                    LoadSettings(settings);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(App.Current.MainWindow, "Error initializing ViewModel: " + ex.Message, "PathMaker", 
                                               System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }
        #endregion Initialization

        #region Public Properties
        private static List<NamedColor> _namedColors;
        public IEnumerable<NamedColor> NamedColors { get { return _namedColors; } }

        private string _pathText = "";
        public String PathText
        {
            get { return _pathText; }
            set
            {
                _pathText = value ?? "";
                OnPropertyChanged("PathText");
            }
        }

        private string _pathErrorText;
        public String PathErrorText
        {
            get { return _pathErrorText; }
            set
            {
                _pathErrorText = value ?? "";
                OnPropertyChanged("PathErrorText");
            }
        }

        private string _appTitle;
        public String AppTitle
        {
            get { return _appTitle; }
            set
            {
                _appTitle = value ?? "";
                OnPropertyChanged("AppTitle");
            }
        }

        private Path _path;
        public Path Path
        {
            get { return _path; }
            set
            {
                _path = value;
                OnPropertyChanged("Path");
            }
        }

        NamedColor _fillColor;
        public NamedColor FillColor
        {
            get { return _fillColor; }
            set
            {
                if (null == value)
                {
                    //throw new ArgumentNullException("value");
                }
                _fillColor = value;
                UpdatePath();
                OnPropertyChanged("FillColor");
            }
        }

        NamedColor _strokeColor;
        public NamedColor StrokeColor
        {
            get { return _strokeColor; }
            set
            {
                if (null == value)
                {
                    //throw new ArgumentNullException("value");
                }
                _strokeColor = value;
                UpdatePath();
                OnPropertyChanged("StrokeColor");
            }
        }

        double _strokeThickness = 1;
        public double StrokeThickness
        {
            get { return _strokeThickness; }
            set
            {
                _strokeThickness = value;
                UpdatePath();
                OnPropertyChanged("StrokeThickness");
            }
        }

        double _scale = 1;
        public double Scale
        {
            get { return _scale; }
            set
            {
                _scale = value;
                UpdatePath();
                OnPropertyChanged("Scale");
            }
        }

        private System.Windows.Rect _pathActualBounds;
        public System.Windows.Rect PathActualBounds
        {
            get { return _pathActualBounds; }
            protected set
            {
                _pathActualBounds = value;
                OnPropertyChanged("PathActualBounds");
            }
        }

        /*
        private Transform _renderTransform = new ScaleTransform(1, 1);
        public Transform RenderTransform
        {
            get { return _renderTransform; }
            set {
                _renderTransform = value;
                OnPropertyChanged("RenderTransform");
            }
        }
        */

        private Brush GetFillBrush()
        {
            if (null == FillColor)
            {
                return Brushes.Transparent;
            }
            return FillColor.Brush;
        }

        private Brush GetStrokeBrush()
        {
            if (null == StrokeColor)
            {
                return Brushes.Black;
            }
            return StrokeColor.Brush;
        }

        protected ICommand CreatePathCommand()
        {
            var cmd = new RoutedUICommand("DisplayPath", "Display Path", typeof(MainWindow));

            var binding = new CommandBinding(cmd,
                (s, e) =>
                {
                    UpdatePath();
                },
                (s, e) =>
                {
                    e.CanExecute = !String.IsNullOrWhiteSpace(PathText);
                    e.Handled = true;
                });

            CommandManager.RegisterClassCommandBinding(typeof(MainWindow), binding);

            return cmd;
        }

        private ICommand _displayPathCommand;
        public ICommand DisplayPathCommand
        {
            get
            {
                if (null == _displayPathCommand)
                {
                    _displayPathCommand = CreatePathCommand();
                }

                return _displayPathCommand;
            }
        }
        #endregion Public Properties

        #region Public Methods
        public void LoadFile(string fileName)
        {
            try
            {
                PathText = System.IO.File.ReadAllText(fileName);

                AppTitle = System.IO.Path.GetFileName(fileName) + " - PathMaker";

                UpdatePath();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(App.Current.MainWindow, "Error loading path: " + ex.Message, "PathMaker",
                                               System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        //  http://stackoverflow.com/a/1918890/424129
        public static Size GetScreenDPI(Visual vis)
        {
            PresentationSource source = PresentationSource.FromVisual(vis);

            double dpiX = 0, dpiY = 0;

            if (source != null)
            {
                dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
            }

            return new Size(dpiX, dpiY);
        }

        public BitmapSource CreateImage(Transform tfm)
        {
            var path = CreatePath();

            var rcBounds = path.Data.GetRenderBounds(GetPen());

            if (null != tfm)
            {
                path.RenderTransform = tfm;
                rcBounds = tfm.TransformBounds(rcBounds);
            }

            path.Measure(rcBounds.Size);
            path.Arrange(rcBounds);

            int pxWidth = (int)Math.Round(rcBounds.Width);
            int pxHeight = (int)Math.Round(rcBounds.Height);

            RenderTargetBitmap rtb = new RenderTargetBitmap(pxWidth, pxHeight, 96, 96, PixelFormats.Pbgra32);

            rtb.Render(path);

            /*
            var canvas = new System.Windows.Controls.Canvas();

            canvas.Width = pxWidth;
            canvas.Height = pxHeight;
            canvas.Margin = new System.Windows.Thickness(0);
            canvas.Background = Brushes.Transparent;

            canvas.Measure(new Size(canvas.Width, canvas.Height));
            canvas.Arrange(new Rect(new Size(canvas.Width, canvas.Height)));
            canvas.UpdateLayout();

            canvas.Children.Add(path);

            canvas.RenderTransform = tfm;

            rtb.Render(canvas);
            */

            return BitmapFrame.Create(rtb);
        }

        public Pen GetPen()
        {
            return new Pen(GetStrokeBrush(), StrokeThickness);
        }

        public void UpdatePath()
        {
            PathErrorText = "";
            var path = CreatePath();
            if (null != path)
            {
                Path = path;
                PathActualBounds = Path.Data.GetRenderBounds(GetPen());
            }
        }

        public Path CreatePath()
        {
            try
            {
                var path = new Path()
                {
                    Data = Geometry.Parse(PathText),
                    Fill = GetFillBrush(),
                    Stroke = GetStrokeBrush(),
                    StrokeThickness = StrokeThickness
                };

                /*
                var tfmScale = new ScaleTransform(Scale, Scale, 0, 0);
                var tfmOffset = new TranslateTransform(-(rect.Left + (rect.Width / 2)), -(rect.Top + (rect.Height / 2)));

                var tfmG = new TransformGroup();

                tfmG.Children.Add(tfmOffset);
                tfmG.Children.Add(tfmScale);

                RenderTransform = tfmG;
                */

                /*
                var tfmScale = new ScaleTransform(Scale, Scale, 0, 0);
                var tfmG = new TransformGroup();
                tfmG.Children.Add(tfmScale);
                tfmG.Children.Add(tfmTrans);
                */

                var rect = path.Data.GetRenderBounds(GetPen());

                var tfmTrans = new TranslateTransform(Math.Abs(rect.Left), Math.Abs(rect.Top));

                //path.RenderTransform = tfmTrans;

                return path;
            }
            catch (Exception ex)
            {
                PathErrorText = ex.Message;
            }

            return null;
        }

        internal void LoadSettings(PathMaker.Properties.Settings settings)
        {
            StrokeColor = NamedColors.Where(nc => nc.Color == settings.StrokeColor).FirstOrDefault();
            FillColor = NamedColors.Where(nc => nc.Color == settings.FillColor).FirstOrDefault();
            StrokeThickness = settings.StrokeThickness;
            Scale = settings.Scale;

            if (Math.Abs(((double)(int)Math.Abs(Scale)) - Math.Abs(Scale)) < 0.0001)
            {
                Scale = (int)Scale;
            }

        }

        internal void SaveSettings(PathMaker.Properties.Settings settings)
        {
            settings.StrokeColor = StrokeColor.Color;
            settings.FillColor = FillColor.Color;
            settings.StrokeThickness = StrokeThickness;
            settings.Scale = Scale;
        }
        #endregion Public Methods

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(String propName)
        {
            var handler = PropertyChanged;

            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propName));
            }
        }
        #endregion INotifyPropertyChanged

        public void Dispose()
        {
            for (int i = 0; i < _namedColors.Count; ++i)
            {
                _namedColors[i].Dispose();
                _namedColors[i] = null;
            }
        }
    }
}