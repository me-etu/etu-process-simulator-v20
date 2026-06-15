using Siemens.Simatic.Simulation.Runtime;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Globalization;
using System.Windows.Input;

namespace CoSimulationPlcSimAdv
{
    public class Timer
    {
        public CoSimulationPlcSimAdv.App App { get; set; }
        public Simulation Simulation { get; set; }

        public Timer (int time = 5000)
        {
            Time = time;
            App = Application.Current as CoSimulationPlcSimAdv.App;
            App.Loop += OnLoop;
        }

        public void OnLoop(IInstance instance, bool init = false)
        {
            if (!In)
            {
                TimeActive = false;
                Q = false;
            }
            else
            {
                int time = Environment.TickCount & Int32.MaxValue;
             
                if (!TimeActive)
                {
                    SetTime = time + Time;
                    TimeActive = true;
                }
                if (time >= SetTime) Q = true;
            }
        }

        public bool In = false;
        public bool Q = false;
        private bool TimeActive = false;
        public int Time;
        public int SetTime;

    }
}
