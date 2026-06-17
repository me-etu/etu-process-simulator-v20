using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace CoSimulationPlcSimAdv.Models
{
    public class DeviceUiUnitConfig
    {
        public string UnitName { get; set; }
        public DeviceUiConfig Config { get; set; }
    }

    public class DeviceUiConfig
    {
        public string description { get; set; }
        public List<DeviceUiAnalogInput> analogInputs { get; set; }
        public List<DeviceUiDigitalInput> digitalInputs { get; set; }
        public List<DeviceUiValve> valves { get; set; }
        public List<DeviceUiActuator> actuators { get; set; }
        public List<DeviceUiMarkerFallback> markerFallbacks { get; set; }
        public List<string> notes { get; set; }
    }

    public class DeviceUiAnalogInput
    {
        public string uiId { get; set; }
        public string displayName { get; set; }
        public string plcTag { get; set; }
        public string dataType { get; set; }
        public float minEngineeringValue { get; set; }
        public float maxEngineeringValue { get; set; }
        public short minRawValue { get; set; }
        public int maxRawValue { get; set; }
        public short defaultRawValue { get; set; }
        public int travelTimeSeconds { get; set; }
        public string qualityBitTag { get; set; }
        public DeviceUiAnalogControls uiControls { get; set; }
    }

    public class DeviceUiDigitalInput
    {
        public string uiId { get; set; }
        public string displayName { get; set; }
        public string plcTag { get; set; }
        public string dataType { get; set; }
        public bool normallyClosed { get; set; }
        public bool defaultValue { get; set; }
        public string qualityBitTag { get; set; }
        public DeviceUiDigitalControls uiControls { get; set; }
    }

    public class DeviceUiValve
    {
        public string uiId { get; set; }
        public string displayName { get; set; }
        public bool normallyClosed { get; set; }
        public int travelTimeMs { get; set; }
        public string controlTag { get; set; }
        public string feedbackOpenTag { get; set; }
        public string feedbackCloseTag { get; set; }
        public string feedbackOpenQualityBitTag { get; set; }
        public string feedbackCloseQualityBitTag { get; set; }
        public DeviceUiValveControls uiControls { get; set; }
    }

    public class DeviceUiActuator
    {
        public string uiId { get; set; }
        public string displayName { get; set; }
        public string controlTag { get; set; }
        public string feedbackOnTag { get; set; }
        public string feedbackQualityBitTag { get; set; }
        public DeviceUiActuatorControls uiControls { get; set; }
    }

    public class DeviceUiMarkerFallback
    {
        public string plcTag { get; set; }
        public string markerAddress { get; set; }
        public string dataType { get; set; }
    }

    public class DeviceUiAnalogControls
    {
        public string actualValueButton { get; set; }
        public string setpointButton { get; set; }
        public string setpointEdit { get; set; }
    }

    public class DeviceUiDigitalControls
    {
        public string actualValueButton { get; set; }
    }

    public class DeviceUiValveControls
    {
        public string simulateFeedbackButton { get; set; }
        public string forceOpenButton { get; set; }
        public string forceCloseButton { get; set; }
        public string openQualityErrorButton { get; set; }
        public string closeQualityErrorButton { get; set; }
        public string manualControlButton { get; set; }
    }

    public class DeviceUiActuatorControls
    {
        public string manualFeedbackButton { get; set; }
        public string forceOnButton { get; set; }
    }

    public static class DeviceUiConfigLoader
    {
        public static List<DeviceUiUnitConfig> LoadUnitConfigs()
        {
            var configDirectory = ResolveConfigDirectory();
            if (!Directory.Exists(configDirectory))
            {
                return new List<DeviceUiUnitConfig>();
            }

            var serializer = new JavaScriptSerializer();
            return Directory.GetFiles(configDirectory, "DeviceUi_*.json")
                .Where(path => !Path.GetFileName(path).Equals("DeviceUiTemplate.json", StringComparison.OrdinalIgnoreCase))
                .Select(path => new DeviceUiUnitConfig
                {
                    UnitName = GetUnitName(path),
                    Config = serializer.Deserialize<DeviceUiConfig>(File.ReadAllText(path))
                })
                .Where(unit => unit.Config != null)
                .OrderBy(unit => unit.UnitName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static bool ShouldSkipValve(DeviceUiUnitConfig unit, DeviceUiValve valve)
        {
            return unit != null
                && valve != null
                && string.Equals(unit.UnitName, "BL170", StringComparison.OrdinalIgnoreCase)
                && string.Equals(valve.uiId, "Q21", StringComparison.OrdinalIgnoreCase);
        }

        public static IEnumerable<DeviceUiMarkerFallback> GetActiveMarkerFallbacks(IEnumerable<DeviceUiUnitConfig> units)
        {
            var activeTags = new HashSet<string>(StringComparer.Ordinal);

            foreach (var unit in units)
            {
                foreach (var analog in unit.Config.analogInputs ?? new List<DeviceUiAnalogInput>())
                {
                    AddTag(activeTags, analog.plcTag);
                    AddTag(activeTags, analog.qualityBitTag);
                }

                foreach (var digital in unit.Config.digitalInputs ?? new List<DeviceUiDigitalInput>())
                {
                    AddTag(activeTags, digital.plcTag);
                    AddTag(activeTags, digital.qualityBitTag);
                }

                foreach (var valve in unit.Config.valves ?? new List<DeviceUiValve>())
                {
                    if (ShouldSkipValve(unit, valve))
                    {
                        continue;
                    }

                    AddTag(activeTags, valve.controlTag);
                    AddTag(activeTags, valve.feedbackOpenTag);
                    AddTag(activeTags, valve.feedbackOpenQualityBitTag);
                    AddTag(activeTags, valve.feedbackCloseTag);
                    AddTag(activeTags, valve.feedbackCloseQualityBitTag);
                }
            }

            return units
                .SelectMany(unit => unit.Config.markerFallbacks ?? new List<DeviceUiMarkerFallback>())
                .Where(fallback => fallback != null && activeTags.Contains(fallback.plcTag))
                .ToList();
        }

        public static IEnumerable<string> GetConfiguredBoolDiagnosticTags(IEnumerable<DeviceUiUnitConfig> units)
        {
            foreach (var unit in units)
            {
                foreach (var digital in unit.Config.digitalInputs ?? new List<DeviceUiDigitalInput>())
                {
                    if (IsBool(digital.dataType))
                    {
                        yield return digital.plcTag;
                        if (!string.IsNullOrWhiteSpace(digital.qualityBitTag))
                        {
                            yield return digital.qualityBitTag;
                        }
                    }
                }

                foreach (var valve in unit.Config.valves ?? new List<DeviceUiValve>())
                {
                    if (ShouldSkipValve(unit, valve))
                    {
                        continue;
                    }

                    yield return valve.controlTag;
                    yield return valve.feedbackOpenTag;
                    yield return valve.feedbackOpenQualityBitTag;
                    yield return valve.feedbackCloseTag;
                    yield return valve.feedbackCloseQualityBitTag;
                }
            }
        }

        public static IEnumerable<string> GetConfiguredInt16DiagnosticTags(IEnumerable<DeviceUiUnitConfig> units)
        {
            return units
                .SelectMany(unit => unit.Config.analogInputs ?? new List<DeviceUiAnalogInput>())
                .Where(analog => analog != null && IsInt16(analog.dataType))
                .Select(analog => analog.plcTag);
        }

        private static string ResolveConfigDirectory()
        {
            var outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs");
            if (Directory.Exists(outputDirectory))
            {
                return outputDirectory;
            }

            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Configs"));
        }

        private static string GetUnitName(string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            return fileName.StartsWith("DeviceUi_", StringComparison.OrdinalIgnoreCase)
                ? fileName.Substring("DeviceUi_".Length)
                : fileName;
        }

        private static bool IsBool(string dataType)
        {
            return dataType != null && dataType.Equals("Bool", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsInt16(string dataType)
        {
            return dataType != null && dataType.Equals("Int16", StringComparison.OrdinalIgnoreCase);
        }

        private static void AddTag(HashSet<string> tags, string tagName)
        {
            if (!string.IsNullOrWhiteSpace(tagName))
            {
                tags.Add(tagName);
            }
        }
    }
}
