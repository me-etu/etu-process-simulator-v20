using Siemens.Simatic.Simulation.Runtime;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CoSimulationPlcSimAdv
{
    internal sealed class PlcIoAddress
    {
        public PlcIoAddress(uint byteOffset, byte? bitOffset)
        {
            ByteOffset = byteOffset;
            BitOffset = bitOffset;
        }

        public uint ByteOffset { get; }
        public byte? BitOffset { get; }

        public static bool TryParseMarkerAddress(string address, out PlcIoAddress result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(address) || !address.StartsWith("%M", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var body = address.Substring(2);
            if (body.StartsWith("W", StringComparison.OrdinalIgnoreCase))
            {
                body = body.Substring(1);
            }

            var parts = body.Split('.');
            uint byteOffset;
            if (!uint.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out byteOffset))
            {
                return false;
            }

            byte? bitOffset = null;
            if (parts.Length == 2)
            {
                byte bit;
                if (!byte.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out bit) || bit > 7)
                {
                    return false;
                }

                bitOffset = bit;
            }
            else if (parts.Length > 2)
            {
                return false;
            }

            result = new PlcIoAddress(byteOffset, bitOffset);
            return true;
        }
    }

    internal static class PlcIo
    {
        private static readonly HashSet<string> LoggedFallbacks = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> LoggedFailures = new HashSet<string>(StringComparer.Ordinal);

        private static readonly Dictionary<string, PlcIoAddress> Addresses = new Dictionary<string, PlcIoAddress>(StringComparer.Ordinal)
        {
            { "IN_LSA-4-1-8-3", Marker("%M1000.0") },
            { "IN_LSA-4-1-8-3_QB", Marker("%M1000.1") },
            { "IN_FS-8-2-35-2", Marker("%M1000.2") },
            { "IN_FS-8-2-35-2_QB", Marker("%M1000.3") },
            { "IN_PS-6-3-8-4", Marker("%M1000.4") },
            { "IN_PS-6-3-8-4_QB", Marker("%M1000.5") },
            { "IN_FI-8-1-31-1_PULSE", Marker("%M1000.6") },
            { "IN_FI-8-1-31-1_PULSE_QB", Marker("%M1000.7") },
            { "IN_QC-9-1-8-4_CALIB", Marker("%M1001.0") },
            { "IN_QC-9-1-8-4_CALIB_QB", Marker("%M1001.1") },
            { "IN_G21-MS", Marker("%M1001.2") },
            { "IN_G21-MS_QB", Marker("%M1001.3") },
            { "IN_G31-MS", Marker("%M1001.4") },
            { "IN_G31-MS_QB", Marker("%M1001.5") },
            { "IN_G41-MS", Marker("%M1001.6") },
            { "IN_G41-MS_QB", Marker("%M1001.7") },
            { "IN_G21-MPS", Marker("%M1002.0") },
            { "IN_G21-MPS_QB", Marker("%M1002.1") },
            { "IN_G31-MPS", Marker("%M1002.2") },
            { "IN_G31-MPS_QB", Marker("%M1002.3") },
            { "IN_G41-MPS", Marker("%M1002.4") },
            { "IN_G41-MPS_QB", Marker("%M1002.5") },

            { "IN_LC-4-20-8-4", Marker("%MW2000") },
            { "IN_QC-9-1-8-4", Marker("%MW2002") },
            { "IN_QC-10-1-8-4", Marker("%MW2004") },
            { "IN_PS-6-1-8-4", Marker("%MW2006") },
            { "IN_TC-5-3-31-1", Marker("%MW2008") },
            { "IN_TI-5-10-31-1", Marker("%MW2010") },
            { "IN_TI-5-20-31-1", Marker("%MW2012") },
            { "IN_FI-8-1-31-1", Marker("%MW2014") },
            { "FB_OPN_Q21", Marker("%M500.0") },
            { "FB_OPN_Q21_QB", Marker("%M500.1") },
            { "FB_CLS_Q21", Marker("%M500.2") },
            { "FB_CLS_Q21_QB", Marker("%M500.3") },
            { "CTRL_Q21", Marker("%M500.4") },
            { "FB_OPN_Q31", Marker("%M500.5") },
            { "FB_OPN_Q31_QB", Marker("%M500.6") },
            { "FB_CLS_Q31", Marker("%M501.0") },
            { "FB_CLS_Q31_QB", Marker("%M501.1") },
            { "CTRL_Q31", Marker("%M501.2") },
            { "FB_OPN_Q41", Marker("%M501.3") },
            { "FB_OPN_Q41_QB", Marker("%M501.4") },
            { "FB_CLS_Q41", Marker("%M501.5") },
            { "FB_CLS_Q41_QB", Marker("%M501.6") },
            { "CTRL_Q41", Marker("%M502.0") },
            { "CTRL_G21", Marker("%M2500.0") },
            { "FB_ON_G21", Marker("%M2500.1") },
            { "CTRL_E21", Marker("%M2500.2") },
            { "FB_ON_E21", Marker("%M2500.3") },
        };

        public static void LoadMarkerFallbacks(IEnumerable<CoSimulationPlcSimAdv.Models.DeviceUiMarkerFallback> markerFallbacks)
        {
            foreach (var markerFallback in markerFallbacks)
            {
                if (markerFallback == null
                    || string.IsNullOrWhiteSpace(markerFallback.plcTag)
                    || string.IsNullOrWhiteSpace(markerFallback.markerAddress)
                    || Addresses.ContainsKey(markerFallback.plcTag))
                {
                    continue;
                }

                Addresses[markerFallback.plcTag] = Marker(markerFallback.markerAddress);
            }
        }

        public static bool TryWriteBool(IInstance instance, string tagName, bool value, CoSimulationPlcSimAdv.App app, string context)
        {
            try
            {
                instance.WriteBool(tagName, value);
                Exception ignoredMarkerError;
                TryWriteMarkerBool(instance, tagName, value, out ignoredMarkerError);
                ClearLogState(tagName);
                return true;
            }
            catch (SimulationRuntimeException symbolicEx)
            {
                Exception markerEx;
                if (TryWriteMarkerBool(instance, tagName, value, out markerEx))
                {
                    LogOnce(app, LoggedFallbacks, tagName, $"{context} used marker fallback for {tagName}: {symbolicEx.Message}");
                    return true;
                }

                LogOnce(app, LoggedFailures, tagName, $"{context} failed for {tagName}: {symbolicEx.Message}{FormatFallback(markerEx)}");
                return false;
            }
        }

        public static bool TryReadBool(IInstance instance, string tagName, out bool value, CoSimulationPlcSimAdv.App app, string context)
        {
            value = false;

            try
            {
                value = instance.ReadBool(tagName);
                ClearLogState(tagName);
                return true;
            }
            catch (SimulationRuntimeException symbolicEx)
            {
                Exception markerEx;
                if (TryReadMarkerBool(instance, tagName, out value, out markerEx))
                {
                    LogOnce(app, LoggedFallbacks, tagName, $"{context} used marker fallback for {tagName}: {symbolicEx.Message}");
                    return true;
                }

                LogOnce(app, LoggedFailures, tagName, $"{context} failed for {tagName}: {symbolicEx.Message}{FormatFallback(markerEx)}");
                return false;
            }
        }

        public static bool TryWriteSymbolicBool(IInstance instance, string tagName, bool value, CoSimulationPlcSimAdv.App app, string context)
        {
            try
            {
                instance.WriteBool(tagName, value);
                ClearLogState(tagName);
                return true;
            }
            catch (SimulationRuntimeException ex)
            {
                LogOnce(app, LoggedFailures, tagName, $"{context} failed for {tagName}: {ex.Message}");
                return false;
            }
        }

        public static bool TryReadSymbolicBool(IInstance instance, string tagName, out bool value, CoSimulationPlcSimAdv.App app, string context)
        {
            value = false;

            try
            {
                value = instance.ReadBool(tagName);
                ClearLogState(tagName);
                return true;
            }
            catch (SimulationRuntimeException ex)
            {
                LogOnce(app, LoggedFailures, tagName, $"{context} failed for {tagName}: {ex.Message}");
                return false;
            }
        }

        public static bool TryWriteInt16(IInstance instance, string tagName, short value, CoSimulationPlcSimAdv.App app, string context)
        {
            try
            {
                instance.WriteInt16(tagName, value);
                Exception ignoredMarkerError;
                TryWriteMarkerInt16(instance, tagName, value, out ignoredMarkerError);
                ClearLogState(tagName);
                return true;
            }
            catch (SimulationRuntimeException symbolicEx)
            {
                Exception markerEx;
                if (TryWriteMarkerInt16(instance, tagName, value, out markerEx))
                {
                    LogOnce(app, LoggedFallbacks, tagName, $"{context} used marker fallback for {tagName}: {symbolicEx.Message}");
                    return true;
                }

                LogOnce(app, LoggedFailures, tagName, $"{context} failed for {tagName}: {symbolicEx.Message}{FormatFallback(markerEx)}");
                return false;
            }
        }

        public static bool TryReadInt16(IInstance instance, string tagName, out short value, CoSimulationPlcSimAdv.App app, string context)
        {
            value = 0;

            try
            {
                value = instance.ReadInt16(tagName);
                ClearLogState(tagName);
                return true;
            }
            catch (SimulationRuntimeException symbolicEx)
            {
                Exception markerEx;
                if (TryReadMarkerInt16(instance, tagName, out value, out markerEx))
                {
                    LogOnce(app, LoggedFallbacks, tagName, $"{context} used marker fallback for {tagName}: {symbolicEx.Message}");
                    return true;
                }

                LogOnce(app, LoggedFailures, tagName, $"{context} failed for {tagName}: {symbolicEx.Message}{FormatFallback(markerEx)}");
                return false;
            }
        }

        public static bool TryWriteReal(IInstance instance, string tagName, float value, CoSimulationPlcSimAdv.App app, string context)
        {
            try
            {
                instance.WriteFloat(tagName, value);
                ClearLogState(tagName);
                return true;
            }
            catch (SimulationRuntimeException ex)
            {
                LogOnce(app, LoggedFailures, tagName, $"{context} failed for {tagName}: {ex.Message}");
                return false;
            }
        }

        public static bool TryReadReal(IInstance instance, string tagName, out float value, CoSimulationPlcSimAdv.App app, string context)
        {
            value = 0;

            try
            {
                value = instance.ReadFloat(tagName);
                ClearLogState(tagName);
                return true;
            }
            catch (SimulationRuntimeException ex)
            {
                LogOnce(app, LoggedFailures, tagName, $"{context} failed for {tagName}: {ex.Message}");
                return false;
            }
        }

        private static PlcIoAddress Marker(string address)
        {
            PlcIoAddress result;
            if (!PlcIoAddress.TryParseMarkerAddress(address, out result))
            {
                throw new ArgumentException("Invalid marker address.", nameof(address));
            }

            return result;
        }

        private static bool TryWriteMarkerBool(IInstance instance, string tagName, bool value, out Exception error)
        {
            error = null;

            PlcIoAddress address;
            if (!Addresses.TryGetValue(tagName, out address) || address.BitOffset == null)
            {
                return false;
            }

            try
            {
                instance.MarkerArea.WriteBit(address.ByteOffset, address.BitOffset.Value, value);
                return true;
            }
            catch (SimulationRuntimeException ex)
            {
                error = ex;
                return false;
            }
        }

        private static bool TryReadMarkerBool(IInstance instance, string tagName, out bool value, out Exception error)
        {
            value = false;
            error = null;

            PlcIoAddress address;
            if (!Addresses.TryGetValue(tagName, out address) || address.BitOffset == null)
            {
                return false;
            }

            try
            {
                value = instance.MarkerArea.ReadBit(address.ByteOffset, address.BitOffset.Value);
                return true;
            }
            catch (SimulationRuntimeException ex)
            {
                error = ex;
                return false;
            }
        }

        private static bool TryWriteMarkerInt16(IInstance instance, string tagName, short value, out Exception error)
        {
            error = null;

            PlcIoAddress address;
            if (!Addresses.TryGetValue(tagName, out address) || address.BitOffset != null)
            {
                return false;
            }

            try
            {
                unchecked
                {
                    var unsignedValue = (ushort)value;
                    instance.MarkerArea.WriteBytes(address.ByteOffset, new[]
                    {
                        (byte)(unsignedValue >> 8),
                        (byte)(unsignedValue & 0xFF)
                    });
                }

                return true;
            }
            catch (SimulationRuntimeException ex)
            {
                error = ex;
                return false;
            }
        }

        private static bool TryReadMarkerInt16(IInstance instance, string tagName, out short value, out Exception error)
        {
            value = 0;
            error = null;

            PlcIoAddress address;
            if (!Addresses.TryGetValue(tagName, out address) || address.BitOffset != null)
            {
                return false;
            }

            try
            {
                var bytes = instance.MarkerArea.ReadBytes(address.ByteOffset, 2);
                value = unchecked((short)((bytes[0] << 8) | bytes[1]));
                return true;
            }
            catch (SimulationRuntimeException ex)
            {
                error = ex;
                return false;
            }
        }

        private static string FormatFallback(Exception markerEx)
        {
            return markerEx == null ? string.Empty : $" Marker fallback failed: {markerEx.Message}";
        }

        private static void LogOnce(CoSimulationPlcSimAdv.App app, HashSet<string> logSet, string tagName, string message)
        {
            if (logSet.Add(tagName))
            {
                app?.LogStatus(message);
            }
        }

        private static void ClearLogState(string tagName)
        {
            LoggedFallbacks.Remove(tagName);
            LoggedFailures.Remove(tagName);
        }
    }
}
