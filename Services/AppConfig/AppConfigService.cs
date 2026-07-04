using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SwanCode.Core.Services.AppConfig
{
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
    }

    public static class AppConfigService
    {
        /// <summary>Legacy-папка, которую до v0.12.x делили оба продукта.</summary>
        private const string LegacyFolderName = "SwanCodeClient";

        private static string _folderName = LegacyFolderName;

        /// <summary>
        /// Задаёт продуктовую папку настроек (IProductBranding.ConfigFolderName) и один раз
        /// мигрирует *.json из legacy-папки. Вызывать в App.OnStartup до первого Load().
        /// Без вызова работает legacy-путь — поведение до v0.12.x.
        /// </summary>
        public static void Initialize(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                return;

            _folderName = folderName;
            MigrateFromLegacy();
        }

        /// <summary>Полный путь продуктовой папки настроек (для window.json и т.п.).</summary>
        public static string ConfigDirectory => ConfigDir;

        private static string ConfigDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            _folderName);

        private static string ConfigPath => Path.Combine(ConfigDir, "config.json");

        private static void MigrateFromLegacy()
        {
            try
            {
                if (_folderName == LegacyFolderName)
                    return;

                var legacyDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    LegacyFolderName);
                if (!Directory.Exists(legacyDir))
                    return;

                // Копируем только отсутствующие файлы; legacy-папку не трогаем —
                // второй продукт мигрирует из неё независимо.
                foreach (var src in Directory.GetFiles(legacyDir, "*.json"))
                {
                    var dst = Path.Combine(ConfigDir, Path.GetFileName(src));
                    if (File.Exists(dst))
                        continue;

                    Directory.CreateDirectory(ConfigDir);
                    File.Copy(src, dst);
                }
            }
            catch
            {
                // Миграция best-effort: при сбое продукт стартует с дефолтами
            }
        }

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
