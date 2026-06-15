using Siemens.Simatic.Simulation.Runtime;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CoSimulationPlcSimAdv
{
    public class Valve
    {
        public CoSimulationPlcSimAdv.App App { get; set; }
        public Simulation Simulation { get; set; }

        public Valve(string tagName, bool nc = true, string qualityBitName = "_QB", int time = 4000, bool qualityBit = true, bool mv = false, string nameCtrl = "CTRL", string nameFbOpen = "FB_OPN", string nameFbClose = "FB_CLS")
        {
            Closed = false;
            Initialized = false;

            var normalizedTagName = tagName.Replace("-", "_");
            NameCtrl = nameCtrl + "_" + normalizedTagName;
            NameFbOpen = nameFbOpen + "_" + normalizedTagName;
            NameFbClose = nameFbClose + "_" + normalizedTagName;
            NC = nc;
            Opened = false;
            Ctrl = false;
            Time = time;
            TimeChanged = 0;
            QualityBit = qualityBit;
            QualityBitName = qualityBitName;
            MV = mv;

            App = Application.Current as CoSimulationPlcSimAdv.App;
            App.Loop += OnLoop;

            var wnd = App.MainWindow as CoSimulationPlcSimAdv.Views.MainWindow;
            var grid = wnd.Content as Grid;
            var name = normalizedTagName;

            SimFbButton = grid.FindName(name + "_SimFbButton") as ToggleButton;
            if (SimFbButton != null) SimFbButton.Click += OnSimFbButtonClick;

            SimFbOpenButton = grid.FindName(name + "_SimFbOpenButton") as ToggleButton;
            if (SimFbOpenButton != null) SimFbOpenButton.Click += OnSimFbOpenButtonClick;

            SimFbCloseButton = grid.FindName(name + "_SimFbCloseButton") as ToggleButton;
            if (SimFbCloseButton != null) SimFbCloseButton.Click += OnSimFbCloseButtonClick;

            ErrorFbOpenButton = grid.FindName(name + qualityBitName + "_ErrorFbOpenButton") as ToggleButton;
            if (ErrorFbOpenButton != null) ErrorFbOpenButton.Click += OnErrorFbOpenButtonClick;

            ErrorFbCloseButton = grid.FindName(name + qualityBitName + "_ErrorFbCloseButton") as ToggleButton;
            if (ErrorFbCloseButton != null) ErrorFbCloseButton.Click += OnErrorFbCloseButtonClick;

            MVCtrlButton = grid.FindName(name + "_MVCtrlButton") as ToggleButton;
            if (MVCtrlButton != null) MVCtrlButton.Click += OnManualValveButtonClick;
        }

        public void OnLoop(IInstance instance, bool init = false)
        {
            if (init)
            {
                Initialized = false;
            }

            if (NameCtrl == "") return;

            if (SimFb)
            {
                PlcIo.TryWriteBool(instance, NameFbOpen + QualityBitName, !ErrorFbOpen, App, "Valve manual open quality write");
                PlcIo.TryWriteBool(instance, NameFbClose + QualityBitName, !ErrorFbClose, App, "Valve manual close quality write");
                PlcIo.TryWriteBool(instance, NameFbOpen, SimFbOpen, App, "Valve manual open feedback write");
                PlcIo.TryWriteBool(instance, NameFbClose, SimFbClose, App, "Valve manual close feedback write");

                TimeChanged = 0;
                AfterSim = true;
                return;
            }

            bool ctrl = MVCtrl;

            if (!MV)
            {
                if (!PlcIo.TryReadBool(instance, NameCtrl, out ctrl, App, "Valve control read"))
                {
                    return;
                }
            }

            bool opened = ctrl;
            bool closed = !ctrl;

            if (!ErrorFbOpen && !ErrorFbClose && AfterSim)
            {
                Initialized = false;
                AfterSim = false;
            }

            if (NC)
            {
                opened = !ctrl;
                closed = ctrl;
            }
            if (!Initialized)
            {
                Ctrl = ctrl;
                if (NameFbOpen != "")
                {
                    PlcIo.TryWriteBool(instance, NameFbOpen, opened, App, "Valve open feedback init write");
                }
                if (NameFbClose != "")
                {
                    PlcIo.TryWriteBool(instance, NameFbClose, closed, App, "Valve close feedback init write");
                }

                if (QualityBit)
                {
                    if (NameFbClose != "")
                    {
                        PlcIo.TryWriteBool(instance, NameFbClose + QualityBitName, true, App, "Valve close quality init write");
                    }
                    if (NameFbOpen != "")
                    {
                        PlcIo.TryWriteBool(instance, NameFbOpen + QualityBitName, true, App, "Valve open quality init write");
                    }
                }
                Initialized = true;
                return;
            }

            if (!ErrorFbOpen & !ErrorFbClose)
            {
                if (TimeChanged != 0)
                {
                    int time = Environment.TickCount & Int32.MaxValue;
                    if (time > TimeChanged + Time)
                    {
                        if (NameFbOpen != "")
                        {
                            PlcIo.TryWriteBool(instance, NameFbOpen, opened, App, "Valve open feedback write");
                        }
                        if (NameFbClose != "")
                        {
                            PlcIo.TryWriteBool(instance, NameFbClose, closed, App, "Valve close feedback write");
                        }
                        TimeChanged = 0;
                    }
                }
            }
            else
            {
                PlcIo.TryWriteBool(instance, NameFbOpen + QualityBitName, !ErrorFbOpen, App, "Valve open quality write");
                PlcIo.TryWriteBool(instance, NameFbClose + QualityBitName, !ErrorFbClose, App, "Valve close quality write");

                TimeChanged = 0;
                AfterSim = true;
                return;
            }

            if (ctrl != Ctrl)
            {
                Ctrl = ctrl;
                TimeChanged = Environment.TickCount & Int32.MaxValue;
            }
        }

        private void OnManualValveButtonClick(object sender, RoutedEventArgs args)
        {
            MVCtrl = !MVCtrl;
        }

        private void OnSimFbButtonClick(object sender, RoutedEventArgs args)
        {
            SimFb = !SimFb;

            if (!SimFb)
            {
                SimFbOpen = false;
                SimFbClose = false;
            }
        }

        private void OnSimFbOpenButtonClick(object sender, RoutedEventArgs args)
        {
            SimFb = true;
            SimFbOpen = !SimFbOpen;
            SimFbClose = false;
        }

        private void OnSimFbCloseButtonClick(object sender, RoutedEventArgs args)
        {
            SimFb = true;
            SimFbClose = !SimFbClose;
            SimFbOpen = false;
        }

        private void OnErrorFbCloseButtonClick(object sender, RoutedEventArgs args)
        {
            ErrorFbClose = !ErrorFbClose;
        }

        private void OnErrorFbOpenButtonClick(object sender, RoutedEventArgs args)
        {
            ErrorFbOpen = !ErrorFbOpen;
        }

        public bool Closed;
        public bool Initialized;
        public String NameFbClose;
        public String NameFbOpen;
        public String NameCtrl;
        public String QualityBitName;
        public bool NC;
        public bool Opened;
        public bool Ctrl;
        public int Time;
        private int TimeChanged;
        public bool QualityBit;
        public bool MV;
        public bool MVCtrl;
        public bool SimFb;
        public bool SimFbOpen;
        public bool SimFbClose;
        public bool ErrorFbOpen;
        public bool ErrorFbClose;
        private bool AfterSim;
        public ToggleButton MVCtrlButton;
        public ToggleButton SimFbOpenButton;
        public ToggleButton SimFbCloseButton;
        public ToggleButton ErrorFbOpenButton;
        public ToggleButton ErrorFbCloseButton;
        public ToggleButton SimFbButton;
    }
}
