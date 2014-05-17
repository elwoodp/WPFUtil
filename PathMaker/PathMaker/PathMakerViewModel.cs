using System;
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
using System.Xml;

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

#if DEBUG
            PathText = CornerAndLeftBevelBezier12x22;
#endif
        }

        public static readonly String CornerAndLeftBevelBezier12x22 = "M -2.5,26 \nL -2.5,24 A 3,3 0 0 1 0,21.5 \nC 4,21.5 4,0 12,0.5 ";
        public static readonly String LeftBevelBezier12x22 = "M 0,21.5 C 4,21.5 4,0 12,0.5";
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
                if (!String.Equals(_pathText, value))
                {
                    IsDirty = true;
                }
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
        public Transform OffsetTransform
        {
            get { return _offsetTransform; }
            set
            {
                _offsetTransform = value;
                OnPropertyChanged("OffsetTransform");
            }
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

        private bool _isDirty = false;
        public bool IsDirty
        {
            get { return _isDirty; }
            set
            {
                bool bChanging = _isDirty != value;
                _isDirty = value;
                if (bChanging)
                {
                    UpdateAppTitle();
                }
                OnPropertyChanged("IsDirty");
            }
        }

        private string _fileName;
        public String FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                UpdateAppTitle();
                OnPropertyChanged("FileName");
            }
        }
        private Geometry _overlayLines;
        public Geometry OverlayLines
        {
            get { return _overlayLines; }
            set
            {
                _overlayLines = value;
                OnPropertyChanged("OverlayLines");
            }
        }

        private Geometry _overlayBoxes;
        public Geometry OverlayBoxes
        {
            get { return _overlayBoxes; }
            set
            {
                _overlayBoxes = value;
                OnPropertyChanged("OverlayBoxes");
            }
        }
        #endregion Public Properties

        #region Graphics Methods
        protected PathFigure MakeGrabBox(PointRef ptref)
        {
            double width = 8 / Scale;

            var ptCenter = ptref.Get();

            var ptStart = new System.Windows.Point(ptCenter.X - (width / 2), ptCenter.Y - (width / 2));

            var points = new System.Windows.Point[] {
                new System.Windows.Point( ptCenter.X - (width / 2), ptCenter.Y + (width / 2)),
                new System.Windows.Point( ptCenter.X + (width / 2), ptCenter.Y + (width / 2)),
                new System.Windows.Point( ptCenter.X + (width / 2), ptCenter.Y - (width / 2))
            };

            PathSegment[] segments = { new PolyLineSegment(points, true) };

            var pfig = new PathFigure(ptStart, segments, true);

            pfig.Changed += (s, e) =>
            {
                ptref.Set(new System.Windows.Point(pfig.StartPoint.X + (width / 2), pfig.StartPoint.Y + (width / 2)));
                OnPropertyChanged("OverlayBoxes");
                OnPropertyChanged("OverlayLines");
            };

            return pfig;
        }

        protected void UpdateGridGeometry()
        {
            double spacing = 1;
            if (Scale < 5)
                spacing = Math.Round(10 / Scale);

            var rect = PathActualBounds;

            List<PathFigure> grid = new List<PathFigure>();
            PathSegment[] end = new PathSegment[1];

            double xMin = Math.Floor(rect.Left) - 1;
            double yMin = Math.Floor(rect.Top) - 1;
            double xMax = Math.Ceiling(rect.Right) + 1;
            double yMax = Math.Ceiling(rect.Bottom) + 1;

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
            triangle[1] = new LineSegment(new System.Windows.Point(lenMarker / 2, yMax + lenMarker), true);

            gridZero.Add(new PathFigure(new System.Windows.Point(-(lenMarker / 2), yMax + lenMarker), triangle, true));

            triangle[0] = new LineSegment(new System.Windows.Point(xMax, 0), true);
            triangle[1] = new LineSegment(new System.Windows.Point(xMax + lenMarker, lenMarker / 2), true);

            gridZero.Add(new PathFigure(new System.Windows.Point(xMax + lenMarker, -(lenMarker / 2)), triangle, true));

            GridStrokeThickness = 1.0 / Scale;

            GridZeroGeometry = new PathGeometry(gridZero);
            GridGeometry = new PathGeometry(grid);
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

        #endregion Graphics Methods

        #region Public Methods
        public void LoadFile(string fileName)
        {
            try
            {
                var xml = new XmlDocument();
                xml.LoadXml(System.IO.File.ReadAllText(fileName));

                var nodes = xml.GetElementsByTagName("Path");

                if ( nodes.Count == 0 )
                {
                    throw new Exception("File contains no Path tag.");
                }

                var tagPath = nodes[0];
 
                var attrData = tagPath.Attributes.GetNamedItem("Data");
                var attrStrokeThickness = tagPath.Attributes.GetNamedItem("StrokeThickness");
                var attrStroke = tagPath.Attributes.GetNamedItem("Stroke");
                var attrFill = tagPath.Attributes.GetNamedItem("Fill");

                if (null == attrData)
                {
                    throw new Exception("Path tag contains no Data attribute.");
                }

                StrokeThickness = 1;
                if (null != attrStrokeThickness)
                {
                    double th;
                    if ( double.TryParse(attrStrokeThickness.Value, out th))
                    {
                        StrokeThickness = th;
                    }
                }

                StrokeColor = NamedColors.Where(nc => nc.Color == Colors.Black).FirstOrDefault();
                if (null != attrStroke)
                {
                    StrokeColor = NamedColors.Where(nc => nc.Name == attrStroke.Value).FirstOrDefault() ?? StrokeColor;
                }

                FillColor = NamedColors.Where(nc => nc.Color == Colors.Transparent).FirstOrDefault();
                if (null != attrFill)
                {
                    FillColor = NamedColors.Where(nc => nc.Name == attrFill.Value).FirstOrDefault() ?? FillColor;
                }

                IsDirty = false;

                FileName = fileName;

                UpdatePath();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(App.Current.MainWindow, "Error loading path: " + ex.Message, "PathMaker",
                                               System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        private void UpdateAppTitle()
        {
            var dirtymark = (IsDirty) ? "*" : "";
            AppTitle = System.IO.Path.GetFileName(FileName) + dirtymark  + " - PathMaker";
        }

        public String ToXAML(bool bIncludeGrid)
        {
            String xml = "";
            
            xml += String.Format("<Path Data=\"{0}\" StrokeThickness=\"{1}\" Stroke=\"{2}\" Fill=\"{3}\" />",
                                 PathText, StrokeThickness, StrokeColor.Name, FillColor.Name);

            return xml;
        }

        public void SaveFile(String fileName)
        {
            if (String.IsNullOrEmpty(fileName))
            {
                fileName = FileName;
            }

            System.IO.File.WriteAllText(fileName, ToXAML(false));

            if (!System.IO.Path.Equals(fileName, fileName))
            {
                FileName = fileName;
            }
            
            IsDirty = false;
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
            var path = new Path()
            {
                Data = Geometry.Parse(PathText),
                StrokeThickness = StrokeThickness,
                Stroke = StrokeColor.Brush,
                Fill = FillColor.Brush
            };

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

            return BitmapFrame.Create(rtb);
        }

        public Pen GetPen()
        {
            return new Pen(GetStrokeBrush(), StrokeThickness);
        }

        /*public void UpdatePath()
        {
            try
            {
                PathErrorText = "";

                var geometry = PathGeometry.Parse(PathText);

                var rect = geometry.GetRenderBounds(new Pen(GetStrokeBrush(), StrokeThickness));

                double xMin = Math.Abs(Math.Floor(rect.Left) - 1);
                double yMin = Math.Abs(Math.Floor(rect.Top) - 1);

                OffsetTransform = new TranslateTransform(xMin, yMin);
                RequiredOffset = new System.Windows.Size(xMin, yMin);

                PathActualBounds = rect;

                Geometry = geometry;
            }
            catch (Exception ex)
            {
                PathErrorText = ex.Message;
            }
        }*/
        public void UpdatePath()
        {
            try
            {
                PathErrorText = "";

                var pathGeometry = new PathGeometry(PathFigureCollection.Parse(PathText));

                List<PathFigure> overlayLines = new List<PathFigure>();
                List<PathFigure> overlayBoxes = new List<PathFigure>();
                PathSegment[] end = new PathSegment[1];

                System.Windows.Point ptStart;

                pathGeometry.Changed += (s, e) =>
                {
                    _pathText = pathGeometry.ToString();
                    OnPropertyChanged("PathText");
                };

                foreach (var pfLoop in pathGeometry.Figures)
                {
                    PathFigure pf = pfLoop;

                    //  XXX  For Beziers, if the previous segment's StartPoint changed, that 
                    //  will correctly change the bezier segment's start point, but not its controlLine startpoint
                    ptStart = pf.StartPoint;

                    PathSegment psPrev = null;

                    overlayBoxes.Add(MakeGrabBox(new PointRef(() => pf.StartPoint, (pt) => pf.StartPoint = pt)));

                    foreach (var seg in pf.Segments)
                    {
                        if (seg is BezierSegment)
                        {
                            var segBez = seg as BezierSegment;

                            //  Two control point lines
                            //      1. Start to Point1
                            //      2. End (Point3) to Point2
                            end[0] = new LineSegment(segBez.Point1, true);
                            var cp1Line = new PathFigure(ptStart, end, false);
                            overlayLines.Add(cp1Line);

                            end[0] = new LineSegment(segBez.Point2, true);
                            var cp2Line = new PathFigure(segBez.Point3, end, false);
                            overlayLines.Add(cp2Line);

                            segBez.Changed += (s, e) =>
                            {
                                (cp1Line.Segments[0] as LineSegment).Point = segBez.Point1;
                                (cp2Line.Segments[0] as LineSegment).Point = segBez.Point2;
                                cp2Line.StartPoint = segBez.Point3;
                            };

                            if (psPrev != null)
                            {
                                var psPrevLocal = psPrev;
                                psPrevLocal.Changed += (s, e) =>
                                {
                                    cp1Line.StartPoint = psPrevLocal.GetEndPoint();
                                };
                            }
                            else
                            {
                                pf.Changed += (s, e) => cp1Line.StartPoint = pf.StartPoint;

                                //overlayBoxes.Add(MakeGrabBox(new PointRef(() => pf.StartPoint, (pt) => cp1Line.StartPoint = pf.StartPoint = pt)));
                            }
                            overlayBoxes.Add(MakeGrabBox(new PointRef(() => segBez.Point1, (pt) => segBez.Point1 = pt)));
                            overlayBoxes.Add(MakeGrabBox(new PointRef(() => segBez.Point2, (pt) => segBez.Point2 = pt)));
                            overlayBoxes.Add(MakeGrabBox(new PointRef(() => segBez.Point3, (pt) => segBez.Point3 = pt)));
                        }
                        else if (seg is LineSegment)
                        {
                            //  Control end point
                            //  xxx Multiple line segments get converted to PolyLineSegment
                            var segLine = (seg as LineSegment);
                            //overlayBoxes.Add(MakeGrabBox(new PointRef(() => pf.StartPoint, (pt) => pf.StartPoint = pt)));
                            overlayBoxes.Add(MakeGrabBox(new PointRef(() => segLine.Point, (pt) => segLine.Point = pt)));
                        }
                        else if (seg is ArcSegment)
                        {
                            var segArc = (seg as ArcSegment);

                            //  Control what?
                            //  Y'know, we can map a control point onto anything. 

                            if (psPrev != null)
                            {
                                //  xxx ???
                                var psPrevLocal = psPrev;
                                psPrevLocal.Changed += (s, e) =>
                                {
                                    //pf.StartPoint = psPrevLocal.GetEndPoint();
                                };
                            }

                            //overlayBoxes.Add(MakeGrabBox(new PointRef(() => pf.StartPoint, (pt) => pf.StartPoint = pt)));
                            overlayBoxes.Add(MakeGrabBox(new PointRef(() => segArc.Point, (pt) => segArc.Point = pt)));
                        }
                        else if (seg is QuadraticBezierSegment)
                        {
                            //  One control point line
                            var segQBS = seg as QuadraticBezierSegment;

                            end[0] = new LineSegment(segQBS.Point1, true);
                            var cp1Line = new PathFigure(ptStart, end, false);
                            overlayLines.Add(cp1Line);

                            segQBS.Changed += (s, e) =>
                            {
                                (cp1Line.Segments[0] as LineSegment).Point = segQBS.Point1;
                            };

                            if (psPrev != null)
                            {
                                var psPrevLocal = psPrev;
                                psPrevLocal.Changed += (s, e) =>
                                {
                                    cp1Line.StartPoint = psPrevLocal.GetEndPoint();
                                };
                            }
                            else
                            {
                                pf.Changed += (s, e) => cp1Line.StartPoint = pf.StartPoint;
                                //overlayBoxes.Add(MakeGrabBox(new PointRef(() => pf.StartPoint, (pt) => cp1Line.StartPoint = pf.StartPoint = pt)));
                            }

                            overlayBoxes.Add(MakeGrabBox(new PointRef(() => segQBS.Point1, (pt) => segQBS.Point1 = pt)));
                            overlayBoxes.Add(MakeGrabBox(new PointRef(() => segQBS.Point2, (pt) => segQBS.Point2 = pt)));
                        }
                        else if (seg is PolyBezierSegment)
                        {
                            var segPBS = (seg as PolyBezierSegment);
                        }
                        else if (seg is PolyLineSegment)
                        {
                            var segPLS = (seg as PolyLineSegment);

                            overlayBoxes.Add(MakeGrabBox(new PointRef(() => pf.StartPoint, (pt) => pf.StartPoint = pt)));
                            for (int i = 0; i < segPLS.Points.Count; ++i)
                            {
                                //  Don't hand a loop variable to a closure!
                                var idx = i;
                                overlayBoxes.Add(MakeGrabBox(new PointRef(() => segPLS.Points[idx], (pt) => segPLS.Points[idx] = pt)));
                            }
                        }
                        else if (seg is PolyQuadraticBezierSegment)
                        {
                            var segPQBS = (seg as PolyQuadraticBezierSegment);
                        }

                        ptStart = seg.GetEndPoint();
                        psPrev = seg;
                    }

                    psPrev = null;
                }

                OverlayLines = new PathGeometry(overlayLines);
                OverlayBoxes = new PathGeometry(overlayBoxes);

                var geometry = pathGeometry; //PathGeometry.Parse(PathText);

                var rect = geometry.GetRenderBounds(new Pen(GetStrokeBrush(), StrokeThickness));

                double xMin = Math.Abs(Math.Floor(rect.Left) - 1);
                double yMin = Math.Abs(Math.Floor(rect.Top) - 1);

                OffsetTransform = new TranslateTransform(xMin, yMin);
                RequiredOffset = new System.Windows.Size(xMin, yMin);

                PathActualBounds = rect;

                Geometry = geometry;

                //Path = path;
            }
            catch (Exception ex)
            {
                PathErrorText = ex.Message;
            }
        }

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

        #region Commands
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
            get { return _displayPathCommand ?? ( _displayPathCommand = CreatePathCommand() ); }
        }

        private ICommand _newPathCommand;
        public ICommand NewPathCommand
        {
            get { return _newPathCommand ?? ( _newPathCommand = CreateNewPathCommand() ); }
        }
        protected ICommand CreateNewPathCommand()
        {
            var cmd = new RoutedUICommand("DisplayPath", "Display Path", typeof(MainWindow));

            var binding = new CommandBinding(cmd,
                (s, e) =>
                {
                    Clear();
                },
                (s, e) =>
                {
                    e.CanExecute = true;
                    e.Handled = true;
                });

            CommandManager.RegisterClassCommandBinding(typeof(MainWindow), binding);

            return cmd;
        }

        private static int _ct = 0;
        public void Clear()
        {
            PathText = "";
            UpdatePath();
            IsDirty = false;
            FileName = "New Path " + ++_ct;
        }
        #endregion Commands
    }

    public class PointRef
    {
        public PointRef(Func<System.Windows.Point> get, Action<System.Windows.Point> set)
        {
            Get = get;
            Set = set;
        }
        public readonly Func<System.Windows.Point> Get;
        public readonly Action<System.Windows.Point> Set;
    }

    public static class PathSegmentExtensions
    {
        public static System.Windows.Point GetEndPoint(this PathSegment seg)
        {
            if (seg == null)
            {
                throw new ArgumentNullException("seg");
            }

            if (seg is BezierSegment)
            {
                return (seg as BezierSegment).Point3;
            }
            else if (seg is LineSegment)
            {
                return (seg as LineSegment).Point;
            }
            else if (seg is ArcSegment)
            {
                return (seg as ArcSegment).Point;
            }
            else if (seg is QuadraticBezierSegment)
            {
                return (seg as QuadraticBezierSegment).Point2;
            }
            else if (seg is PolyBezierSegment)
            {
                return (seg as PolyBezierSegment).Points.LastOrDefault();
            }
            else if (seg is PolyLineSegment)
            {
                return (seg as PolyLineSegment).Points.LastOrDefault();
            }
            else if (seg is PolyQuadraticBezierSegment)
            {
                return (seg as PolyQuadraticBezierSegment).Points.LastOrDefault();
            }
            else
            {
                throw new Exception("Unaccomodated path segment type " + seg.GetType().Name);
            }
        }
    }
}
