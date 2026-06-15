using System;
using System.Linq;
using Siemens.Simatic.Simulation.Runtime;
using PlcSimAdvancedFramework.Models;

namespace PlcSimAdvancedFramework.Services
{
    public class PlcSimRuntimeService
    {
        private readonly SIPSuite4 defaultIpSuite = new SIPSuite4("172.16.0.1", "255.255.255.0", "0.0.0.0");
        private IInstance instance;

        public event Action<string> StatusChanged;
        public event Action<string> OperatingStateChanged;

        public bool IsConnected
        {
            get { return instance != null; }
        }

        public string CurrentState
        {
            get { return instance == null ? "Disconnected" : instance.OperatingState.ToString(); }
        }

        public void AttachOrCreate(string instanceName)
        {
            if (!SimulationRuntimeManager.IsInitialized)
            {
                throw new InvalidOperationException("PLCSIM Advanced runtime is not initialized.");
            }

            var registeredInstances = SimulationRuntimeManager.RegisteredInstanceInfo;
            var hasExistingInstance = registeredInstances.Any(
                x => x.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase));

            if (hasExistingInstance)
            {
                var info = registeredInstances.First(
                    x => x.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase));
                instance = SimulationRuntimeManager.CreateInterface(info.ID);
            }
            else
            {
                instance = SimulationRuntimeManager.RegisterInstance(instanceName);
            }

            instance.OperatingMode = EOperatingMode.Default;
            instance.CommunicationInterface = ECommunicationInterface.TCPIP;
            instance.IsSendSyncEventInDefaultModeEnabled = true;
            instance.ScaleFactor = 1;
            instance.OnOperatingStateChanged += OnOperatingStateChanged;

            RaiseStatus($"Attached to instance '{instance.Name}'.");
        }
        public void PowerOn()
        {
            EnsureInstance();
            instance.PowerOn(60000);
            instance.SetIPSuite(0, defaultIpSuite, false);
            RaiseStatus("Instance powered on.");
        }

        public void PowerOff()
        {
            EnsureInstance();
            instance.PowerOff(60000);
            RaiseStatus("Instance powered off.");
        }

        public void Run()
        {
            EnsureInstance();
            instance.Run(60000);
            RaiseStatus("Instance switched to RUN.");
        }

        public void Stop()
        {
            EnsureInstance();
            instance.Stop(60000);
            RaiseStatus("Instance switched to STOP.");
        }

        public void RefreshTagList()
        {
            EnsureInstance();
            instance.UpdateTagList(ETagListDetails.IOMCTDB, false);
            RaiseStatus("Tag list refreshed.");
        }

        public string ReadTag(TagDefinition tag)
        {
            EnsureInstance();
            switch (tag.DataType)
            {
                case TagDataType.Bool:
                    return instance.ReadBool(tag.Address).ToString();
                case TagDataType.Int16:
                    return instance.ReadInt16(tag.Address).ToString();
                case TagDataType.Float:
                    return instance.ReadFloat(tag.Address).ToString("0.###");
                default:
                    throw new InvalidOperationException("Unsupported data type.");
            }
        }

        public void WriteTag(TagDefinition tag)
        {
            EnsureInstance();
            var value = tag.ParseManualValue();

            switch (tag.DataType)
            {
                case TagDataType.Bool:
                    instance.WriteBool(tag.Address, (bool)value);
                    break;
                case TagDataType.Int16:
                    instance.WriteInt16(tag.Address, (short)value);
                    break;
                case TagDataType.Float:
                    instance.WriteFloat(tag.Address, (float)value);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported data type.");
            }

            RaiseStatus($"Wrote {tag.ManualValue} to {tag.Address}.");
        }

        public void Detach()
        {
            if (instance == null)
            {
                return;
            }

            instance.OnOperatingStateChanged -= OnOperatingStateChanged;
            instance = null;
            RaiseStatus("Disconnected from instance.");
        }

        private void OnOperatingStateChanged(IInstance sender, ERuntimeErrorCode errorCode, DateTime dateTime, EOperatingState prevState, EOperatingState operatingState)
        {
            OperatingStateChanged?.Invoke(operatingState.ToString());
            RaiseStatus($"Operating state changed from {prevState} to {operatingState}.");
        }

        private void EnsureInstance()
        {
            if (instance == null)
            {
                throw new InvalidOperationException("No PLCSIM Advanced instance is attached.");
            }
        }

        private void RaiseStatus(string message)
        {
            StatusChanged?.Invoke(message);
        }
    }
}
