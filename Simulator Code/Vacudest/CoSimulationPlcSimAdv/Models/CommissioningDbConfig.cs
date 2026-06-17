using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace CoSimulationPlcSimAdv.Models
{
    public class CommissioningDbConfig
    {
        public string sourceFile { get; set; }
        public string sourceImportedAtUtc { get; set; }
        public string dbName { get; set; }
        public List<CommissioningDbVariable> variables { get; set; }
    }

    public class CommissioningDbVariable
    {
        public string uiId { get; set; }
        public string displayName { get; set; }
        public string plcTag { get; set; }
        public string dataType { get; set; }
        public float defaultValue { get; set; }
    }

    public class CommissioningDbImportResult
    {
        public CommissioningDbConfig Config { get; set; }
        public List<string> SkippedLines { get; set; }
        public string SavedPath { get; set; }
    }

    public static class CommissioningDbConfigLoader
    {
        private static readonly Regex DbNamePattern = new Regex(
            @"^\s*DATA_BLOCK\s+""(?<name>[^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex VariablePattern = new Regex(
            @"^\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(\{.*?\})?\s*:\s*(?<type>[A-Za-z][A-Za-z0-9_]*)\s*;",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static List<CommissioningDbConfig> LoadConfigs()
        {
            var configDirectory = ResolveConfigDirectory();
            if (!Directory.Exists(configDirectory))
            {
                return new List<CommissioningDbConfig>();
            }

            var serializer = new JavaScriptSerializer();
            return Directory.GetFiles(configDirectory, "CommissioningDb_*.json")
                .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                .Select(path => serializer.Deserialize<CommissioningDbConfig>(File.ReadAllText(path)))
                .Where(config => config != null && config.variables != null)
                .Select(NormalizeConfig)
                .OrderBy(config => config.dbName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static CommissioningDbImportResult ImportAndSave(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                throw new ArgumentException("No DB source file was selected.", nameof(sourcePath));
            }

            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("The selected DB source file was not found.", sourcePath);
            }

            var result = Parse(File.ReadAllLines(sourcePath));
            result.Config.sourceFile = sourcePath;
            result.Config.sourceImportedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var configDirectory = ResolveConfigDirectory();
            Directory.CreateDirectory(configDirectory);

            var fileName = "CommissioningDb_" + ToSafeFileName(result.Config.dbName) + ".json";
            var targetPath = Path.Combine(configDirectory, fileName);
            var serializer = new JavaScriptSerializer();
            File.WriteAllText(targetPath, serializer.Serialize(result.Config));
            result.SavedPath = targetPath;
            return result;
        }

        public static IEnumerable<string> GetBoolDiagnosticTags(IEnumerable<CommissioningDbConfig> configs)
        {
            return GetDiagnosticTags(configs, "Bool");
        }

        public static IEnumerable<string> GetRealDiagnosticTags(IEnumerable<CommissioningDbConfig> configs)
        {
            return GetDiagnosticTags(configs, "Real");
        }

        private static CommissioningDbImportResult Parse(IEnumerable<string> lines)
        {
            string dbName = null;
            var inVarBlock = false;
            var variables = new List<CommissioningDbVariable>();
            var skippedLines = new List<string>();

            foreach (var line in lines)
            {
                if (dbName == null)
                {
                    var dbMatch = DbNamePattern.Match(line);
                    if (dbMatch.Success)
                    {
                        dbName = dbMatch.Groups["name"].Value;
                    }
                }

                if (!inVarBlock)
                {
                    if (line.Trim().Equals("VAR", StringComparison.OrdinalIgnoreCase))
                    {
                        inVarBlock = true;
                    }

                    continue;
                }

                if (line.Trim().Equals("END_VAR", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var variableMatch = VariablePattern.Match(line);
                if (!variableMatch.Success)
                {
                    skippedLines.Add("Unsupported declaration: " + line.Trim());
                    continue;
                }

                var dataType = variableMatch.Groups["type"].Value;
                if (!IsSupportedDataType(dataType))
                {
                    skippedLines.Add("Skipped " + variableMatch.Groups["name"].Value + ": unsupported type " + dataType);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(dbName))
                {
                    continue;
                }

                var memberName = variableMatch.Groups["name"].Value;
                variables.Add(new CommissioningDbVariable
                {
                    uiId = ToUiId(dbName + "_" + memberName),
                    displayName = memberName,
                    plcTag = FormatPlcTag(dbName, memberName),
                    dataType = NormalizeDataType(dataType),
                    defaultValue = 0
                });
            }

            if (string.IsNullOrWhiteSpace(dbName))
            {
                throw new InvalidOperationException("Import failed: no DATA_BLOCK name was found.");
            }

            if (variables.Count == 0)
            {
                throw new InvalidOperationException("Import failed: no supported top-level Bool or Real variables were found.");
            }

            return new CommissioningDbImportResult
            {
                Config = new CommissioningDbConfig
                {
                    dbName = dbName,
                    variables = variables
                },
                SkippedLines = skippedLines
            };
        }

        private static IEnumerable<string> GetDiagnosticTags(IEnumerable<CommissioningDbConfig> configs, string dataType)
        {
            return configs
                .Where(config => config != null)
                .SelectMany(config => config.variables ?? new List<CommissioningDbVariable>())
                .Where(variable => variable != null && string.Equals(variable.dataType, dataType, StringComparison.OrdinalIgnoreCase))
                .Select(variable => variable.plcTag);
        }

        private static CommissioningDbConfig NormalizeConfig(CommissioningDbConfig config)
        {
            if (config == null || config.variables == null || string.IsNullOrWhiteSpace(config.dbName))
            {
                return config;
            }

            foreach (var variable in config.variables)
            {
                if (variable == null || string.IsNullOrWhiteSpace(variable.displayName))
                {
                    continue;
                }

                var quotedTag = "\"" + config.dbName + "\"." + variable.displayName;
                if (string.Equals(variable.plcTag, quotedTag, StringComparison.Ordinal))
                {
                    variable.plcTag = FormatPlcTag(config.dbName, variable.displayName);
                }
            }

            return config;
        }

        private static bool IsSupportedDataType(string dataType)
        {
            return dataType != null
                && (dataType.Equals("Bool", StringComparison.OrdinalIgnoreCase)
                    || dataType.Equals("Real", StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeDataType(string dataType)
        {
            return dataType.Equals("Bool", StringComparison.OrdinalIgnoreCase) ? "Bool" : "Real";
        }

        private static string FormatPlcTag(string dbName, string memberName)
        {
            return dbName + "." + memberName;
        }

        private static string ResolveConfigDirectory()
        {
            var sourceDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Configs"));
            if (Directory.Exists(sourceDirectory))
            {
                return sourceDirectory;
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs");
        }

        private static string ToSafeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        }

        private static string ToUiId(string value)
        {
            return Regex.Replace(value, @"[^A-Za-z0-9_]", "_");
        }
    }
}
