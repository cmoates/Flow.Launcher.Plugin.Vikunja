using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Flow.Launcher.Plugin.Vikunja.Models;

namespace Flow.Launcher.Plugin.Vikunja
{
    public class VikunjaApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly Settings _settings;

        public VikunjaApiClient(Settings settings)
        {
            _settings = settings;
            _httpClient = new HttpClient();
            
            // Clear any existing authorization headers
            _httpClient.DefaultRequestHeaders.Authorization = null;
            
            if (!string.IsNullOrEmpty(_settings.ApiToken))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiToken}");
            }
            else
            {
            }
        }

        public async Task<bool> CreateTaskAsync(ParsedTask parsedTask, int? defaultProjectId = null)
        {
            try
            {
                // Determine project ID
                var projectId = defaultProjectId ?? 1; // Fallback to project 1 if nothing specified
                
                if (!string.IsNullOrEmpty(parsedTask.Project))
                {
                    // Try to find project by name (simplified - in a real implementation you might want to cache projects)
                    var foundProjectId = await FindProjectByNameAsync(parsedTask.Project);
                    if (foundProjectId.HasValue)
                    {
                        projectId = foundProjectId.Value;
                    }
                    // If project not found, we'll use the default project ID
                }

                // Create the task
                var vikujaTask = new VikujaTask
                {
                    Title = parsedTask.Title,
                    Description = parsedTask.Description,
                    DueDate = parsedTask.DueDate,
                    Priority = parsedTask.Priority,
                    ProjectId = projectId
                };

                // Configure JSON serializer to use ISO 8601 format for dates
                var jsonSettings = new JsonSerializerSettings
                {
                    DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ",
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    NullValueHandling = NullValueHandling.Ignore
                };

                var taskJson = JsonConvert.SerializeObject(vikujaTask, jsonSettings);
                var content = new StringContent(taskJson, Encoding.UTF8, "application/json");

                var url = $"{_settings.ServerUrl.TrimEnd('/')}/api/v1/projects/{projectId}/tasks";
                
                if (parsedTask.DueDate.HasValue)
                {
                }

                var response = await _httpClient.PutAsync(url, content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var createdTask = JsonConvert.DeserializeObject<VikujaTaskResponse>(responseContent);

                    // Add labels if any
                    if (parsedTask.Labels.Count > 0 && createdTask != null)
                    {
                        await AddLabelsToTaskAsync(createdTask.Id, parsedTask.Labels);
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<int?> FindProjectByNameAsync(string projectName)
        {
            try
            {
                var url = $"{_settings.ServerUrl.TrimEnd('/')}/api/v1/projects";
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    var projects = JsonConvert.DeserializeObject<List<VikujaProject>>(responseContent);
                    
                    var project = projects?.Find(p => 
                        string.Equals(p.Title, projectName, StringComparison.OrdinalIgnoreCase));
                    
                    if (project != null)
                    {
                        return project.Id;
                    }
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        public async Task<bool> AddLabelsToTaskAsync(int taskId, List<string> labelNames)
        {
            try
            {
                foreach (var labelName in labelNames)
                {
                    // First, try to find existing label
                    var labelId = await FindOrCreateLabelAsync(labelName);
                    if (labelId.HasValue)
                    {
                        // Add label to task using proper API format
                        var labelAssignment = new VikujaLabelTask { LabelId = labelId.Value };
                        var content = new StringContent(
                            JsonConvert.SerializeObject(labelAssignment), 
                            Encoding.UTF8, 
                            "application/json");

                        var url = $"{_settings.ServerUrl.TrimEnd('/')}/api/v1/tasks/{taskId}/labels";
                        
                        var response = await _httpClient.PutAsync(url, content);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<int?> FindOrCreateLabelAsync(string labelName)
        {
            try
            {
                // First try to find existing label
                var url = $"{_settings.ServerUrl.TrimEnd('/')}/api/v1/labels";
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var labels = JsonConvert.DeserializeObject<List<VikujaLabel>>(responseContent);
                    
                    var existingLabel = labels?.Find(l => 
                        string.Equals(l.Title, labelName, StringComparison.OrdinalIgnoreCase));
                    
                    if (existingLabel != null)
                    {
                        return existingLabel.Id;
                    }
                }

                // Create new label if not found
                var newLabel = new VikujaLabel
                {
                    Title = labelName,
                    HexColor = "#1973ff" // Default blue color
                };

                var labelJson = JsonConvert.SerializeObject(newLabel);
                var content = new StringContent(labelJson, Encoding.UTF8, "application/json");

                var createUrl = $"{_settings.ServerUrl.TrimEnd('/')}/api/v1/labels";
                
                var createResponse = await _httpClient.PutAsync(createUrl, content);
                var createContent = await createResponse.Content.ReadAsStringAsync();
                
                if (createResponse.IsSuccessStatusCode)
                {
                    var createdLabel = JsonConvert.DeserializeObject<VikujaLabel>(createContent);
                    return createdLabel?.Id;
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_settings.ServerUrl}/api/v1/user");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
