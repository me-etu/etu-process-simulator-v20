using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Siemens.Simatic.Simulation.Runtime;

namespace CoSimulationPlcSimAdv.Models
{
    public class PLCInstance : IDisposable
    {
        private readonly BlockingCollection<Action> plcActions = new BlockingCollection<Action>();
        private readonly Thread plcThread;
        private readonly SIPSuite4 instanceIP = new SIPSuite4("172.16.0.1", "255.255.255.0", "0.0.0.0");
        private readonly string instanceName;
        private volatile bool workerRunning = true;
        private volatile bool loopInitialized;
        private volatile bool disposed;
        private DateTime lastReconnectAttemptUtc = DateTime.MinValue;
        private DateTime lastMaintenanceUtc = DateTime.MinValue;
        private bool recoveringConnection;

        public IInstance instance { get; private set; }
        public CoSimulationPlcSimAdv.App App { get; set; }
        public bool IsActiv;
        public bool IsUpToDate;
        public bool IsOff;
        public int LastTime;
        public int Settime;
        public bool TogBit;
        public event Action<EOperatingState> OperatingStateChanged;

        public PLCInstance(string instanceName)
        {
            this.instanceName = instanceName;
            App = Application.Current as CoSimulationPlcSimAdv.App;

            plcThread = new Thread(PlcThreadLoop)
            {
                IsBackground = true,
                Name = "PLCSIM-Worker"
            };
            plcThread.SetApartmentState(ApartmentState.STA);
            plcThread.Start();

            ExecuteOnWorker(() =>
            {
                ConnectOrReconnectInterface();
                updateTags(instance);
                loopInitialized = false;
            });
        }

        public void PowerOnPLCInstance()
        {
            ExecuteOnWorker(() =>
            {
                ConnectOrReconnectInterface();
                instance.PowerOn(60000);
                instance.SetIPSuite(0, instanceIP, false);
                IsOff = false;
                IsActiv = true;
                ConnectOrReconnectInterface();
                updateTags(instance);
                loopInitialized = false;
            });
        }

        public void PowerOffPLCInstance()
        {
            ExecuteOnWorker(() =>
            {
                if (!IsActiv)
                {
                    return;
                }

                instance.PowerOff(60000);
                IsOff = true;
                IsUpToDate = false;
                IsActiv = false;
                loopInitialized = false;
                RaiseOperatingStateChanged(EOperatingState.Off);
            });
        }

        public void RunPLCInstance()
        {
            ExecuteOnWorker(() =>
            {
                ConnectOrReconnectInterface();
                instance.Run(60 * 1000);
                while (instance.OperatingState != EOperatingState.Run)
                {
                    Thread.Sleep(200);
                }

                ConnectOrReconnectInterface();
                updateTags(instance);
                loopInitialized = false;
            });
        }

        public void StopPLCInstance()
        {
            ExecuteOnWorker(() =>
            {
                if (instance.OperatingState == EOperatingState.Stop || instance.OperatingState == EOperatingState.Off)
                {
                    return;
                }

                instance.Stop(60 * 1000);
                while (instance.OperatingState != EOperatingState.Stop)
                {
                    Thread.Sleep(200);
                }

                ConnectOrReconnectInterface();
                loopInitialized = false;
            });
        }

        public bool DiagnosticWriteReadBool(string tagName, bool value)
        {
            return ExecuteOnWorker(() =>
            {
                ConnectOrReconnectInterface();
                updateTags(instance);
                if (!PlcIo.TryWriteBool(instance, tagName, value, App, "Diagnostic bool write"))
                {
                    throw new InvalidOperationException($"Unable to write {tagName}.");
                }

                bool result;
                if (!PlcIo.TryReadBool(instance, tagName, out result, App, "Diagnostic bool read"))
                {
                    throw new InvalidOperationException($"Unable to read {tagName}.");
                }

                return result;
            });
        }

        public short DiagnosticWriteReadInt16(string tagName, short value)
        {
            return ExecuteOnWorker(() =>
            {
                ConnectOrReconnectInterface();
                updateTags(instance);
                if (!PlcIo.TryWriteInt16(instance, tagName, value, App, "Diagnostic int write"))
                {
                    throw new InvalidOperationException($"Unable to write {tagName}.");
                }

                short result;
                if (!PlcIo.TryReadInt16(instance, tagName, out result, App, "Diagnostic int read"))
                {
                    throw new InvalidOperationException($"Unable to read {tagName}.");
                }

                return result;
            });
        }

        public void WriteCommissioningBool(string tagName, bool value)
        {
            ExecuteOnWorker(() =>
            {
                ConnectOrReconnectInterface();
                updateTags(instance);
                updateTagsForCommissioningDb(instance, tagName);
                if (!PlcIo.TryWriteSymbolicBool(instance, tagName, value, App, "Commissioning DB bool write"))
                {
                    LogCommissioningTagHints(tagName);
                    throw new InvalidOperationException($"Unable to write {tagName}.");
                }
            });
        }

        public bool DiagnosticWriteReadCommissioningBool(string tagName, bool value)
        {
            return ExecuteOnWorker(() =>
            {
                ConnectOrReconnectInterface();
                updateTags(instance);
                updateTagsForCommissioningDb(instance, tagName);
                if (!PlcIo.TryWriteSymbolicBool(instance, tagName, value, App, "Diagnostic commissioning bool write"))
                {
                    LogCommissioningTagHints(tagName);
                    throw new InvalidOperationException($"Unable to write {tagName}.");
                }

                bool result;
                if (!PlcIo.TryReadSymbolicBool(instance, tagName, out result, App, "Diagnostic commissioning bool read"))
                {
                    LogCommissioningTagHints(tagName);
                    throw new InvalidOperationException($"Unable to read {tagName}.");
                }

                return result;
            });
        }

        public void WriteCommissioningReal(string tagName, float value)
        {
            ExecuteOnWorker(() =>
            {
                ConnectOrReconnectInterface();
                updateTags(instance);
                updateTagsForCommissioningDb(instance, tagName);
                if (!PlcIo.TryWriteReal(instance, tagName, value, App, "Commissioning DB real write"))
                {
                    LogCommissioningTagHints(tagName);
                    throw new InvalidOperationException($"Unable to write {tagName}.");
                }
            });
        }

        public void WriteCommissioningInt16(string tagName, short value)
        {
            ExecuteOnWorker(() =>
            {
                ConnectOrReconnectInterface();
                updateTags(instance);
                updateTagsForCommissioningDb(instance, tagName);
                if (!PlcIo.TryWriteSymbolicInt16(instance, tagName, value, App, "Commissioning DB int write"))
                {
                    LogCommissioningTagHints(tagName);
                    throw new InvalidOperationException($"Unable to write {tagName}.");
                }
            });
        }

        public short DiagnosticWriteReadCommissioningInt16(string tagName, short value)
        {
            return ExecuteOnWorker(() =>
            {
                ConnectOrReconnectInterface();
                updateTags(instance);
                updateTagsForCommissioningDb(instance, tagName);
                if (!PlcIo.TryWriteSymbolicInt16(instance, tagName, value, App, "Diagnostic commissioning int write"))
                {
                    LogCommissioningTagHints(tagName);
                    throw new InvalidOperationException($"Unable to write {tagName}.");
                }

                short result;
                if (!PlcIo.TryReadSymbolicInt16(instance, tagName, out result, App, "Diagnostic commissioning int read"))
                {
                    LogCommissioningTagHints(tagName);
                    throw new InvalidOperationException($"Unable to read {tagName}.");
                }

                return result;
            });
        }

        public float DiagnosticWriteReadReal(string tagName, float value)
        {
            return ExecuteOnWorker(() =>
            {
                ConnectOrReconnectInterface();
                updateTags(instance);
                updateTagsForCommissioningDb(instance, tagName);
                if (!PlcIo.TryWriteReal(instance, tagName, value, App, "Diagnostic real write"))
                {
                    LogCommissioningTagHints(tagName);
                    throw new InvalidOperationException($"Unable to write {tagName}.");
                }

                float result;
                if (!PlcIo.TryReadReal(instance, tagName, out result, App, "Diagnostic real read"))
                {
                    LogCommissioningTagHints(tagName);
                    throw new InvalidOperationException($"Unable to read {tagName}.");
                }

                return result;
            });
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            try
            {
                ExecuteOnWorker(() =>
                {
                    DetachInterface();
                });
            }
            catch
            {
            }

            workerRunning = false;
            plcActions.CompleteAdding();
            plcThread.Join(1000);
        }

        private void PlcThreadLoop()
        {
            while (workerRunning)
            {
                Action action;
                if (plcActions.TryTake(out action, 100))
                {
                    action();
                }

                if (!workerRunning || instance == null)
                {
                    continue;
                }

                var state = instance.OperatingState;
                if (state != EOperatingState.Run && state != EOperatingState.Stop)
                {
                    loopInitialized = false;
                    continue;
                }

                try
                {
                    EnsureFreshLoopConnection();
                    App.DoLoop(instance, !loopInitialized);
                    loopInitialized = true;
                }
                catch (Exception ex)
                {
                    loopInitialized = false;
                    App?.LogStatus($"Refresh loop failed: {ex.Message}");
                    TryRecoverLoopConnection();
                }
            }
        }

        private void ConnectOrReconnectInterface()
        {
            if (!SimulationRuntimeManager.IsInitialized)
            {
                throw new InvalidOperationException("PLCSIM Advanced runtime is not initialized.");
            }

            DetachInterface();

            var info = SimulationRuntimeManager.RegisteredInstanceInfo.SingleOrDefault(x => x.Name.Equals(instanceName));
            if (info.Name != null)
            {
                instance = SimulationRuntimeManager.CreateInterface(info.ID);
            }
            else
            {
                instance = SimulationRuntimeManager.RegisterInstance(instanceName);
            }

            instance.OperatingMode = EOperatingMode.Default;
            instance.IsSendSyncEventInDefaultModeEnabled = false;
            instance.CommunicationInterface = ECommunicationInterface.TCPIP;
            instance.ScaleFactor = 1;
            instance.CPUType = ECPUType.CPU1500_Unspecified;
            instance.OnHardwareConfigChanged += Instance_OnHardwareConfigurationChanged;
            instance.OnUpdateEventDone += Instance_OnUpdateEventDone;
            instance.OnOperatingStateChanged += Instance_OnOperatingStateChanged;

            App.Instance = instance;
            App?.LogStatus($"Attached API interface to '{instance.Name}' ({instance.ID}).");
            RaiseOperatingStateChanged(instance.OperatingState);
        }

        private void DetachInterface()
        {
            if (instance == null)
            {
                return;
            }

            try
            {
                instance.OnHardwareConfigChanged -= Instance_OnHardwareConfigurationChanged;
                instance.OnUpdateEventDone -= Instance_OnUpdateEventDone;
                instance.OnOperatingStateChanged -= Instance_OnOperatingStateChanged;
            }
            catch (SimulationRuntimeException ex)
            {
                App?.LogStatus($"Interface detach failed: {ex.Message}");
            }
        }

        private void ExecuteOnWorker(Action action)
        {
            ExecuteOnWorker<object>(() =>
            {
                action();
                return null;
            });
        }

        private T ExecuteOnWorker<T>(Func<T> action)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(PLCInstance));
            }

            var completion = new TaskCompletionSource<T>();
            plcActions.Add(() =>
            {
                try
                {
                    completion.SetResult(action());
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
            });

            return completion.Task.GetAwaiter().GetResult();
        }

        private void Instance_OnUpdateEventDone(IInstance in_Sender, ERuntimeErrorCode in_ErrorCode, DateTime in_SystemTime, uint in_HardwareIdentifier)
        {
            updateTags(in_Sender);
            App.Instance = in_Sender;
            loopInitialized = false;
        }

        private void Instance_OnOperatingStateChanged(IInstance in_Sender, ERuntimeErrorCode in_ErrorCode, DateTime in_DateTime, EOperatingState in_PrevState, EOperatingState in_OperatingState)
        {
            App.Instance = in_Sender;
            loopInitialized = false;
            RaiseOperatingStateChanged(in_OperatingState);
        }

        private void Instance_OnHardwareConfigurationChanged(IInstance in_Sender, ERuntimeErrorCode in_ErrorCode, DateTime in_DateTime)
        {
            updateTags(in_Sender);
            App.Instance = in_Sender;
            loopInitialized = false;
        }

        private void updateTags(IInstance in_Sender)
        {
            IsUpToDate = false;

            try
            {
                int i = 0;
                while (!in_Sender.OperatingState.Equals(EOperatingState.Run) && !in_Sender.OperatingState.Equals(EOperatingState.Stop) && i <= 10)
                {
                    i++;
                    Thread.Sleep(100);
                }

                in_Sender.UpdateTagList(ETagListDetails.IOMCTDB, false);
                IsUpToDate = true;
            }
            catch (SimulationRuntimeException simEx)
            {
                App?.LogStatus($"Tag list update failed: {simEx.Message}");
            }
        }

        private void updateTagsForCommissioningDb(IInstance in_Sender, string tagName)
        {
            var dbName = ExtractDbName(tagName);
            if (string.IsNullOrWhiteSpace(dbName))
            {
                return;
            }

            try
            {
                in_Sender.UpdateTagList(ETagListDetails.IOMCTDB, false, "\"" + dbName + "\"");
                IsUpToDate = true;
            }
            catch (SimulationRuntimeException simEx)
            {
                App?.LogStatus($"Commissioning DB tag list update failed for {dbName}: {simEx.Message}");
            }
        }

        private void LogCommissioningTagHints(string tagName)
        {
            try
            {
                var memberName = ExtractMemberName(tagName);
                if (string.IsNullOrWhiteSpace(memberName) || instance?.TagInfos == null)
                {
                    return;
                }

                var matches = instance.TagInfos
                    .Where(tag => !string.IsNullOrWhiteSpace(tag.Name)
                        && tag.Name.IndexOf(memberName, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(tag => tag.Name)
                    .Distinct()
                    .Take(8)
                    .ToList();

                if (matches.Count > 0)
                {
                    App?.LogStatus("PLCSIM tag-list names matching " + memberName + ": " + string.Join(", ", matches));
                }
                else
                {
                    App?.LogStatus("PLCSIM tag list contains no names matching " + memberName + ".");
                }
            }
            catch (Exception ex)
            {
                App?.LogStatus($"Commissioning DB tag hint lookup failed: {ex.Message}");
            }
        }

        private static string ExtractDbName(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return null;
            }

            if (tagName.StartsWith("\"", StringComparison.Ordinal))
            {
                var endQuote = tagName.IndexOf('"', 1);
                return endQuote > 1 ? tagName.Substring(1, endQuote - 1) : null;
            }

            var dotIndex = tagName.IndexOf('.');
            return dotIndex > 0 ? tagName.Substring(0, dotIndex) : null;
        }

        private static string ExtractMemberName(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return null;
            }

            var dotIndex = tagName.LastIndexOf('.');
            if (dotIndex < 0 || dotIndex >= tagName.Length - 1)
            {
                return tagName;
            }

            return tagName.Substring(dotIndex + 1).Trim('"');
        }

        private void EnsureFreshLoopConnection()
        {
            var utcNow = DateTime.UtcNow;
            if ((utcNow - lastMaintenanceUtc).TotalSeconds < 30)
            {
                return;
            }

            lastMaintenanceUtc = utcNow;

            try
            {
                ConnectOrReconnectInterface();
                updateTags(instance);
            }
            catch (Exception ex)
            {
                App?.LogStatus($"Periodic PLCSIM refresh failed: {ex.Message}");
            }
        }

        private void TryRecoverLoopConnection()
        {
            if (recoveringConnection)
            {
                return;
            }

            var utcNow = DateTime.UtcNow;
            if ((utcNow - lastReconnectAttemptUtc).TotalSeconds < 2)
            {
                return;
            }

            recoveringConnection = true;
            lastReconnectAttemptUtc = utcNow;

            try
            {
                App?.LogStatus("Attempting PLCSIM interface recovery for cyclic IO update.");
                ConnectOrReconnectInterface();
                updateTags(instance);
            }
            catch (Exception reconnectEx)
            {
                App?.LogStatus($"PLCSIM interface recovery failed: {reconnectEx.Message}");
            }
            finally
            {
                recoveringConnection = false;
            }
        }

        private void RaiseOperatingStateChanged(EOperatingState state)
        {
            try
            {
                OperatingStateChanged?.Invoke(state);
            }
            catch (Exception ex)
            {
                App?.LogStatus($"Operating state notification failed: {ex.Message}");
            }
        }
    }
}
