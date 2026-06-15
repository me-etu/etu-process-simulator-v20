using System;
using System.Collections.Generic;
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
using CoSimulationPlcSimAdv.Models;
using Siemens.Simatic.Simulation.Runtime;
using Microsoft.VisualBasic;
using System.Globalization;


namespace CoSimulationPlcSimAdv.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>    

    public partial class MainWindow : Window 
    {

        public CoSimulationPlcSimAdv.App App { get; set; }
        public Simulation Simulation { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new ViewModels.MainWindowViewModel();
            this.Loaded += OnLoaded;
            App = Application.Current as CoSimulationPlcSimAdv.App;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            App.Initialize();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ViewModels.MainWindowViewModel Application = this.DataContext as ViewModels.MainWindowViewModel;

            if (Application?.virtualController != null)
            {
                Application.virtualController.Dispose();
            }
        }

        private void TimeFactor_Setpoint_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            try
            {
                App.Simulation.TimeFact = float.Parse(TimeFactor_Setpoint.Text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch {}
        }

    }
}
