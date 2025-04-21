
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using WpfApp1;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WpfApp
{
    public partial class MainWindow : Window
    {

        DockController dc = new DockController();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 计算 3 厘米对应的像素值
            double centimeters = 3;
            double inches = centimeters / 2.54;
            dc._snapThresholdPixels = inches * System.Windows.Media.VisualTreeHelper.GetDpi(this).PixelsPerInchY;

            dc._hiddenTopPosition = -dc._snapThresholdPixels; // 初始隐藏在屏幕上方一点
            Top = dc._hiddenTopPosition; // 初始时将窗口隐藏起来

            dc._positionCheckTimer = new DispatcherTimer();
            dc._positionCheckTimer.Interval = TimeSpan.FromMilliseconds(200);
            dc._positionCheckTimer.Tick += PositionCheckTimer_Tick;
            dc._positionCheckTimer.Start();


            leftform leftForm = new leftform();
            leftForm.Show();

        }

        public void updateIcon()
        {
            SetupWindow();
            InitializeDock();

        }

        public MainWindow()
        {
            InitializeComponent();

            this.ShowInTaskbar = false;
            //AllocConsole();
            //Console.WriteLine("Hello from WPF to Console!");

            // 构建配置文件的完整路径
            dc.appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string configDirectory = Path.Combine(dc.appDirectory, "config");
            dc._configFilePath = Path.Combine(configDirectory, "cfg.json");

            // 确保配置文件夹存在
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            // 首次启动或配置文件不存在时，可以创建默认配置文件
            if (!File.Exists(dc._configFilePath))
            {
                CreateDefaultConfigFile();
            }

            // 加载配置
            LoadConfig();

            dc.imagePaths = _currentConfig.Settings.Values.Select(s => Path.Combine(dc.appDirectory, "img", s.ImageFilename)).ToArray();
            dc.ItemBaseSize = _currentConfig.Config.Nodesize;

            Loaded += MainWindow_Loaded;
            LocationChanged += MainWindow_LocationChanged;
            MouseEnter += MainWindow_MouseEnter;

            MouseUp += MainWindow_MouseUp;
            MouseWheel += MainWindow_MouseWheel; // Add MouseWheel event handler
            Closing += MainWindow_Closing; // 确保保存配置


            SetupWindow();
            InitializeDock();
        }

        private void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {

            double newSize = dc.ItemBaseSize;
            if (e.Delta > 0)
                newSize = Math.Round(newSize * 1.1); // Scale up by 10%
            else
                newSize = Math.Round(newSize * 0.9); // Scale down by 10%

            // Ensure node size stays within reasonable bounds
            newSize = Math.Max(20, Math.Min(100, newSize));

            dc.ItemBaseSize = newSize;
            _currentConfig.Config.Nodesize = (int)newSize;

            // Save updated config
            SaveConfig();

            InitializeDock();
            SetupWindow();
            e.Handled = true;
        }

        private void SetupWindow()
        {
            string backgroundImagePath = Path.Combine(dc.appDirectory, "img", _currentConfig.Config.Bg);
            var backgroundImage = new System.Windows.Controls.Image
            {
                Source = new BitmapImage(new Uri(backgroundImagePath, UriKind.RelativeOrAbsolute)),
                Stretch = Stretch.UniformToFill
            };

            backgroundImage.MouseLeftButtonUp += BackgroundImage_MouseLeftButtonUp;

            // 加到 Grid 最底层（作为背景）
            mainGrid.Children.Insert(0, backgroundImage); // 插入到最底层

            double totalBaseWidth = dc.imagePaths.Length * (dc.ItemBaseSize + dc.Padding1);
            Width = totalBaseWidth + dc.Padding1;
            //Height = dc.ItemBaseSize + 2 * dc.DesiredMargin + dc.ExtraToolTipSpace;
            Height = 68 + 2 * dc.DesiredMargin + dc.ExtraToolTipSpace;

            imageContainer.VerticalAlignment = VerticalAlignment.Center;

            // 设置窗口初始位置
            if (_currentConfig.Config != null)
            {
                if (double.TryParse(_currentConfig.Config.Left, out double left))
                {
                    //Left = left;
                    Left = (SystemParameters.WorkArea.Width - Width) / 2;
                }
                if (double.TryParse(_currentConfig.Config.Top, out double top))
                {
                    Top = top;
                }
                _currentConfig.Config.Left = Left.ToString();
                _currentConfig.Config.Top = Top.ToString();
            }

        }

        private void BackgroundImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dc.EventDef.IsLeftClick = false;
            int currentX = (int)e.GetPosition(this).X;
            int currentY = (int)e.GetPosition(this).Y;
            dc.EventDef.X = currentX;
            dc.EventDef.Y = currentY;
        }

        private void InitializeDock()
        {
            dc.dockItems.Clear();
            dc.itemData.Clear();
            dc.animations.Clear();
            imageContainer.Children.Clear();
            int counter = 0; // 初始化计数器

            foreach (var settingPair in _currentConfig.Settings)
            {
                string key = settingPair.Key; // 可以使用 Key 如果需要
                Setting setting = settingPair.Value;
                string imagePath = Path.Combine(dc.appDirectory, "img", setting.ImageFilename);
                string toolTip = setting.Tooltip;
                string app_path = setting.Path;
                // 检查文件是否存在，避免加载失败
                if (File.Exists(imagePath))
                {
                    Node item = CreateDockItem(imagePath, app_path, toolTip, counter);
                    dc.dockItems.Add(item);
                    imageContainer.Children.Add(item);
                    counter++; // 增加计数器
                }
            }

            // 根据加载的 Item 数量重新计算窗口宽度
            double totalBaseWidth = dc.dockItems.Count * (dc.ItemBaseSize + dc.Padding1);
            Width = totalBaseWidth + dc.Padding1;
        }
        private void node_mouse_down(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                dc.EventDef.IsLeftClick = true;
                dc.EventDef.X = (int)e.GetPosition(this).X;
                dc.EventDef.Y = (int)e.GetPosition(this).Y;

                // 关键代码，阻止事件冒泡
                e.Handled = true;
            }
        }
        private void node_mouse_leave(object sender, MouseEventArgs e)
        {
            dc.EventDef.Y = 0;
            dc.EventDef.X = 0;
            dc.EventDef.IsLeftClick = false;

            if (Mouse.Captured == this)
                Mouse.Capture(null);
            e.Handled = true;
        }
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        private bool BringWindowToFrontWPF(string windowTitle)
        {
            IntPtr hWnd = FindWindow(null, windowTitle);
            if (hWnd != IntPtr.Zero)
            {
                return SetForegroundWindow(hWnd);
            }
            return false;
        }

        private void node_leftmouse_up(object sender, MouseButtonEventArgs e)
        {
            // 获取当前鼠标相对于窗口的 X 和 Y 坐标
            int currentX = (int)e.GetPosition(this).X;
            int currentY = (int)e.GetPosition(this).Y;

            if (currentX == dc.EventDef.X || currentY == dc.EventDef.Y)
            {

                if (sender is Node clickedNode)
                {
                    string appPath = clickedNode.app_path;
                    string toolTip = clickedNode.ToolTip as string;
                    int id = clickedNode.Id;
                    if (string.IsNullOrEmpty(appPath))
                    {
                        e.Handled = true;
                        return;
                    }
                    dc._oldNodeId = id;

                    if (toolTip == "开始菜单")
                    {

                        //MessageBox.Show("模拟打开开始菜单 (WPF 中需要特定实现)", "提示");
                    }
                    else if (string.IsNullOrEmpty(toolTip))
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(appPath) { UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            //MessageBox.Show($"无法启动应用程序: {ex.Message}", "错误");
                        }
                    }
                    else
                    {
                        bool broughtToFront = BringWindowToFrontWPF(toolTip);
                        if (!broughtToFront)
                        {
                            try
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(appPath) { UseShellExecute = true });
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                        else
                        {
                            try
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(appPath) { UseShellExecute = true });
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }

                    dc.EventDef.IsLeftClick = false;
                    e.Handled = true;
                }
            }
        }

        private void node_mouse_move(object sender, MouseEventArgs e)
        {
            if (dc.EventDef.IsLeftClick)
            {
                // 获取当前鼠标相对于窗口的 X 和 Y 坐标
                int currentX = (int)e.GetPosition(this).X;
                int currentY = (int)e.GetPosition(this).Y;

                if (currentX != dc.EventDef.X || currentY != dc.EventDef.Y)
                {
                    dc.EventDef.X = currentX;
                    dc.EventDef.Y = currentY;

                    if (Mouse.LeftButton == MouseButtonState.Pressed && this.WindowState == WindowState.Normal)
                    {
                        try
                        {
                            this.DragMove();
                            Console.WriteLine($"node_mouse_move - Top: {Top}");
                        }
                        catch (InvalidOperationException)
                        {
                            // 忽略拖动失败或记录日志
                        }
                    }
                }
            }
            else
            {
                Point cursorPos = e.GetPosition(imageContainer);
                double totalWidth = 0;
                foreach (Node item in dc.dockItems)
                {
                    var (transform, _, firstEnter) = dc.itemData[item];
                    var (scaleXAnimation, scaleYAnimation) = dc.animations[item];

                    Point itemCenter = item.TranslatePoint(new Point(item.Width * 0.5, item.Height), imageContainer);
                    Vector diff = Point.Subtract(itemCenter, cursorPos);
                    double distance = diff.Length;
                    double rate = Math.Exp(-distance / dc.DecayFactor);

                    double maxSize = Math.Min(item.Width * 2.0, dc.MaxMagnifiedSize);
                    double targetScale = 1.0 + (maxSize / dc.ItemBaseSize - 1.0) * rate;

                    // 设置动画时长
                    scaleXAnimation.Duration = TimeSpan.FromMilliseconds(firstEnter ? dc.SlowAnimationDuration : dc.NormalAnimationDuration);
                    scaleYAnimation.Duration = TimeSpan.FromMilliseconds(firstEnter ? dc.SlowAnimationDuration : dc.NormalAnimationDuration);

                    scaleXAnimation.To = targetScale;
                    scaleYAnimation.To = targetScale;

                    transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
                    transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);

                    // 标记为已进入
                    dc.itemData[item] = (transform, dc.itemData[item].OriginalSize, false);

                    totalWidth += dc.ItemBaseSize * targetScale + dc.Padding1;
                }

                AnimateWindowWidth(totalWidth);
            }
            e.Handled = true;
        }
        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            Console.WriteLine($"LocationChanged - Top: {Top}");
            if (!dc._isDragging && !dc._isMouseOver)
            {
                if (Top <= dc._snapThresholdPixels)
                {
                    AnimateTop(dc._hiddenTopPosition, 200); // 缩进动画
                }
            }
        }

        private void MainWindow_MouseEnter(object sender, MouseEventArgs e)
        {
            dc._isMouseOver = true;
            AnimateTop(dc._shownTopPosition, 200); // 下拉动画
        }
        private void MainWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            dc._isDragging = false;
            Left = (SystemParameters.WorkArea.Width - Width) / 2; // 水平居中
            Top = dc._shownTopPosition; // 恢复到显示位置
            Console.WriteLine($"MouseUp - Top: {Top}, Left: {Left}");

            // 保存位置
            if (_currentConfig?.Config != null)
            {
                _currentConfig.Config.Left = Left.ToString();
                _currentConfig.Config.Top = Top.ToString();
                //SaveConfig();
            }
        }

        private void PositionCheckTimer_Tick(object sender, EventArgs e)
        {
            if (!dc._isDragging && !dc._isMouseOver)
            {
                if (Top <= dc._snapThresholdPixels)
                {
                    // 确保在非拖动且鼠标不在上方时缩进
                    if (Math.Abs(Top - dc._hiddenTopPosition) > 1) // 避免重复动画
                    {
                        AnimateTop(dc._hiddenTopPosition, 200);
                    }
                }
            }
        }

        private void AnimateTop(double toValue, int duration)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                To = toValue,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(Window.TopProperty, animation);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Handled) return; // 跳过已处理的事件
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
                dc.EventDef.IsLeftClick = true;
                dc.EventDef.X = (int)e.GetPosition(this).X;
                dc.EventDef.Y = (int)e.GetPosition(this).Y;

                // 使用 Dispatcher 延迟获取 Top
                Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                {
                    Console.WriteLine($"OnMouseMove (Delayed) - Top: {Top}");
                }));
            }
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (e.Handled) return; // 跳过已处理的事件
            base.OnMouseLeftButtonDown(e);

            dc.EventDef.IsLeftClick = true;
            dc.EventDef.X = (int)e.GetPosition(this).X;
            dc.EventDef.Y = (int)e.GetPosition(this).Y;
            if (e.Source == this || e.Source == imageContainer)
                Mouse.Capture(this);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                dc._isDragging = true;
            }
        }


        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (e.Handled) return; // 跳过已处理的事件
            base.OnMouseLeave(e);


            dc.EventDef.IsLeftClick = false;
            dc.EventDef.X = 0;
            dc.EventDef.Y = 0;

            foreach (Node item in dc.dockItems)
            {
                var (transform, a, b) = dc.itemData[item];
                var (scaleXAnimation, scaleYAnimation) = dc.animations[item];

                scaleXAnimation.Duration = TimeSpan.FromMilliseconds(dc.NormalAnimationDuration);
                scaleYAnimation.Duration = TimeSpan.FromMilliseconds(dc.NormalAnimationDuration);
                scaleXAnimation.To = 1.0;
                scaleYAnimation.To = 1.0;

                transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);

                // 重置 FirstEnter 标记
                dc.itemData[item] = (transform, dc.itemData[item].OriginalSize, true);
            }

            double originalWidth = dc.imagePaths.Length * (dc.ItemBaseSize + dc.Padding1);
            AnimateWindowWidth(originalWidth);

            dc._isMouseOver = false;
            if (!dc._isDragging)
            {
                //AnimateTop(dc._hiddenTopPosition, 200); // 缩回动画
            }

        }

        private Node CreateDockItem(string imagePath, string app_path, string toolTip, int index)
        {
            var node = new WpfApp1.Node // 使用 Node 类创建实例
            {
                Source = new BitmapImage(new Uri(imagePath)),
                Width = dc.ItemBaseSize,
                Height = dc.ItemBaseSize,
                Margin = new Thickness(dc.Padding1 / 2.0, 0, dc.Padding1 / 2.0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Name = $"node_{index}", // 将索引添加到 Name 属性;
                ToolTip = toolTip,// 设置 ToolTip
                filePath = imagePath, // 设置必需的成员 'filePath'
                app_path = app_path,
                Id = index,
            };

            node.MouseLeave += node_mouse_leave;
            node.MouseMove += node_mouse_move;
            node.MouseDown += node_mouse_down;
            node.MouseLeftButtonUp += node_leftmouse_up;

            // 设置 ToolTip 显示位置
            ToolTipService.SetPlacement(node, PlacementMode.Top);
            ToolTipService.SetVerticalOffset(node, -10); // 根据需要微调
            ToolTipService.SetHorizontalOffset(node, 16);

            var scaleTransform = new ScaleTransform(1.0, 1.0);
            node.LayoutTransform = new TransformGroup { Children = { scaleTransform } };

            dc.itemData[node] = (scaleTransform, new Size(dc.ItemBaseSize, dc.ItemBaseSize), true);
            dc.animations[node] = (
                new DoubleAnimation { FillBehavior = FillBehavior.HoldEnd },
                new DoubleAnimation { FillBehavior = FillBehavior.HoldEnd }
            );

            return node;
        }


        private void AnimateWindowWidth(double targetWidth)
        {
            double currentWidth = ActualWidth > 0 ? ActualWidth : Width;
            if (Math.Abs(targetWidth - currentWidth) <= 1) return;

            Width = targetWidth;
            double deltaWidth = targetWidth - currentWidth;
            Left -= deltaWidth / 2.0;
        }

        private void CreateDefaultConfigFile()
        {
            var defaultConfig = new ConfigData
            {
                Settings = new Dictionary<string, Setting>
            {
                { "defaultItem", new Setting { ImageFilename = "default.png", Path = "default", Tooltip = "Default Item" } }
            },
                Config = new Config { Debug = "false", Web3d = "false", Layout = "left", Nodesize = 50, Translator = "", Shortcut = "", Style = "style-1", Definestart = "false", Left = "100", Top = "100" },
                Exclusion = new Exclusion { Value = "" }
            };

            try
            {
                string jsonString = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dc._configFilePath, jsonString);
                MessageBox.Show($"默认配置文件已创建：{dc._configFilePath}", "提示");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建默认配置文件失败：{ex.Message}", "错误");
            }
        }

        public ConfigData _currentConfig;

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(dc._configFilePath))
                {
                    string jsonString = File.ReadAllText(dc._configFilePath);
                    _currentConfig = JsonSerializer.Deserialize<ConfigData>(jsonString);

                    if (_currentConfig != null)
                    {
                    }
                    else
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

        public void SaveConfig()
        {
            if (_currentConfig == null)
            {
                return;
            }

            try
            {
                string jsonString = JsonSerializer.Serialize(_currentConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dc._configFilePath, jsonString);
            }
            catch (Exception ex)
            {
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (dc._positionCheckTimer != null)
            {
                dc._positionCheckTimer.Stop();
            }
            SaveConfig();
        }
    }
}
