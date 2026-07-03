using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.Vikunja
{
    public class Main : IAsyncPlugin, ISettingProvider
    {
        private PluginInitContext? _context;
        private Settings? _settings;
        private string? _pluginId;
        private VikunjaApiClient? _apiClient;
        private TaskParserService? _parser;

        public Task InitAsync(PluginInitContext context)
        {
            _context = context;
            _pluginId = context.CurrentPluginMetadata.ID;
            _settings = context.API.LoadSettingJsonStorage<Settings>() ?? new Settings();
            
            // Migrate old settings format to multitenant format if needed
            _settings.MigrateIfNeeded(_pluginId);
            
            // Initialize API client with this plugin's configuration
            var config = _settings.GetConfiguration(_pluginId);
            _apiClient = new VikunjaApiClient(config);
            _parser = new TaskParserService();
            
            return Task.CompletedTask;
        }

        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            var results = new List<Result>();

            try
            {
                var config = _settings?.GetConfiguration(_pluginId!);

                if (string.IsNullOrWhiteSpace(query.Search))
                {
                    var syntaxHelp = config?.ParsingMode == ParsingMode.Todoist
                        ? "Enter a task with optional due date and tags (#project, p1-p5, @label)"
                        : "Enter a task with optional due date and tags (+project, !1-5, *label)";

                    results.Add(new Result
                    {
                        Title = "Vikunja Quick Add",
                        SubTitle = syntaxHelp,
                        IcoPath = "icon.png"
                    });
                    return results;
                }

                // Validate settings
                if (string.IsNullOrEmpty(config?.ServerUrl) || string.IsNullOrEmpty(config?.ApiToken))
                {
                    results.Add(new Result
                    {
                        Title = "⚠️ Configuration Required",
                        SubTitle = $"Please configure server URL and API token in settings. Current: URL='{config?.ServerUrl}', Token='{(!string.IsNullOrEmpty(config?.ApiToken) ? "***" : "empty")}'",
                        IcoPath = "icon.png",
                        Action = _ =>
                        {
                            _context?.API.OpenSettingDialog();
                            return true;
                        }
                    });
                    return results;
                }

                // Parse the task
                var parsedTask = _parser!.ParseTask(query.Search, config!.ParsingMode);
                
                // Build the preview subtitle
                var subtitle = BuildPreviewSubtitle(parsedTask, config);
                
                results.Add(new Result
                {
                    Title = $"Create task: {parsedTask.Title}",
                    SubTitle = subtitle,
                    IcoPath = "icon.png",
                    Action = _ =>
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                // Refresh settings and API client before making the call
                                _settings = _context?.API.LoadSettingJsonStorage<Settings>() ?? new Settings();
                                _settings.MigrateIfNeeded(_pluginId!);
                                var currentConfig = _settings.GetConfiguration(_pluginId!);
                                _apiClient = new VikunjaApiClient(currentConfig);
                                
                                var success = await _apiClient!.CreateTaskAsync(parsedTask, currentConfig.DefaultProjectId);
                                if (success)
                                {
                                    _context?.API.ShowMsg("Task Created", $"Successfully created task: {parsedTask.Title}");
                                }
                                else
                                {
                                    _context?.API.ShowMsg("Error", $"Failed to create task. Check Flow Launcher logs for details.\nServer: {currentConfig.ServerUrl}\nProject ID: {currentConfig.DefaultProjectId}");
                                }
                            }
                            catch (Exception ex)
                            {
                                _context?.API.ShowMsg("Error", $"Exception: {ex.Message}");
                            }
                        });
                        return true;
                    }
                });
            }
            catch (Exception ex)
            {
                results.Add(new Result
                {
                    Title = "Error parsing task",
                    SubTitle = ex.Message,
                    IcoPath = "icon.png"
                });
            }

            return results;
        }

        private string BuildPreviewSubtitle(ParsedTask task, PluginConfiguration config)
        {
            var parts = new List<string>();
            
            // Debug info first
            parts.Add($"Title:'{task.Title}'");
            
            if (task.Labels.Count > 0)
                parts.Add($"Labels:[{string.Join(",", task.Labels)}]");
                
            if (!string.IsNullOrEmpty(task.Project))
                parts.Add($"Project:{task.Project}");
            else if (config.DefaultProjectId > 0)
                parts.Add("Project:Default");
                
            if (task.DueDate.HasValue)
            {
                // Show time only if it's not midnight (00:00)
                if (task.DueDate.Value.TimeOfDay != TimeSpan.Zero)
                {
                    parts.Add($"Due:{task.DueDate.Value:MMM dd, yyyy HH:mm}");
                }
                else
                {
                    parts.Add($"Due:{task.DueDate.Value:MMM dd, yyyy}");
                }
            }
                
            if (task.Priority > 0)
            {
                var priorityName = task.Priority switch
                {
                    1 => "Low",
                    2 => "Medium",
                    3 => "High",
                    4 => "Urgent",
                    5 => "DO NOW",
                    _ => task.Priority.ToString()
                };
                parts.Add($"Priority:{priorityName}");
            }

            return string.Join(" | ", parts);
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public System.Windows.Controls.Control CreateSettingPanel()
        {
            return new SettingsPanel(_context!, _settings!, _pluginId!);
        }
    }
}