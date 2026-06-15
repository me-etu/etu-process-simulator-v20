using Siemens.Simatic.Simulation.Runtime;
using System.Collections.Generic;
using System.Windows;
using System.Xml.Linq;

namespace CoSimulationPlcSimAdv
{
    public class Simulation
    {
        private readonly HashSet<string> loggedTagFailures = new HashSet<string>();

        public CoSimulationPlcSimAdv.App App {get; set; }

        public Simulation()
        {
            App = Application.Current as CoSimulationPlcSimAdv.App;
            App.Loop += OnLoop;
        }

        public void OnLoop(IInstance instance, bool init = false)
        {
            Instance = instance;
            if (Instance == null)
            {
                return;
            }

            try
            {
                //bool notAusActive;
                //if (TryReadBool("E_NotAus_Ok", out notAusActive))
                //{
                //    PISL3150_16.Value = notAusActive;
                //}                                            

                MirrorBool("CTRL_G21", "FB_ON_G21");
                MirrorBool("CTRL_E21", "FB_ON_E21");
            }
            catch (SimulationRuntimeException ex)
            {
                App?.LogStatus($"Simulation loop failed: {ex.Message}");
            }
        }

        private bool TryReadBool(string name, out bool value)
        {
            value = false;
            try
            {
                if (!PlcIo.TryReadBool(Instance, name, out value, App, "Simulation read"))
                {
                    return false;
                }

                loggedTagFailures.Remove(name);
                return true;
            }
            catch (SimulationRuntimeException ex)
            {
                LogTagFailureOnce(name, $"ReadBool failed for {name}: {ex.Message}");
                return false;
            }
        }

        private bool TryReadFloat(string name, out float value)
        {
            value = 0;
            try
            {
                value = Instance.ReadFloat(name);
                loggedTagFailures.Remove(name);
                return true;
            }
            catch (SimulationRuntimeException ex)
            {
                LogTagFailureOnce(name, $"ReadFloat failed for {name}: {ex.Message}");
                return false;
            }
        }

        private bool TryWriteBool(string name, bool value)
        {
            try
            {
                if (!PlcIo.TryWriteBool(Instance, name, value, App, "Simulation write"))
                {
                    return false;
                }

                loggedTagFailures.Remove(name);
                return true;
            }
            catch (SimulationRuntimeException ex)
            {
                LogTagFailureOnce(name, $"WriteBool failed for {name}: {ex.Message}");
                return false;
            }
        }

        private bool TryIsPidActive(string name, out bool active)
        {
            bool mvTrackOn;
            if (TryReadBool(name + ".MV_TRKON", out mvTrackOn))
            {
                active = !mvTrackOn;
                return true;
            }

            active = false;
            return false;
        }

        private void LogTagFailureOnce(string tagName, string message)
        {
            if (loggedTagFailures.Add(tagName))
            {
                App?.LogStatus(message);
            }
        }

        private void MirrorBool(string sourceTagName, string targetTagName)
        {
            bool value;
            if (TryReadBool(sourceTagName, out value))
            {
                TryWriteBool(targetTagName, value);
            }
        }

        public bool ReadBit(string name)
        {
            return Instance.ReadBool(name);
        }

        public bool IsPhaseRun(string name)
        {
            return Instance.ReadBool(name + ".START") || Instance.ReadBool(name + ".PAUSE");
        }

        public bool IsPhaseHeld(string name)
        {
            return Instance.ReadBool(name + ".HELD");
        }

        public bool IsValveOpened(string name)
        {
            return Instance.ReadBool(name + ".QOPEN");
        }

        public bool IsMotorRun(string name)
        {
            return Instance.ReadBool(name + ".QRUN");
        }

        public bool IsValveClosed(string name)
        {
            return Instance.ReadBool(name + ".QCLOSE");
        }

        public short ReadStep()
        {
            return Instance.ReadInt16(Phase + ".step");
        }

        public bool IsPIDActive(string name)
        {
            return !Instance.ReadBool(name + ".MV_TRKON"); 
        }

        public float ActSpPID(string name)
        {
            return Instance.ReadFloat(name + ".QSP");
        }

        public bool IsTransferActive(string name = "Transfer")
        {
            return Instance.ReadBool(Phase + "." + name + ".ACTIVE");
        }

        public bool IsNotAusActive ()
        {
            return Instance.ReadBool("E_NotAus_Ok");
        }

        private IInstance Instance;
        private short step;
        public string IsphRun;
        public float TimeFact = 1;
        public bool xEdge1;
        public bool xEdge2;
        public bool xEdge3;
        public bool xEdge4;
        public bool xEdge5;
        private string Phase;

        //Valves
        public Valve Q21 = new Valve("Q21");
        public Valve Q31 = new Valve("Q31");
        public Valve Q41 = new Valve("Q41");

        //Analog
        public Analog LC42084 = new Analog("IN_LC-4-20-8-4", 0, 100);
        public Analog QC9184 = new Analog("IN_QC-9-1-8-4", 0, 14);
        public Analog QC10184 = new Analog("IN_QC-10-1-8-4", 0, 1000);
        public Analog PS6184 = new Analog("IN_PS-6-1-8-4", 0, 10);
        public Analog TC53311 = new Analog("IN_TC-5-3-31-1", -50, 200);
        public Analog TI510311 = new Analog("IN_TI-5-10-31-1", -50, 200);
        public Analog TI520311 = new Analog("IN_TI-5-20-31-1", -50, 200);
        public Analog FI81311 = new Analog("IN_FI-8-1-31-1", 0, 9600);



        //Digital
        public Digital LSA4183 = new Digital("IN_LSA-4-1-8-3");
        public Digital FS82352 = new Digital("IN_FS-8-2-35-2");
        public Digital PS6384 = new Digital("IN_PS-6-3-8-4");
        public Digital QC9184C  = new Digital("IN_QC-9-1-8-4_CALIB");
        public Digital FI81311P  = new Digital("IN_FI-8-1-31-1_PULSE");
        public Digital G21MS  = new Digital("IN_G21-MS");
        public Digital G21MPS = new Digital("IN_G21-MPS");
        public Digital G31MS = new Digital("IN_G31-MS");
        public Digital G31MPS = new Digital("IN_G31-MPS");
        public Digital G41MS = new Digital("IN_G41-MS");
        public Digital G41MPS = new Digital("IN_G41-MPS");

        //Motor
        public Motor E21 = new Motor("E21");
        public Motor G21 = new Motor("G21");
        public Motor G31 = new Motor("G31");
        public Motor G41 = new Motor("G41");
    }
}

