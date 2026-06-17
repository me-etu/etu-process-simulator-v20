using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CoSimulationPlcSimAdv.Commands;
using CoSimulationPlcSimAdv.Models;
using Microsoft.Win32;
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

        private ObservableCollection<CommissioningDbConfig> commissioningDbConfigs;
        public ObservableCollection<CommissioningDbConfig> CommissioningDbConfigs
        {
            get { return commissioningDbConfigs; }
            set
            {
                if (commissioningDbConfigs == value)
                {
                    return;
                }

                commissioningDbConfigs = value;
                RaisePropertyChanged("CommissioningDbConfigs");
            }
        }

        private bool isDiagnosticIoRunning;
        public bool IsDiagnosticIoRunning
        {
            get { return isDiagnosticIoRunning; }
            set
            {
                if (isDiagnosticIoRunning == value)
                {
                    return;
                }

                isDiagnosticIoRunning = value;
                RaisePropertyChanged("IsDiagnosticIoRunning");
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string diagnosticProgressText;
        public string DiagnosticProgressText
        {
            get { return diagnosticProgressText; }
            set
            {
                if (diagnosticProgressText == value)
                {
                    return;
                }

                diagnosticProgressText = value;
                RaisePropertyChanged("DiagnosticProgressText");
            }
        }

        private int diagnosticProgressValue;
        public int DiagnosticProgressValue
        {
            get { return diagnosticProgressValue; }
            set
            {
                if (diagnosticProgressValue == value)
                {
                    return;
                }

                diagnosticProgressValue = value;
                RaisePropertyChanged("DiagnosticProgressValue");
            }
        }

        private int diagnosticProgressMaximum = 1;
        public int DiagnosticProgressMaximum
        {
            get { return diagnosticProgressMaximum; }
            set
            {
                if (diagnosticProgressMaximum == value)
                {
                    return;
                }

                diagnosticProgressMaximum = value;
                RaisePropertyChanged("DiagnosticProgressMaximum");
            }
        }

        private Visibility diagnosticProgressVisibility = Visibility.Collapsed;
        public Visibility DiagnosticProgressVisibility
        {
            get { return diagnosticProgressVisibility; }
            set
            {
                if (diagnosticProgressVisibility == value)
                {
                    return;
                }

                diagnosticProgressVisibility = value;
                RaisePropertyChanged("DiagnosticProgressVisibility");
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

        private ICommand importCommissioningDbCommand;
        public ICommand ImportCommissioningDbCommand
        {
            get
            {
                if (importCommissioningDbCommand == null)
                {
                    importCommissioningDbCommand = new RelayCommand(param => this.ImportCommissioningDb());
                }

                return importCommissioningDbCommand;
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
                    diagnosticIoCommand = new RelayCommand(param => this.StartDiagnosticIoTest(), param => this.IsInstanceNotNull() && !this.IsDiagnosticIoRunning);
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
            ReloadCommissioningDbConfigs();

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

        public async void StartDiagnosticIoTest()
        {
            if (IsDiagnosticIoRunning)
            {
                return;
            }

            IsDiagnosticIoRunning = true;
            DiagnosticProgressVisibility = Visibility.Visible;
            DiagnosticProgressValue = 0;
            DiagnosticProgressMaximum = 1;
            DiagnosticProgressText = "Starting IO audit...";
            WriteStatusEntry("Starting IO audit.");

            try
            {
                var message = await Task.Run(() => BuildDiagnosticIoReport());
                SetDiagnosticProgress(DiagnosticProgressMaximum, DiagnosticProgressMaximum, "IO audit complete.");
                WriteStatusEntry(message);
                MessageBox.Show(message, "IO Audit");
            }
            catch (Exception ex)
            {
                var message = "IO audit failed: " + ex.Message;
                WriteStatusEntry(message);
                MessageBox.Show(message, "IO Audit");
            }
            finally
            {
                IsDiagnosticIoRunning = false;
                DiagnosticProgressText = "IO audit idle.";
                DiagnosticProgressVisibility = Visibility.Collapsed;
            }
        }

        private string BuildDiagnosticIoReport()
        {
            var units = DeviceUiConfigLoader.LoadUnitConfigs();
            var commissioningConfigs = CommissioningDbConfigLoader.LoadConfigs();

            var digitalTags = new List<string>
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
            digitalTags.AddRange(DeviceUiConfigLoader.GetConfiguredBoolDiagnosticTags(units));
            var commissioningBoolTags = CommissioningDbConfigLoader.GetBoolDiagnosticTags(commissioningConfigs).ToList();

            var analogTags = new List<string>
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
            analogTags.AddRange(DeviceUiConfigLoader.GetConfiguredInt16DiagnosticTags(units));
            var commissioningRealTags = CommissioningDbConfigLoader.GetRealDiagnosticTags(commissioningConfigs).ToList();

            var distinctDigitalTags = digitalTags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct().ToList();
            var distinctCommissioningBoolTags = commissioningBoolTags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct().ToList();
            var distinctAnalogTags = analogTags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct().ToList();
            var distinctCommissioningRealTags = commissioningRealTags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct().ToList();
            var totalTagCount = distinctDigitalTags.Count
                + distinctCommissioningBoolTags.Count
                + distinctAnalogTags.Count
                + distinctCommissioningRealTags.Count;
            var completedTagCount = 0;

            SetDiagnosticProgress(0, Math.Max(totalTagCount, 1), "Preparing IO audit...");

            var report = new StringBuilder();
            report.AppendLine("Current-project IO audit:");
            report.AppendLine();
            report.AppendLine("Digital/Bool tags:");

            foreach (var tag in distinctDigitalTags)
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

                completedTagCount++;
                SetDiagnosticProgress(completedTagCount, totalTagCount, FormatDiagnosticProgress("Digital/Bool tags", completedTagCount, totalTagCount, tag));
            }

            report.AppendLine();
            report.AppendLine("Commissioning DB Bool tags:");

            foreach (var tag in distinctCommissioningBoolTags)
            {
                try
                {
                    var trueResult = virtualController.DiagnosticWriteReadCommissioningBool(tag, true);
                    var falseResult = virtualController.DiagnosticWriteReadCommissioningBool(tag, false);
                    report.AppendLine($"OK   {tag} -> TRUE:{trueResult} FALSE:{falseResult}");
                }
                catch (Exception ex)
                {
                    report.AppendLine($"FAIL {tag} -> {ex.Message}");
                }

                completedTagCount++;
                SetDiagnosticProgress(completedTagCount, totalTagCount, FormatDiagnosticProgress("Commissioning DB Bool tags", completedTagCount, totalTagCount, tag));
            }

            report.AppendLine();
            report.AppendLine("Analog/Int tags:");

            foreach (var tag in distinctAnalogTags)
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

                completedTagCount++;
                SetDiagnosticProgress(completedTagCount, totalTagCount, FormatDiagnosticProgress("Analog/Int tags", completedTagCount, totalTagCount, tag));
            }

            report.AppendLine();
            report.AppendLine("Commissioning DB Real tags:");

            foreach (var tag in distinctCommissioningRealTags)
            {
                try
                {
                    var testResult = virtualController.DiagnosticWriteReadReal(tag, 1234.5f);
                    var resetResult = virtualController.DiagnosticWriteReadReal(tag, 0f);
                    report.AppendLine(
                        "OK   " + tag
                        + " -> TEST:" + testResult.ToString(CultureInfo.InvariantCulture)
                        + " RESET:" + resetResult.ToString(CultureInfo.InvariantCulture));
                }
                catch (Exception ex)
                {
                    report.AppendLine($"FAIL {tag} -> {ex.Message}");
                }

                completedTagCount++;
                SetDiagnosticProgress(completedTagCount, totalTagCount, FormatDiagnosticProgress("Commissioning DB Real tags", completedTagCount, totalTagCount, tag));
            }

            return report.ToString();
        }

        private static string FormatDiagnosticProgress(string sectionName, int completed, int total, string tag)
        {
            return sectionName + ": " + completed + "/" + total + " - " + tag;
        }

        private void SetDiagnosticProgress(int value, int maximum, string text)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
            {
                DiagnosticProgressMaximum = Math.Max(maximum, 1);
                DiagnosticProgressValue = Math.Min(value, DiagnosticProgressMaximum);
                DiagnosticProgressText = text;
            }));
        }

        public void WriteCommissioningBool(CommissioningDbVariable variable, bool value)
        {
            if (variable == null || string.IsNullOrWhiteSpace(variable.plcTag))
            {
                return;
            }

            try
            {
                virtualController.WriteCommissioningBool(variable.plcTag, value);
                WriteStatusEntry("Commissioning DB wrote " + variable.plcTag + " = " + value);
            }
            catch (Exception ex)
            {
                WriteStatusEntry("Commissioning DB bool write failed for " + variable.plcTag + ": " + ex.Message);
            }
        }

        public void WriteCommissioningReal(CommissioningDbVariable variable, string textValue)
        {
            if (variable == null || string.IsNullOrWhiteSpace(variable.plcTag))
            {
                return;
            }

            float value;
            if (!float.TryParse(textValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                WriteStatusEntry("Commissioning DB real value is invalid for " + variable.plcTag + ": " + textValue);
                return;
            }

            try
            {
                virtualController.WriteCommissioningReal(variable.plcTag, value);
                WriteStatusEntry("Commissioning DB wrote " + variable.plcTag + " = " + value.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                WriteStatusEntry("Commissioning DB real write failed for " + variable.plcTag + ": " + ex.Message);
            }
        }

        public void ExitApplication()
        {
            Application.Current.Shutdown();
        }

        private void ImportCommissioningDb()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import TIA DB Source",
                Filter = "TIA DB source (*.db)|*.db|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                var result = CommissioningDbConfigLoader.ImportAndSave(dialog.FileName);
                ReloadCommissioningDbConfigs();

                var message = "Imported " + result.Config.variables.Count + " commissioning DB variables from "
                    + result.Config.dbName + "." + Environment.NewLine
                    + "Saved: " + result.SavedPath;

                if (result.SkippedLines.Count > 0)
                {
                    message += Environment.NewLine + Environment.NewLine
                        + "Skipped declarations:" + Environment.NewLine
                        + string.Join(Environment.NewLine, result.SkippedLines);
                }

                WriteStatusEntry(message);
                MessageBox.Show(message, "Commissioning DB Import");
            }
            catch (Exception ex)
            {
                var message = "Commissioning DB import failed: " + ex.Message;
                WriteStatusEntry(message);
                MessageBox.Show(message, "Commissioning DB Import");
            }
        }

        private void ReloadCommissioningDbConfigs()
        {
            CommissioningDbConfigs = new ObservableCollection<CommissioningDbConfig>(CommissioningDbConfigLoader.LoadConfigs());
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
