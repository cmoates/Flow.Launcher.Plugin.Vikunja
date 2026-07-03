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
        private VikunjaApiClient? _apiClient;
        private TaskParserService? _parser;

        public Task InitAsync(PluginInitContext context)
        {
            _context = context;
            _settings = context.API.LoadSettingJsonStorage<Settings>() ?? new Settings();
            _apiClient = new VikunjaApiClient(_settings);
            _parser = new TaskParserService();
            
            return Task.CompletedTask;
        }

        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            var results = new List<Result>();

            try
            {
                if (string.IsNullOrWhiteSpace(query.Search))
                {
                    var syntaxHelp = _settings?.ParsingMode == ParsingMode.Todoist
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
                if (string.IsNullOrEmpty(_settings?.ServerUrl) || string.IsNullOrEmpty(_settings?.ApiToken))
                {
                    results.Add(new Result
                    {
                        Title = "⚠️ Configuration Required",
                        SubTitle = $"Please configure server URL and API token in settings. Current: URL='{_settings?.ServerUrl}', Token='{(!string.IsNullOrEmpty(_settings?.ApiToken) ? "***" : "empty")}'",
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
                var parsedTask = _parser!.ParseTask(query.Search, _settings.ParsingMode);
                
                // Build the preview subtitle
                var subtitle = BuildPreviewSubtitle(parsedTask);
                
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
                                _apiClient = new VikunjaApiClient(_settings);
                                
                                var success = await _apiClient!.CreateTaskAsync(parsedTask, _settings.DefaultProjectId);
                                if (success)
                                {
                                    _context?.API.ShowMsg("Task Created", $"Successfully created task: {parsedTask.Title}");
                                }
                                else
                                {
                                    _context?.API.ShowMsg("Error", $"Failed to create task. Check Flow Launcher logs for details.\nServer: {_settings.ServerUrl}\nProject ID: {_settings.DefaultProjectId}");
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

        private string BuildPreviewSubtitle(ParsedTask task)
        {
            var parts = new List<string>();
            
            // Debug info first
            parts.Add($"Title:'{task.Title}'");
            
            if (task.Labels.Count > 0)
                parts.Add($"Labels:[{string.Join(",", task.Labels)}]");
                
            if (!string.IsNullOrEmpty(task.Project))
                parts.Add($"Project:{task.Project}");
            else if (_settings?.DefaultProjectId > 0)
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
            return new SettingsPanel(_context!, _settings!);
        }
    }
}