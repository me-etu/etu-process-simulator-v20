using Siemens.Simatic.Simulation.Runtime;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;

namespace CoSimulationPlcSimAdv
{
    public class Motor
    {
        public CoSimulationPlcSimAdv.App App { get; set; }
        public Simulation Simulation { get; set; }

        public Motor(String name = "", String zSW = "", String actSp = "",  float minValue = 0, float maxValue = 100, short maxDig = 16384, short minDig = 0, short zSW_On = -1228, short zSW_Off = -1487, int time = 10000)
        {
            Name = name;
            ZSW = zSW;
            ZSW_On = zSW_On;
            ZSW_Off = zSW_Off;
            ActSp = actSp;

            if(ActSp != "")
            {
                Speed = new Analog("");
                Speed.MinDig = minDig;
                Speed.MaxDig = maxDig;
                Speed.MaxValue = maxValue;
                Speed.MinValue = minValue;
                Speed.Time = time;
                Speed.ActValueName = actSp;
                Speed.SetpointName = "";
            }
            

            App = Application.Current as CoSimulationPlcSimAdv.App;
            App.Loop += OnLoop;


            var wnd = App.MainWindow as CoSimulationPlcSimAdv.Views.MainWindow;
            var grid = wnd.Content as Grid;

            SimFbButton = grid.FindName(zSW + "_SimFbButton") as ToggleButton;
            if (SimFbButton != null) SimFbButton.Click += OnSimFbButtonClick;

            SimFbOnButton = grid.FindName(zSW + "_SimFbOnButton") as ToggleButton;
            if (SimFbOnButton != null) SimFbOnButton.Click += OnSimFbOnButtonClick;

        }
        public void OnLoop(IInstance instance, bool init = false)
        {
            if (Name == "" | ZSW  == "") return;

            try
            {
                Setpoint = instance.ReadFloat(Name + ".QSETPOINT");

                if (!SimFb)
                {
                    if (instance.ReadBool(Name + ".QSTARTING") | instance.ReadBool(Name + ".QRUN"))
                    {

                        instance.WriteInt16(ZSW, ZSW_On);
                        if (ActSp != "") Speed.Setpoint = Setpoint;
                        IsRun = true;

                    }
                    else
                    {
                        instance.WriteInt16(ZSW, ZSW_Off);
                        if (ActSp != "") Speed.Setpoint = 0;
                        IsRun = false;
                    }
                }
                else 
                {
                    if(SimFbOn)
                    {
                        instance.WriteInt16(ZSW, ZSW_On);
                        if (ActSp != "") Speed.Setpoint = Setpoint;
                        IsRun = true;
                    }
                    else
                    {
                        instance.WriteInt16(ZSW, ZSW_Off);
                        if (ActSp != "") Speed.Setpoint = 0;
                        IsRun = false;
                    }
                }
            }
            catch {}
            
        }

        private void OnSimFbButtonClick(object sender, RoutedEventArgs args)
        {
            SimFb = !SimFb;
        }

        private void OnSimFbOnButtonClick(object sender, RoutedEventArgs args)
        {
            SimFbOn = !SimFbOn;
        }


        private short ZSW_On;
        private short ZSW_Off;
        private String Name;
        private String ZSW;
        private String ActSp;
        private Analog Speed;
        public bool IsRun;
        public float Setpoint;
        public ToggleButton SimFbOnButton;
        public ToggleButton SimFbButton;
        public bool SimFb;
        public bool SimFbOn;
    }
}
