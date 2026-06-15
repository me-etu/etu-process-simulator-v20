using Siemens.Simatic.Simulation.Runtime;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CoSimulationPlcSimAdv
{
   
    public class Digital
    {
        public CoSimulationPlcSimAdv.App App { get; set; }
        public Simulation Simulation { get; set; }

        public Digital(String actValueName = "", bool value = false, bool qualityBit = true, bool nc = true)
        {
            Name = actValueName;
            Value = value;
            QualityBit = qualityBit;
            NC = nc;
            ActValueManual = false;
            MValue = false;

            App = Application.Current as CoSimulationPlcSimAdv.App;

            App.Loop += OnLoop;

            var wnd = App.MainWindow as CoSimulationPlcSimAdv.Views.MainWindow;
            var grid = wnd.Content as Grid;
            var name = actValueName.Replace("IN_", "").Replace("E_", "").Replace("-", "_");

            ActValueButton = grid.FindName(name + "_ActValueButton") as ToggleButton;
            if (ActValueButton != null) ActValueButton.Click += OnActValueButtonClick;
        }
      
        public void OnLoop(IInstance instance, bool init = false)
        {
            if (init) Initialized = false;

            if (Name == "") return;

            if (QualityBit)
            {
                PlcIo.TryWriteBool(instance, Name + "_QB", true, App, "Digital quality write");
            }
            Initialized = true;

            if (NC)
            {
                if(!ActValueManual)
                {
                    PlcIo.TryWriteBool(instance, Name, Value, App, "Digital write");
                }
                if (ActValueManual)
                {
                    PlcIo.TryWriteBool(instance, Name, MValue, App, "Digital manual write");
                }
                return;
            }
            if (!ActValueManual)
            {
                PlcIo.TryWriteBool(instance, Name, !Value, App, "Digital write");
            }
            if (ActValueManual)
            {
                PlcIo.TryWriteBool(instance, Name, !MValue, App, "Digital manual write");
            }
        }

        private void OnActValueButtonClick(object sender, RoutedEventArgs args)
        {
            var isChecked = ActValueButton?.IsChecked == true;
            ActValueManual = isChecked;
            MValue = isChecked ? !Value : Value;
        }

        public string Name;
        public bool Value;
        public bool QualityBit;
        public bool Initialized;
        public ToggleButton ActValueButton;
        public bool NC;
        public bool ActValueManual;
        public bool MValue;
    }
}
