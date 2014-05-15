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

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Path":
                    //PathContainer.UpdateLayout();
                    break;
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
                TopPanel.ItemHeight = Settings.TopPanelHeight;

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
                Settings.TopPanelHeight = TopPanel.ItemHeight;

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
                dValue = (int)dValue;
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
                int change = e.Delta / 120;

                dValue += change;

                dValue = Math.Max(0, dValue);

                tb.Text = dValue.ToString();
            }
            catch (Exception ex)
            {
                StatusBar.Items.Clear();
                StatusBar.Items.Add(ex.Message);
            }
        }

        private void UDoubleBox_TextChanged(object sender, TextChangedEventArgs e)
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

                double dValue = 1;

                if (!Double.TryParse(tb.Text, out dValue) || dValue < 0)
                {
                    dValue = Math.Max(0, dValue);

                    tb.Text = dValue.ToString();
                }
            }
            catch (Exception ex)
            {
                StatusBar.Items.Clear();
                StatusBar.Items.Add(ex.Message);
            }
        }
    }
}
