
using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WpfApp1
{
    public partial class leftform : Window
    {


        private List<Panel> pnls = new List<Panel>();
        private Dictionary<string, Action> ActionMap = new Dictionary<string, Action>();
        private double ScaleFactor = 1.0; // WPF 默认处理 DPI，这里可能不需要显式缩放
        private bool into_snap_windows = false;
        private DispatcherTimer snapTimer;
        private ConfigData _currentConfig;
        private string appDirectory;
        private string configFilePath;

        public leftform()
        {
            InitializeComponent();


            this.ShowInTaskbar = false;
            appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string configDirectory = System.IO.Path.Combine(appDirectory, "config");
            configFilePath = System.IO.Path.Combine(configDirectory, "cfg.json");

            // 确保配置文件夹存在
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            // 首次启动或配置文件不存在时，可以创建默认配置文件
            if (!File.Exists(configFilePath))
            {
                CreateDefaultConfigFile();
            }

            // 加载配置
            LoadConfig();

            // 初始化 ActionMap
            ActionMap.Add("关机", () => System.Diagnostics.Process.Start("shutdown", "/s /t 0"));
            ActionMap.Add("重启", () => System.Diagnostics.Process.Start("shutdown", "/r /t 0"));
            ActionMap.Add("翻译", () => LaunchApp(_currentConfig?.Config?.Translator));
            ActionMap.Add("快捷", () =>
            {
                try
                {
                    // 运行选中的 EXE 文件
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_currentConfig?.Config?.Shortcut)
                    {
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法启动程序: {ex.Message}", "错误");
                }
            });
            ActionMap.Add("配置", () =>
            {
                setform sf = new setform();
                sf.Show();
            });
            ActionMap.Add("退出", () => Application.Current.Shutdown());

            // 初始化定时器
            snapTimer = new DispatcherTimer();
            snapTimer.Interval = TimeSpan.FromMilliseconds(100);
            snapTimer.Tick += SortLayout;
        }

        private void CreateDefaultConfigFile()
        {
            var defaultConfig = new ConfigData
            {
                Settings = new Dictionary<string, Setting>
                {
                    { "翻译", new Setting { ImageFilename = "icons8-translation-64.png", Path = "translate", Tooltip = "翻译" } },
                    { "快捷", new Setting { ImageFilename = "icons8-ergonomic-keyboard-100.png", Path = "shortcut", Tooltip = "快捷" } },
                    { "配置", new Setting { ImageFilename = "cfg.png", Path = "config", Tooltip = "配置" } },
                    { "退出", new Setting { ImageFilename = "icons8-esc-40.png", Path = "exit", Tooltip = "退出" } },
                    { "重启", new Setting { ImageFilename = "reset_hover.png", Path = "reboot", Tooltip = "重启" } },
                    { "关机", new Setting { ImageFilename = "close.png", Path = "shutdown", Tooltip = "关机" } },
                },
                Config = new Config { Layout = "left", Translator = "", Shortcut = "" },
                Exclusion = new Exclusion { Value = "" }
            };

            try
            {
                string jsonString = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configFilePath, jsonString);
                MessageBox.Show($"默认配置文件已创建：{configFilePath}", "提示");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建默认配置文件失败：{ex.Message}", "错误");
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    string jsonString = File.ReadAllText(configFilePath);
                    _currentConfig = JsonSerializer.Deserialize<ConfigData>(jsonString);

                    if (_currentConfig == null)
                    {
                        MessageBox.Show("加载配置文件失败，文件可能损坏。", "错误");
                    }
                }
                else
                {
                    MessageBox.Show("配置文件不存在。", "警告");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载配置文件时发生错误：{ex.Message}", "错误");
            }
        }

        private void SetConfigValue(string section, string key, string value)
        {
            // 这里需要实现更新 _currentConfig 并保存到文件的逻辑
            // 这只是一个占位符
            MessageBox.Show($"尝试设置配置: {section}.{key} = {value}");
        }

        private void LaunchApp(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法启动应用程序: {ex.Message}", "错误");
                }
            }
        }

        private void SortLayout(object sender, EventArgs e)
        {
            SnapTopWindows();
        }

        private void SnapTopWindows()
        {
            if (_currentConfig?.Config?.Layout == null) return;

            // 在 WPF 中获取鼠标位置
            Point mousePos = PointToScreen(Mouse.GetPosition(this));
            System.Drawing.Point lp = new System.Drawing.Point((int)mousePos.X, (int)mousePos.Y);
            System.Drawing.Rectangle boundsRect = new System.Drawing.Rectangle((int)Left, (int)Top, (int)Width, (int)Height);

            if (!boundsRect.Contains(lp) && !into_snap_windows)
            {
                into_snap_windows = true;

                if (_currentConfig.Config.Layout.ToLower() == "left")
                {
                    Left = -Width + 4;
                }
                else
                {
                    if (Left < SystemParameters.WorkArea.Width - Width)
                    {
                        Top = 0;
                        Left = SystemParameters.WorkArea.Width - Width + 40;
                    }
                }
                into_snap_windows = false;
            }
            else if (_currentConfig.Config.Layout.ToLower() == "left")
            {
                Left = 0;
            }
            else
            {
                Left = SystemParameters.WorkArea.Width - Width;
            }
        }

        private void ShowAApp(string imagePathNormal, string imageName, string imagePathHover)
        {
            Panel panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Height = 60,
                Background = new SolidColorBrush(Color.FromRgb(229, 229, 229)),
                Cursor = Cursors.Hand,
                Tag = imageName
            };

            panel.MouseDown += Panel_MouseDown;
            panel.MouseEnter += Panel_MouseEnter;
            panel.MouseLeave += Panel_MouseLeave;

            pnls.Add(panel);

            Image image = new Image
            {
                Source = new BitmapImage(new Uri(System.IO.Path.Combine(appDirectory, "imgapp", imagePathNormal), UriKind.Absolute)),
                Width = 46,
                Height = 46,
                Cursor = Cursors.Hand,
                Margin = new Thickness(7),
                Tag = imageName
            };
            panel.Children.Add(image);

            buttonPanel.Children.Add(panel);
        }

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int pref = (int)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
        }

        private void Panel_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Panel panel)
            {
                panel.Background = new SolidColorBrush(Color.FromRgb(200, 200, 200)); // 变深一点
            }
        }

        private void Panel_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Panel panel)
            {
                panel.Background = new SolidColorBrush(Color.FromRgb(229, 229, 229)); // 还原
            }
        }


        private void Panel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Panel panel)
            {
                string identifier = panel.Tag as string;
                if (ActionMap.ContainsKey(identifier))
                {
                    ActionMap[identifier]();
                }
            }
        }

        private void LeftForm_Loaded(object sender, RoutedEventArgs e)
        {

            ShowAApp("icons8-translation-64.png", "翻译", "icons8-google-translate-100.png");
            ShowAApp("icons8-ergonomic-keyboard-100.png", "快捷", "icons8-ergonomic-keyboard-100-hover.png");

            ShowAApp("icons8-esc-40.png", "退出", "icons8-esc-40.png");
            ShowAApp("cfg.png", "配置", "cfg.png");
            ShowAApp("reset_hover.png", "重启", "reset.png");
            ShowAApp("close_hover.png", "关机", "close_hover.png");

            // 计算总高度并设置窗口位置
            Height = 6 + pnls.Sum(p => p.Height);
            Top = (SystemParameters.WorkArea.Height - Height) / 2;

            // 启动定时器
            snapTimer.Start();
        }

        private void LeftForm_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        [DllImport("user32.dll")]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        internal static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        internal static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        internal const uint SWP_NOSIZE = 0x0001;
        internal const uint SWP_NOMOVE = 0x0002;
    }

}