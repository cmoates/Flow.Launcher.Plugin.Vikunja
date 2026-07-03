using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Flow.Launcher.Plugin.Vikunja
{
    public class Settings
    {
        /// <summary>
        /// Multitenant configuration storage keyed by plugin ID.
        /// Each entry contains the configuration for that plugin instance.
        /// </summary>
        [JsonProperty("configurations")]
        public Dictionary<string, PluginConfiguration> Configurations { get; set; } = new();

        /// <summary>
        /// Legacy fields for backward compatibility. Used during migration.
        /// </summary>
        [JsonProperty("serverUrl")]
        public string? ServerUrl { get; set; }

        [JsonProperty("apiToken")]
        public string? ApiToken { get; set; }

        [JsonProperty("defaultProjectId")]
        public int? DefaultProjectId { get; set; }

        [JsonProperty("parsingMode")]
        public ParsingMode? ParsingMode { get; set; }

        /// <summary>
        /// Migrates old single-tenant settings to new multitenant format if needed.
        /// Call this after loading settings from storage.
        /// When a new plugin instance loads for the first time with no existing config,
        /// it will copy settings from another instance if available (more likely to be correct).
        /// </summary>
        public void MigrateIfNeeded(string pluginId)
        {
            // If Configurations already has this plugin's ID, migration already happened
            if (Configurations.ContainsKey(pluginId))
            {
                return;
            }

            // Check if old format settings exist
            if (!string.IsNullOrEmpty(ServerUrl) || !string.IsNullOrEmpty(ApiToken))
            {
                // Migrate old settings to new format under this plugin's ID
                Configurations[pluginId] = new PluginConfiguration
                {
                    ServerUrl = ServerUrl ?? "",
                    ApiToken = ApiToken ?? "",
                    DefaultProjectId = DefaultProjectId ?? 1,
                    ParsingMode = ParsingMode ?? ParsingMode.Vikunja
                };

                // Clear old fields so they don't interfere
                ServerUrl = null;
                ApiToken = null;
                DefaultProjectId = null;
                ParsingMode = null;
            }
            else if (!Configurations.ContainsKey(pluginId))
            {
                // No old settings and no config for this plugin yet
                if (Configurations.Count > 0)
                {
                    // Copy settings from another instance if available
                    // This is more likely to be correct than empty defaults, since typically
                    // the same Vikunja server URL and token are reused for multiple keywords
                    var existingConfig = Configurations.Values.First();
                    Configurations[pluginId] = new PluginConfiguration
                    {
                        ServerUrl = existingConfig.ServerUrl,
                        ApiToken = existingConfig.ApiToken,
                        DefaultProjectId = existingConfig.DefaultProjectId,
                        ParsingMode = existingConfig.ParsingMode
                    };
                }
                else
                {
                    // No existing configs - create default
                    Configurations[pluginId] = new PluginConfiguration();
                }
            }
        }

        /// <summary>
        /// Gets the configuration for a specific plugin instance.
        /// </summary>
        public PluginConfiguration GetConfiguration(string pluginId)
        {
            if (!Configurations.ContainsKey(pluginId))
            {
                Configurations[pluginId] = new PluginConfiguration();
            }
            return Configurations[pluginId];
        }

        /// <summary>
        /// Updates the configuration for a specific plugin instance.
        /// </summary>
        public void SetConfiguration(string pluginId, PluginConfiguration config)
        {
            Configurations[pluginId] = config;
        }
    }

    /// <summary>
    /// Configuration for a single plugin instance.
    /// </summary>
    public class PluginConfiguration
    {
        [JsonProperty("serverUrl")]
        public string ServerUrl { get; set; } = "";

        [JsonProperty("apiToken")]
        public string ApiToken { get; set; } = "";

        [JsonProperty("defaultProjectId")]
        public int DefaultProjectId { get; set; } = 1;

        [JsonProperty("parsingMode")]
        public ParsingMode ParsingMode { get; set; } = ParsingMode.Vikunja;
    }

    public enum ParsingMode
    {
        Vikunja,
        Todoist
    }
}