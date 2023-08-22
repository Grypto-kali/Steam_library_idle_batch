using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json.Linq;

namespace SteamGameLauncher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void LaunchGamesButton_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = apiKeyPasswordBox.Password;
            string steamId = steamIdTextBox.Text;

            string url = $"http://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={apiKey}&steamid={steamId}&include_appinfo=1&format=json";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    statusTextBlock.Text = "Executing API request...";
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        dynamic data = JObject.Parse(json);

                        if (data.response != null && data.response.games != null)
                        {
                            JArray gamesArray = data.response.games;
                            dynamic[] games = gamesArray.ToObject<dynamic[]>();
                            using (StreamWriter batchFile = new StreamWriter("launch_games.bat", false))
                            {
                                batchFile.WriteLine("@echo off");
                                foreach (var game in games)
                                {
                                    int appId = game.appid;
                                    string gameName = game.name;
                                    string batchContent = $"echo {gameName}\nstart /min steam-idle.exe {appId}\ntimeout 1 > nul\n";
                                    batchFile.WriteLine(batchContent);
                                }
                            }
                            statusTextBlock.Text = "Batch file created (launch_games.bat).";
                            statusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96));
                        }
                        else
                        {
                            statusTextBlock.Text = "No games found for the user.";
                        }
                    }
                    else
                    {
                        statusTextBlock.Text = "Request error. Status code: " + response.StatusCode;
                        statusTextBlock.Foreground = Brushes.Red;
                    }
                }
                catch (Exception ex)
                {
                    statusTextBlock.Text = "An error occurred: " + ex.Message;
                    statusTextBlock.Foreground = Brushes.Red;
                }
            }
        }
    }
}
