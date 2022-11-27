using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BondReader;
using BondReader.Schemas;
using Halo_Forge_Bot.GameUI;
using Halo_Forge_Bot.Utilities;
using Memory;
using Microsoft.Win32;
using Newtonsoft.Json;
using Serilog;
using Serilog.Formatting.Compact;
using static Bond.Deserialize;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Utils = Halo_Forge_Bot.Utilities.Utils;

namespace Halo_Forge_Bot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.WithThreadId()
                .WriteTo.Console()
                .WriteTo.File($"{Utils.ExePath}/log.txt")
                .WriteTo.File(new CompactJsonFormatter(), $"{Utils.ExePath}/log.json")
                .WriteTo.Debug()
                .CreateLogger();

            Log.Information("----------APP START----------");
            
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);

            Directory.CreateDirectory(Utils.ExePath + "/images/");

            InitializeComponent();
            Input.InitInput();
        }

        private BondSchema? _selectedMap;
        public static string? SelecteMapPath;
        public static bool resume;
        public static string mapName;

        private void LoadMvar_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "mvar files (*.mvar)|*.mvar|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                _selectedMap = BondHelper.ProcessFile<BondSchema>(openFileDialog.FileName);
                SelecteMapPath = Path.GetDirectoryName(openFileDialog.FileName);
                MapItemCount.Content = _selectedMap.Items.Count;
                string estimate = $"{Math.Round(TimeSpan.FromSeconds(_selectedMap.Items.Count * 7).TotalHours, 2)}h";
                EstimatedTime.Content = estimate;
                mapName = openFileDialog.SafeFileName;
                
                mvarLoaded.Text = $"MVAR Loaded- {openFileDialog.SafeFileName}";
                if (File.Exists(Utils.ExePath + $"/recovery/currentObjectRecoveryIndex-{mapName}.json"))
                {
                    var result = MessageBox.Show("It looks like you've run the bot for this mvar file previously. Do you want to resume the previous session?", "Reload", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            resume = true;
                            _selectedMap = null;
                            MapItemCount.Content = "";
                            EstimatedTime.Content = "";
                            LoadMvar.IsEnabled = false;
                            mvarLoaded.Text = "MVAR Loaded - None";
                            resumeCb.IsChecked = true;
                            break;
                        case MessageBoxResult.No:
                            resume = false;                           
                            resumeCb.IsChecked = false;
                            break;
                    }
                }
            }
        }

        private async void StartBot_OnClick(object sender, RoutedEventArgs e)
        {
            if (_selectedMap == null && resume == false)
            {
                Log.Error("Selected map is null, select a map first");
                return;
            }

            Log.Information("-----STARTING BOT-----");
            await Bot.StartBot(_selectedMap, mapName, int.Parse(ItemRangeStart.Text), int.Parse(ItemRangeEnd.Text), resume);
            Log.Information("-----STOPPING BOT-----");
        }

        private async void HandleResumeCheck(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if ((bool)cb.IsChecked)
            {
                resume = true;
                _selectedMap = null;
                MapItemCount.Content = "";
                EstimatedTime.Content = "";
                LoadMvar.IsEnabled = false;
                mvarLoaded.Text = "MVAR Loaded - None";
            }
            else
            {
                resume = false;
                LoadMvar.IsEnabled = true;
            }
        }
    }
}