using Siemens.Simatic.Simulation.Runtime;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Globalization;
using System.Windows.Input;

namespace CoSimulationPlcSimAdv
{
    public class Analog
    {
        public CoSimulationPlcSimAdv.App App { get; set; }
        public Simulation Simulation { get; set; }

        public Analog(string actValueName, float minValue = 0, float maxValue = 500, int time_s = 10, float setpoint = 0, float offset = 0, string setpointName = "", bool spIsInt = false, short minDig = 0, int maxDig = 27648, short actValue = 32767, bool actValueManual = false)
        {
            ActValueName = actValueName;
            MSetpoint = 0;
            SetpointName = setpointName;
            MaxValue = maxValue;
            MinValue = minValue;
            MaxDig = maxDig;
            MinDig = minDig;
            LastTime = 0;
            Offset = offset;
            Setpoint = setpoint;
            SetpointManual = false;
            ActValueManual = actValueManual;
            ActValue = actValue;
            Time = time_s * 1000;
            Initialized = false;
            SpIsInt = spIsInt;

            App = Application.Current as CoSimulationPlcSimAdv.App;

            App.Loop += OnLoop;
            
            var wnd = App.MainWindow as CoSimulationPlcSimAdv.Views.MainWindow;
            var grid = wnd.Content as Grid;
            var name = actValueName.Replace("IN_", "").Replace("-", "_");

            SetpointEdit = grid.FindName(name + "_SetpointEdit") as TextBox;
            if (SetpointEdit != null)
            {
                SetpointEdit.KeyDown += OnSetpointEditKeyDown;              
            }
            SetpointButton = grid.FindName(name + "_SetpointButton") as ToggleButton;

            if (SetpointButton != null) SetpointButton.Click += OnSetpointButtonClick;
            ActValueEdit = grid.FindName(name + "_ActValueEdit") as TextBox;
            if (ActValueEdit != null)
            {
                ActValueEdit.KeyDown += OnActValueEditKeyDown;
            }
            ActValueButton = grid.FindName(name + "_ActValueButton") as ToggleButton;
            if (ActValueButton != null) ActValueButton.Click += OnActValueButtonClick;
        }

        public void OnLoop(IInstance instance, bool init = false)
        {
            if (init) Initialized = false;

            try
            { 
                if (!Initialized)
                {
                    PlcIo.TryWriteInt16(instance, ActValueName, Convert.ToInt16((Setpoint - MinValue) / (MaxValue - MinValue) * (MaxDig - MinDig) + MinDig), App, "Analog init write");
                    Initialized = true;                
                    return;            
                }

                short currentActValue;
                if (!PlcIo.TryReadInt16(instance, ActValueName, out currentActValue, App, "Analog read"))
                {
                    return;
                }

                float actValueDig = currentActValue;

                if (SetpointName != "" && !SetpointManual && !ActValueManual)
                {
                    if (SpIsInt)
                    {
                        short currentSetpoint;
                        if (PlcIo.TryReadInt16(instance, SetpointName, out currentSetpoint, App, "Analog setpoint read"))
                        {
                            Setpoint = currentSetpoint;
                        }
                    }
                    else
                    {
                        Setpoint = instance.ReadFloat(SetpointName);

                    }
                }

           
                float setpoint = SetpointManual ? MSetpoint : Setpoint + Offset;
            

                if (setpoint < MinValue) setpoint = MinValue;
                if (setpoint > MaxValue) setpoint = MaxValue;

                short setpointDig = Convert.ToInt16((setpoint - MinValue) / (MaxValue - MinValue) * (MaxDig - MinDig) + MinDig);

                if (ActValueManual)
                {
                    PlcIo.TryWriteInt16(instance, ActValueName, ActValue, App, "Analog manual write");
                    return;
                }

                if (actValueDig <= MinDig) actValueDig = MinDig;
                if (actValueDig >= MaxDig) actValueDig = MaxDig;

                float actValue = (actValueDig - MinDig) / (MaxDig - MinDig) * (MaxValue - MinValue) + MinValue;

                ActualValue = actValue;

                int time = Environment.TickCount & Int32.MaxValue;

                if (actValueDig == setpointDig)
                {
                    LastTime = 0;
                    Changed = false;
                    return;
                }
                else
                {
                    if (!Changed)
                    {
                        LastTime = time;
                        Changed = true;
                    }
                }

                float dt = time - LastTime;

                float delta = (setpoint - actValue);

                float deltaAbs = delta > 0 ? delta : -delta;

                float totaltime = deltaAbs / (MaxValue - MinValue) * (Time * App.Simulation.TimeFact);

                if (dt > totaltime) dt = totaltime;

                float change = dt / totaltime * delta;

                actValue += change;

                short newActValueDig = Convert.ToInt16((actValue - MinValue) / (MaxValue - MinValue) * (MaxDig - MinDig) + MinDig);

                if (newActValueDig != actValueDig)
                {
                    LastTime = time;
                    PlcIo.TryWriteInt16(instance, ActValueName, newActValueDig, App, "Analog write");
                }
            }
            catch (SimulationRuntimeException ex)
            {
                App?.LogStatus($"Analog IO failed for {ActValueName}: {ex.Message}");
            }
        }

        private void OnSetpointButtonClick(object sender, RoutedEventArgs args)
        {
            SetpointManual = SetpointButton?.IsChecked == true;

            if (SetpointManual)
            {
                ReadSetpointEdit();
            }
            else
            {
                Setpoint = MSetpoint;
                SetpointName = "";
                LastTime = 0;
            }
        }
           
        private void OnSetpointEditKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;
            ReadSetpointEdit();
        }

        private void ReadSetpointEdit()
        {
            float setpointEdit = 0;
            try
            {
                setpointEdit = float.Parse(SetpointEdit.Text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (Exception)
            {
                SetpointEdit.Text = Setpoint.ToString();
                return;
            }
            MSetpoint = setpointEdit;
        }
        
        private void OnActValueButtonClick(object sender, RoutedEventArgs args)
        {
            ActValueManual = !ActValueManual;
        }

        private void OnActValueEditKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;
            short actValue = 0;
            try
            {
                actValue = short.Parse(ActValueEdit.Text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (Exception)
            {
                ActValueEdit.Text = ActValue.ToString();
                return;
            }
            ActValue = actValue;
            ActValue = short.Parse(ActValueEdit.Text, CultureInfo.InvariantCulture.NumberFormat);
        }

        public short ActValue;
        public ToggleButton ActValueButton;
        public TextBox ActValueEdit;
        public String ActValueName;
        public String SetpointName;
        public int Time;
        public int LastTime;
        public float ActualValue;
        public float MaxValue;
        public float MinValue;
        public int MaxDig;
        public short MinDig;
        public float Offset;
        public float Setpoint;
        public float MSetpoint;
        public ToggleButton SetpointButton;
        public TextBox SetpointEdit;
        public bool SetpointManual;
        public bool ActValueManual;
        public bool Initialized;
        public bool SpIsInt;
        public bool Changed;
    }
}
