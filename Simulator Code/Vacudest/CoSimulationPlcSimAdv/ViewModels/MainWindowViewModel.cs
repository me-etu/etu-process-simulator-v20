using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CoSimulationPlcSimAdv.Commands;
using CoSimulationPlcSimAdv.Models;
using Siemens.Simatic.Simulation.Runtime;

namespace CoSimulationPlcSimAdv.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public PLCInstance virtualController = null;

        private ObservableCollection<String> statusListView;
        public ObservableCollection<String> StatusListView
        {
            get { return statusListView; }
            set
            {
                if (statusListView == value)
                {
                    return;
                }
                statusListView = value;
                base.RaisePropertyChanged("StatusListView");
            }
        }

        private String statusPLCInstance;
        public String StatusPLCInstance
        {
            get { return statusPLCInstance; }
            set
            {
                if (value == statusPLCInstance)
                    return;
                statusPLCInstance = value;
                RaisePropertyChanged("StatusPLCInstance");
            }
        }

        private ICommand powerOnInstanceCommand;
        public ICommand PowerOnInstanceCommand
        {
            get
            {
                if (powerOnInstanceCommand == null)
                {
                    powerOnInstanceCommand = new RelayCommand(param => this.PowerOnController(), param => this.IsInstanceNotNull());
                }

                return powerOnInstanceCommand;
            }
        }

        private ICommand powerOffInstanceCommand;
        public ICommand PowerOffInstanceCommand
        {
            get
            {
                if (powerOffInstanceCommand == null)
                {
                    powerOffInstanceCommand = new RelayCommand(param => this.PowerOffController(), param => this.IsInstanceNotNull());
                }

                return powerOffInstanceCommand;
            }
        }

        private ICommand runInstanceCommand;
        public ICommand RunInstanceCommand
        {
            get
            {
                if (runInstanceCommand == null)
                {
                    runInstanceCommand = new RelayCommand(param => this.RunController(), param => this.IsInstanceRunning());
                }

                return runInstanceCommand;
            }
        }

        private ICommand stopInstanceCommand;
        public ICommand StopInstanceCommand
        {
            get
            {
                if (stopInstanceCommand == null)
                {
                    stopInstanceCommand = new RelayCommand(param => this.StopController(), param => this.IsInstanceRunning());
                }

                return stopInstanceCommand;
            }
        }

        private ICommand diagnosticIoCommand;
        public ICommand DiagnosticIoCommand
        {
            get
            {
                if (diagnosticIoCommand == null)
                {
                    diagnosticIoCommand = new RelayCommand(param => this.RunDiagnosticIoTest(), param => this.IsInstanceNotNull());
                }

                return diagnosticIoCommand;
            }
        }

        private ICommand exitCommand;
        public ICommand ExitCommand
        {
            get
            {
                if (exitCommand == null)
                {
                    exitCommand = new RelayCommand(param => this.ExitApplication(), param => this.IsInstanceRunning());
                }

                return exitCommand;
            }
        }

        public MainWindowViewModel()
        {
            StatusListView = new ObservableCollection<String>();

            try
            {
                virtualController = new PLCInstance("H2O Vacudest Test");
                virtualController.OperatingStateChanged += plcInstance_OnOperatingStateChanged;
                WriteStatusEntry(String.Format("Instance registered: {0}", virtualController.instance.Name));
                StatusPLCInstance = virtualController.instance?.OperatingState.ToString();
            }
            catch (SimulationRuntimeException simRuntimeEx)
            {
                WriteStatusEntry("Error during Register of Instance: " + simRuntimeEx.Message);
            }
        }

        void plcInstance_OnOperatingStateChanged(EOperatingState operatingState)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
            {
                StatusPLCInstance = operatingState.ToString();
            }));
        }

        public void WriteStatusEntry(String statusText)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
            {
                StatusListView.Insert(0, DateTime.Now + ": " + statusText);
            }));
        }

        public void PowerOnController()
        {
            try
            {
                WriteStatusEntry(String.Format("Power On Instance: {0}", virtualController.instance.Name));
                virtualController.PowerOnPLCInstance();
            }
            catch (SimulationRuntimeException simRtEx)
            {
                WriteStatusEntry(String.Format("PowerOn Instance failed: {0}", simRtEx.Message));
            }
        }

        public void PowerOffController()
        {
            try
            {
                WriteStatusEntry(String.Format("Power Off Instance: {0}", virtualController.instance.Name));
                virtualController.PowerOffPLCInstance();
            }
            catch (SimulationRuntimeException simRtEx)
            {
                WriteStatusEntry(String.Format("PowerOff Instance failed: {0}", simRtEx.Message));
            }
        }

        public void RunController()
        {
            try
            {
                WriteStatusEntry(String.Format("Run Instance: {0}", virtualController.instance.Name));
                virtualController.RunPLCInstance();
            }
            catch (SimulationRuntimeException simRtEx)
            {
                WriteStatusEntry(String.Format("Run Instance failed: {0}! Please load plc program before execute RUN.", simRtEx.Message));
            }
        }

        public void StopController()
        {
            try
            {
                WriteStatusEntry(String.Format("Stop Instance: {0}", virtualController.instance.Name));
                virtualController.StopPLCInstance();
            }
            catch (SimulationRuntimeException simRtEx)
            {
                WriteStatusEntry(String.Format("Stop Instance failed: {0}", simRtEx.Message));
            }
        }

        public void RunDiagnosticIoTest()
        {
            var digitalTags = new[]
            {
                "IN_LSA-4-1-8-3",
                "IN_LSA-4-1-8-3_QB",
                "IN_FS-8-2-35-2",
                "IN_FS-8-2-35-2_QB",
                "IN_PS-6-3-8-4",
                "IN_PS-6-3-8-4_QB",
                "IN_FI-8-1-31-1_PULSE",
                "IN_FI-8-1-31-1_PULSE_QB",
                "IN_QC-9-1-8-4_CALIB",
                "IN_QC-9-1-8-4_CALIB_QB",
                "IN_G21-MS",
                "IN_G21-MS_QB",
                "IN_G21-MPS",
                "IN_G21-MPS_QB",
                "IN_G31-MS",
                "IN_G31-MS_QB",
                "IN_G31-MPS",
                "IN_G31-MPS_QB",
                "IN_G41-MS",
                "IN_G41-MS_QB",
                "IN_G41-MPS",
                "IN_G41-MPS_QB",
                "CTRL_G21",
                "FB_ON_G21",
                "CTRL_E21",
                "FB_ON_E21",
                "CTRL_Q21",
                "FB_OPN_Q21",
                "FB_OPN_Q21_QB",
                "FB_CLS_Q21",
                "FB_CLS_Q21_QB",
                "CTRL_Q31",
                "FB_OPN_Q31",
                "FB_OPN_Q31_QB",
                "FB_CLS_Q31",
                "FB_CLS_Q31_QB",
                "CTRL_Q41",
                "FB_OPN_Q41",
                "FB_OPN_Q41_QB",
                "FB_CLS_Q41",
                "FB_CLS_Q41_QB"
            };

            var analogTags = new[]
            {
                "IN_LC-4-20-8-4",
                "IN_QC-9-1-8-4",
                "IN_QC-10-1-8-4",
                "IN_PS-6-1-8-4",
                "IN_TC-5-3-31-1",
                "IN_TI-5-10-31-1",
                "IN_TI-5-20-31-1",
                "IN_FI-8-1-31-1"
            };

            var report = new StringBuilder();
            report.AppendLine("Current-project IO audit:");
            report.AppendLine();
            report.AppendLine("Digital/Bool tags:");

            foreach (var tag in digitalTags)
            {
                try
                {
                    var trueResult = virtualController.DiagnosticWriteReadBool(tag, true);
                    var falseResult = virtualController.DiagnosticWriteReadBool(tag, false);
                    report.AppendLine($"OK   {tag} -> TRUE:{trueResult} FALSE:{falseResult}");
                }
                catch (Exception ex)
                {
                    report.AppendLine($"FAIL {tag} -> {ex.Message}");
                }
            }

            report.AppendLine();
            report.AppendLine("Analog/Int tags:");

            foreach (var tag in analogTags)
            {
                try
                {
                    var testResult = virtualController.DiagnosticWriteReadInt16(tag, 1234);
                    var resetResult = virtualController.DiagnosticWriteReadInt16(tag, 0);
                    report.AppendLine($"OK   {tag} -> TEST:{testResult} RESET:{resetResult}");
                }
                catch (Exception ex)
                {
                    report.AppendLine($"FAIL {tag} -> {ex.Message}");
                }
            }

            var message = report.ToString();
            WriteStatusEntry(message);
            MessageBox.Show(message, "IO Audit");
        }

        public void ExitApplication()
        {
            Application.Current.Shutdown();
        }

        private bool IsInstanceNotNull()
        {
            return !(virtualController == null);
        }

        private bool IsInstanceRunning()
        {
            return true;
        }
    }
}
