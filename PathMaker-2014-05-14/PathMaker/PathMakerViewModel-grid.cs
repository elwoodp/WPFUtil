using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
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
        }

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
        public IEnumerable<String> ColorNames
        {
            get { return _namedColors.Select(nc => nc.Name); }
        }

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

        double _gridStrokeThickness = 0.1;
        public double GridStrokeThickness
        {
            get { return _gridStrokeThickness; }
            protected set
            {
                _gridStrokeThickness = value;
                OnPropertyChanged("GridStrokeThickness");
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

        private bool _isGridVisible = false;
        public bool IsGridVisible
        {
            get { return _isGridVisible; }
            set
            {
                _isGridVisible = value;
                OnPropertyChanged("IsGridVisible");
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

        private System.Windows.Size _requiredOffset = new System.Windows.Size();
        public System.Windows.Size RequiredOffset
        {
            get { return _requiredOffset; }
            set
            {
                _requiredOffset = value;
                OnPropertyChanged("RequiredOffset");
            }
        }

        private Transform _offsetTransform = null;
        public Transform OffsetTransform {
            get { return _offsetTransform; }
            set
            {
                _offsetTransform = value;
                OnPropertyChanged("OffsetTransform");
            }
        }

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

        private Geometry _geometry;
        public Geometry Geometry
        {
            get { return _geometry; }
            set
            {
                _geometry = value;
                UpdateGridGeometry();
                OnPropertyChanged("Geometry");
            }
        }

        private Geometry _gridGeometry;
        public Geometry GridGeometry
        {
            get { return _gridGeometry; }
            protected set
            {
                _gridGeometry = value;
                OnPropertyChanged("GridGeometry");
            }
        }

        private Geometry _gridZeroGeometry;
        public Geometry GridZeroGeometry
        {
            get { return _gridZeroGeometry; }
            protected set
            {
                _gridZeroGeometry = value;
                OnPropertyChanged("GridZeroGeometry");
            }
        }

        protected void UpdateGridGeometry()
        {
            double spacing = 1;
            if (Scale < 5)
                spacing = Math.Round(10 / Scale);

            var rect = PathActualBounds;

            List<PathFigure> grid = new List<PathFigure>();
            PathSegment[] end = new PathSegment[1];

            double xMin = Math.Floor(rect.Left - 1);
            double yMin = Math.Floor(rect.Top - 1);
            double xMax = Math.Ceiling(rect.Right + 1);
            double yMax = Math.Ceiling(rect.Bottom + 1);

            xMin -= xMin % spacing;
            yMin -= yMin % spacing;

            for (double x = xMin; x <= xMax; x += spacing)
            {
                end[0] = new LineSegment(new System.Windows.Point(x, yMax), true);
                grid.Add(new PathFigure(new System.Windows.Point(x, yMin), end, false));
            }

            for (double y = yMin; y <= yMax; y += spacing)
            {
                end[0] = new LineSegment(new System.Windows.Point(xMax, y), true);
                grid.Add(new PathFigure(new System.Windows.Point(xMin, y), end, false));
            }

            List<PathFigure> gridZero = new List<PathFigure>();

            end[0] = new LineSegment(new System.Windows.Point(0, yMax), true);
            gridZero.Add(new PathFigure(new System.Windows.Point(0, yMin), end, false));
            end[0] = new LineSegment(new System.Windows.Point(xMax, 0), true);
            gridZero.Add(new PathFigure(new System.Windows.Point(xMin, 0), end, false));

            double lenMarker = 10 / Scale;
            PathSegment[] triangle = new PathSegment[2];

            triangle[0] = new LineSegment(new System.Windows.Point(0, yMax), true);
            triangle[1] = new LineSegment(new System.Windows.Point(lenMarker/2, yMax + lenMarker), true);

            gridZero.Add(new PathFigure(new System.Windows.Point(-(lenMarker / 2), yMax + lenMarker), triangle, true));

            triangle[0] = new LineSegment(new System.Windows.Point(xMax, 0), true);
            triangle[1] = new LineSegment(new System.Windows.Point(xMax + lenMarker, lenMarker / 2), true);

            gridZero.Add(new PathFigure(new System.Windows.Point(xMax + lenMarker, -(lenMarker / 2)), triangle, true));

            GridStrokeThickness = 1.0 / Scale;

            GridZeroGeometry = new PathGeometry(gridZero);
            GridGeometry = new PathGeometry(grid);
        }

        protected void UpdatePath()
        {
            try
            {
                PathErrorText = "";

                /*var path = new Path()
                {
                    Data = Geometry.Parse(PathText),
                    Fill = GetFillBrush(),
                    Stroke = GetStrokeBrush(),
                    StrokeThickness = (int)StrokeThickness
                };*/
                var geometry = PathGeometry.Parse(PathText);

                var rect = geometry.GetRenderBounds(new Pen(GetStrokeBrush(), StrokeThickness));

                /*
                var tfmScale = new ScaleTransform(Scale, Scale, 0, 0);
                var tfmOffset = new TranslateTransform(-(rect.Left + (rect.Width / 2)), -(rect.Top + (rect.Height / 2)));

                var tfmG = new TransformGroup();

                tfmG.Children.Add(tfmOffset);
                tfmG.Children.Add(tfmScale);

                RenderTransform = tfmG;
                */

                OffsetTransform = new TranslateTransform(Math.Abs(rect.Left), Math.Abs(rect.Top));
                RequiredOffset = new System.Windows.Size(Math.Abs(rect.Left), Math.Abs(rect.Top));
                //geometry = new PathGeometry(geometry.Figures, FillRule.EvenOdd, tfm);

                PathActualBounds = rect;

                Geometry = geometry;

                //Path = path;
            }
            catch (Exception ex)
            {
                PathErrorText = ex.Message;
            }
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
        internal void LoadSettings(PathMaker.Properties.Settings settings)
        {
            StrokeColor = NamedColors.Where(nc => nc.Color == settings.StrokeColor).FirstOrDefault();
            FillColor = NamedColors.Where(nc => nc.Color == settings.FillColor).FirstOrDefault();
            StrokeThickness = settings.StrokeThickness;
            Scale = settings.Scale;
            IsGridVisible = settings.IsGridVisible;

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
            settings.IsGridVisible = IsGridVisible;
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
