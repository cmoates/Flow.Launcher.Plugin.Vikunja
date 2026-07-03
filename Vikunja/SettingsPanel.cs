using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Flow.Launcher.Plugin;
using Microsoft.Win32;

namespace Flow.Launcher.Plugin.Vikunja
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public partial class SettingsPanel : UserControl
    {
        private readonly PluginInitContext _context;
        private readonly Settings _settings;
        private readonly string _pluginId;
        private readonly bool _isDarkMode;
        private readonly SolidColorBrush _backgroundColor;
        private readonly SolidColorBrush _foregroundColor;
        private readonly SolidColorBrush _secondaryForegroundColor;
        private readonly SolidColorBrush _accentColor;

        public SettingsPanel(PluginInitContext context, Settings settings, string pluginId)
        {
            _context = context;
            _settings = settings;
            _pluginId = pluginId;
            
            // Detect dark mode from Windows settings
            _isDarkMode = IsWindowsInDarkMode();
            
            // Set colors to match Flow Launcher's theme
            if (_isDarkMode)
            {
                _backgroundColor = new SolidColorBrush(Color.FromRgb(42, 42, 42));
                _foregroundColor = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                _secondaryForegroundColor = new SolidColorBrush(Color.FromRgb(136, 136, 136));
                _accentColor = new SolidColorBrush(Color.FromRgb(91, 155, 213));
            }
            else
            {
                _backgroundColor = Brushes.White;
                _foregroundColor = Brushes.Black;
                _secondaryForegroundColor = Brushes.Gray;
                _accentColor = Brushes.DarkBlue;
            }
            
            InitializeComponent();
            LoadSettings();
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private bool IsWindowsInDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int i && i == 0;
            }
            catch
            {
                return false; // Default to light mode if we can't detect
            }
        }

        private void InitializeComponent()
        {
            this.Background = _backgroundColor;
            
            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20),
                Orientation = Orientation.Vertical
            };

            // Server URL
            stackPanel.Children.Add(new Label 
            { 
                Content = "Server URL:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(0),
                Foreground = _foregroundColor
            });
            
            var serverUrlTextBox = new TextBox 
            { 
                Name = "ServerUrlTextBox", 
                Height = 25,
                Width = 400,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 5),
                Background = _isDarkMode ? new SolidColorBrush(Color.FromRgb(51, 51, 51)) : Brushes.White,
                Foreground = _foregroundColor,
                BorderBrush = _isDarkMode ? new SolidColorBrush(Color.FromRgb(70, 70, 70)) : new SolidColorBrush(Color.FromRgb(171, 173, 179))
            };
            stackPanel.Children.Add(serverUrlTextBox);
            
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "The URL of your Vikunja instance (e.g., https://vikunja.example.com)",
                FontSize = 11,
                Foreground = _secondaryForegroundColor,
                Margin = new Thickness(0, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            });

            // API Token
            stackPanel.Children.Add(new Label 
            { 
                Content = "API Token:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(0),
                Foreground = _foregroundColor
            });
            
            var apiTokenTextBox = new TextBox 
            { 
                Name = "ApiTokenTextBox", 
                Height = 25,
                Width = 400,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 5),
                Background = _isDarkMode ? new SolidColorBrush(Color.FromRgb(51, 51, 51)) : Brushes.White,
                Foreground = _foregroundColor,
                BorderBrush = _isDarkMode ? new SolidColorBrush(Color.FromRgb(70, 70, 70)) : new SolidColorBrush(Color.FromRgb(171, 173, 179))
            };
            stackPanel.Children.Add(apiTokenTextBox);
            
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "Your Vikunja API token (create one in Vikunja Settings → API Tokens)\nThis will be stored as plaintext in the user data directory, please ensure it's not publicly accessible",
                FontSize = 11,
                Foreground = _secondaryForegroundColor,
                Margin = new Thickness(0, 0, 0, 5),
                TextWrapping = TextWrapping.Wrap
            });
            
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "Required API Permissions:",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = _accentColor,
                Margin = new Thickness(0, 5, 0, 3),
                TextWrapping = TextWrapping.Wrap
            });
            
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "• labels: create, read all\n• projects: read all, read one\n• tasks: create\n• tasksLabels: create",
                FontSize = 11,
                Foreground = _secondaryForegroundColor,
                Margin = new Thickness(15, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            });

            // Default Project ID
            stackPanel.Children.Add(new Label 
            { 
                Content = "Default Project ID:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(0),
                Foreground = _foregroundColor
            });
            
            var defaultProjectTextBox = new TextBox 
            { 
                Name = "DefaultProjectTextBox", 
                Height = 25,
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 5),
                Background = _isDarkMode ? new SolidColorBrush(Color.FromRgb(51, 51, 51)) : Brushes.White,
                Foreground = _foregroundColor,
                BorderBrush = _isDarkMode ? new SolidColorBrush(Color.FromRgb(70, 70, 70)) : new SolidColorBrush(Color.FromRgb(171, 173, 179))
            };
            stackPanel.Children.Add(defaultProjectTextBox);
            
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "Project ID to use when no project is specified (1 = Inbox)",
                FontSize = 11,
                Foreground = _secondaryForegroundColor,
                Margin = new Thickness(0, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            });

            // Parsing Mode
            stackPanel.Children.Add(new Label 
            { 
                Content = "Parsing Mode:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(0),
                Foreground = _foregroundColor
            });
            
            var parsingModeComboBox = new ComboBox 
            { 
                Name = "ParsingModeComboBox", 
                Height = 30,
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 5),
                Background = _isDarkMode ? new SolidColorBrush(Color.FromRgb(51, 51, 51)) : Brushes.White,
                Foreground = _foregroundColor,
                BorderBrush = _isDarkMode ? new SolidColorBrush(Color.FromRgb(70, 70, 70)) : new SolidColorBrush(Color.FromRgb(171, 173, 179))
            };
            parsingModeComboBox.Items.Add("Vikunja");
            parsingModeComboBox.Items.Add("Todoist");
            stackPanel.Children.Add(parsingModeComboBox);
            
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "Vikunja: +project *label !priority | Todoist: #project @label p1-p3",
                FontSize = 11,
                Foreground = _secondaryForegroundColor,
                Margin = new Thickness(0, 0, 0, 20),
                TextWrapping = TextWrapping.Wrap
            });

            // Save button
            var saveButton = new Button 
            { 
                Content = "Save", 
                Width = 80, 
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = _isDarkMode ? new SolidColorBrush(Color.FromRgb(51, 51, 51)) : new SolidColorBrush(Color.FromRgb(221, 221, 221)),
                Foreground = _foregroundColor,
                BorderBrush = _isDarkMode ? new SolidColorBrush(Color.FromRgb(70, 70, 70)) : new SolidColorBrush(Color.FromRgb(171, 173, 179))
            };
            saveButton.Click += SaveButton_Click;
            stackPanel.Children.Add(saveButton);

            Content = stackPanel;
        }

        private void LoadSettings()
        {
            if (Content is StackPanel stackPanel)
            {
                var config = _settings.GetConfiguration(_pluginId);
                
                var serverUrlTextBox = stackPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "ServerUrlTextBox");
                var apiTokenTextBox = stackPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "ApiTokenTextBox");
                var defaultProjectTextBox = stackPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "DefaultProjectTextBox");
                var parsingModeComboBox = stackPanel.Children.OfType<ComboBox>().FirstOrDefault(t => t.Name == "ParsingModeComboBox");

                if (serverUrlTextBox != null) serverUrlTextBox.Text = config.ServerUrl;
                if (apiTokenTextBox != null) apiTokenTextBox.Text = config.ApiToken;
                if (defaultProjectTextBox != null) defaultProjectTextBox.Text = config.DefaultProjectId.ToString();
                if (parsingModeComboBox != null) parsingModeComboBox.SelectedIndex = (int)config.ParsingMode;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (Content is StackPanel stackPanel)
            {
                var serverUrlTextBox = stackPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "ServerUrlTextBox");
                var apiTokenTextBox = stackPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "ApiTokenTextBox");
                var defaultProjectTextBox = stackPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "DefaultProjectTextBox");
                var parsingModeComboBox = stackPanel.Children.OfType<ComboBox>().FirstOrDefault(t => t.Name == "ParsingModeComboBox");

                var config = _settings.GetConfiguration(_pluginId);
                
                if (serverUrlTextBox != null) config.ServerUrl = serverUrlTextBox.Text.Trim();
                if (apiTokenTextBox != null) config.ApiToken = apiTokenTextBox.Text.Trim();
                
                if (defaultProjectTextBox != null && int.TryParse(defaultProjectTextBox.Text, out int projectId))
                    config.DefaultProjectId = projectId;

                if (parsingModeComboBox != null)
                    config.ParsingMode = (ParsingMode)parsingModeComboBox.SelectedIndex;

                _settings.SetConfiguration(_pluginId, config);
                
                _context.API.SavePluginSettings();
                MessageBox.Show("Settings saved successfully!", "Vikunja Plugin");
            }
        }
    }
}