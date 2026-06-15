using Siemens.Simatic.Simulation.Runtime;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Globalization;
using System.Windows.Input;

namespace CoSimulationPlcSimAdv
{
    public class WatchDog
    {
        public CoSimulationPlcSimAdv.App App { get; set; }
        public Simulation Simulation { get; set; }

        public WatchDog (String valName = "", int time = 100)
        {

            Time = time;
            ValName = valName;
            SetTime = 0;
            App = Application.Current as CoSimulationPlcSimAdv.App;
            App.Loop += OnLoop;
        
    }


        public void OnLoop(IInstance instance, bool init = false)
        {
   
            if (ValName == "") return;
            try
            {
                //var value = instance.ReadBool(ValName);
                int time = Environment.TickCount & Int32.MaxValue;

                if (time >= SetTime)
                {
                    SetTime = time + Time;
                    instance.WriteBool(ValName, value);
                    value = !value;
                }
            }
            catch {}
        }

        public int Time;
        public int SetTime;
        public String ValName;
        public bool value = true;
    }
}
