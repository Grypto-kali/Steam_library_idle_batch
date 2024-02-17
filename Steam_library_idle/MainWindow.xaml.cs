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

            string url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={apiKey}&steamid={steamId}&include_appinfo=1&format=json";

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
                                batchFile.WriteLine("color a");
                                batchFile.WriteLine("title game_loop");
                                batchFile.WriteLine(":start");
                                batchFile.WriteLine("cls");

                                int groupSize = 40;
                                int numGroups = (int)Math.Ceiling((double)games.Length / groupSize);

                                for (int i = 0; i < numGroups; i++)
                                {
                                    int startIndex = i * groupSize;
                                    int endIndex = Math.Min((i + 1) * groupSize, games.Length);

                                    for (int j = startIndex; j < endIndex; j++)
                                    {
                                        int appId = games[j].appid;
                                        string gameName = games[j].name;
                                        batchFile.WriteLine();
                                        batchFile.WriteLine($"echo {gameName}");
                                        batchFile.WriteLine($"start /min steam-idle.exe {appId}");
                                        batchFile.WriteLine();
                                    }

                                    batchFile.WriteLine("timeout 3600");
                                    batchFile.WriteLine("Taskkill /IM steam-idle.exe /F");
                                    batchFile.WriteLine("cls");
                                    batchFile.WriteLine("timeout 30");
                                    batchFile.WriteLine("cls");
                                }

                                batchFile.WriteLine("goto start");
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
