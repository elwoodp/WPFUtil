using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PathMaker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public String[] Args { get; protected set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Args = e.Args;
        }
    }
}
