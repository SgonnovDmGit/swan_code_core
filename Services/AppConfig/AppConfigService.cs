using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SwanCode.Core.Services.AppConfig
{
    public class ApiKeyConfig
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("useOwnKey")]
        public bool UseOwnKey { get; set; }
    }

    public class AppConfig
    {
        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "Light";

        [JsonPropertyName("language")]
        public string Language { get; set; } = "Ru";

        [JsonPropertyName("serverAddress")]
        public string ServerAddress { get; set; } = "localhost:8080";

        [JsonPropertyName("selectedProvider")]
        public string SelectedProvider { get; set; } = string.Empty;

        [JsonPropertyName("selectedModel")]
        public string SelectedModel { get; set; } = string.Empty;

        [JsonPropertyName("selectedSubProvider")]
        public string SelectedSubProvider { get; set; } = string.Empty;

        [JsonPropertyName("selectedFramework")]
        public string SelectedFramework { get; set; } = string.Empty;

        [JsonPropertyName("selectedIde")]
        public string SelectedIde { get; set; } = string.Empty;

        [JsonPropertyName("frameworkVersion")]
        public string FrameworkVersion { get; set; } = string.Empty;

        [JsonPropertyName("projectPath")]
        public string ProjectPath { get; set; } = string.Empty;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("conversationMode")]
        public bool ConversationMode { get; set; } = true;

        [JsonPropertyName("useThinking")]
        public bool UseThinking { get; set; }

        [JsonPropertyName("debugMode")]
        public bool DebugMode { get; set; }

        [JsonPropertyName("minimizeToTray")]
        public bool MinimizeToTray { get; set; }

        [JsonPropertyName("alwaysOnTop")]
        public bool AlwaysOnTop { get; set; }

        [JsonPropertyName("userKey")]
        public string UserKey { get; set; } = string.Empty;

        [JsonPropertyName("apiKeys")]
        public Dictionary<string, ApiKeyConfig> ApiKeys { get; set; } = new();
    }

    public static class AppConfigService
    {
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SwanCodeClient");

        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public static AppConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                    return new AppConfig();

                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }

        public static void LoadEnvFile()
        {
            try
            {
                var envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
                if (!File.Exists(envPath))
                    return;

                foreach (var line in File.ReadAllLines(envPath))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                        continue;

                    var eqIdx = trimmed.IndexOf('=');
                    if (eqIdx <= 0) continue;

                    var key = trimmed[..eqIdx].Trim();
                    var val = trimmed[(eqIdx + 1)..].Trim();
                    Environment.SetEnvironmentVariable(key, val);
                }
            }
            catch
            {
                // Ignore .env read errors
            }
        }

        public static void Save(AppConfig config)
        {
            try
            {
                if (!Directory.Exists(ConfigDir))
                    Directory.CreateDirectory(ConfigDir);

                var json = JsonSerializer.Serialize(config, JsonOptions);
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // Ignore save errors silently
            }
        }
    }
}
