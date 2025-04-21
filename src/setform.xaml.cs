using Microsoft.Win32;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using System.Windows.Shapes;
using WpfApp;
using static System.Net.Mime.MediaTypeNames;
using Path = System.IO.Path;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace WpfApp1
{
    /// <summary>
    /// setform.xaml 的交互逻辑
    /// </summary>
    public partial class setform : Window
    {


        private KeyValuePair<string, Setting> _draggedItem;

        private ConfigData configRef;
        private List<KeyValuePair<string, Setting>> orderedSettings = new();
        private string _draggingKey = null;


        public setform()
        {
            InitializeComponent();

            Height = 0.75 * SystemParameters.PrimaryScreenHeight;
            Width = 0.55 * SystemParameters.PrimaryScreenWidth;
            //  TilePanel.MinHeight = Height - 256;
            //  TilePanel.MinWidth = Width - 96;
            Top = SystemParameters.WorkArea.Height - Height;
            Left = (SystemParameters.WorkArea.Width - Width) / 2;
            //ApplyBackground();


            configRef = ((MainWindow)System.Windows.Application.Current.MainWindow)._currentConfig;
            RefreshList();
        }


        public class SettingEntry
        {
            public string Key { get; set; }
            public SettingValue Value { get; set; }
        }


        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            EnableDropShadow();
        }

        private void EnableDropShadow()
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            int attrValue = 2; // DWMNCRP_ENABLED
            DwmSetWindowAttribute(hwnd, 2, ref attrValue, 4);

            var margins = new MARGINS() { cxLeftWidth = 1, cxRightWidth = 1, cyBottomHeight = 1, cyTopHeight = 1 };
            DwmExtendFrameIntoClientArea(hwnd, ref margins);
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }


        public class SettingValue
        {
            public string imagefilename { get; set; }
            public string path { get; set; }
            public string tooltip { get; set; }
        }

        Dictionary<string, SettingValue> settingsData = new();

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && IsImageFile(files[0]))
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (var file in files)
                {
                    if (IsImageFile(file))
                    {
                        LoadDroppedImage(file);
                    }
                }

                RefreshList(); // 批量导入后刷新列表
            }
            SaveConfigNow();

        }



        private bool IsImageFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg";
        }



        private void LoadDroppedImage(string imagePath)
        {
            string fileName = Path.GetFileName(imagePath);
            string fileNameNoExt = Path.GetFileNameWithoutExtension(imagePath);
            string imgFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img");
            if (!Directory.Exists(imgFolder)) Directory.CreateDirectory(imgFolder);

            string targetPath = Path.Combine(imgFolder, fileName);

            try
            {
                // 检查图像大小，如果超过128x128则缩放
                BitmapImage original = new BitmapImage(new Uri(imagePath));

                if (original.PixelWidth > 128 || original.PixelHeight > 128)
                {
                    TransformedBitmap resized = new TransformedBitmap(original, new ScaleTransform(
                        128.0 / original.PixelWidth,
                        128.0 / original.PixelHeight
                    ));

                    using FileStream fs = new FileStream(targetPath, FileMode.Create);
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(resized));
                    encoder.Save(fs);
                }
                else
                {
                    File.Copy(imagePath, targetPath, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"处理图像失败：{ex.Message}", "错误");
                return;
            }

            // 更新设置项（ImageBox/Tooltip 填一个代表）
            ImageBox.Text = fileName;
            TooltipBox.Text = fileNameNoExt;
            ImagePreview.Source = new BitmapImage(new Uri(targetPath));

            // 添加或更新 settings 数据
            var setting = new Setting
            {
                ImageFilename = fileName,
                //Path = "",
                Path = PathPrefixBox.Text.TrimEnd('/') + "/" + fileNameNoExt,
                Tooltip = fileNameNoExt
            };

            configRef.Settings[fileNameNoExt] = setting;

            UpdatePreviewPanel();

            SaveConfigNow();

        }
        private void OpenImgFolder_Click(object sender, RoutedEventArgs e)
        {
            string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img");
            if (!Directory.Exists(imgPath))
            {
                Directory.CreateDirectory(imgPath);
            }

            System.Diagnostics.Process.Start("explorer.exe", imgPath);
        }

        private void PathPrefixBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "选择应用程序";
            openFileDialog.Filter = "应用程序 (*.exe;*.bat;*.com)|*.exe;*.bat;*.com|所有文件 (*.*)|*.*";
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == true)
            {
                PathPrefixBox.Text = openFileDialog.FileName;
            }
        }
        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "选择图片",
                Filter = "图像文件 (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                string selectedPath = dialog.FileName;
                string imageFileName = Path.GetFileName(selectedPath);
                string imageNameNoExt = Path.GetFileNameWithoutExtension(selectedPath);

                // 填充字段
                ImageBox.Text = imageFileName;
                TooltipBox.Text = imageNameNoExt;

                // 显示预览图像
                ImagePreview.Source = new BitmapImage(new Uri(selectedPath));

                // 拷贝到 img 目录
                string imgDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img");
                if (!Directory.Exists(imgDir))
                    Directory.CreateDirectory(imgDir);

                string destPath = Path.Combine(imgDir, imageFileName);
                try
                {
                    File.Copy(selectedPath, destPath, overwrite: true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"拷贝图片失败: {ex.Message}");
                }
            }
        }


        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var json = File.ReadAllText("config.json");
            var root = JsonSerializer.Deserialize<JsonElement>(json);
            var settings = root.GetProperty("settings");

            settingsData = settings.EnumerateObject().ToDictionary(p => p.Name, p => JsonSerializer.Deserialize<SettingValue>(p.Value.GetRawText()));

            SettingsGrid.ItemsSource = settingsData.Select(kvp => new SettingEntry { Key = kvp.Key, Value = kvp.Value }).ToList();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void RefreshList()
        {
            if (configRef?.Settings != null)
            {
                orderedSettings = configRef.Settings
        .Select(kv => new KeyValuePair<string, Setting>(kv.Key, kv.Value))
        .ToList();

                SettingsGrid.ItemsSource = null;
                SettingsGrid.ItemsSource = orderedSettings;

                UpdatePreviewPanel();
            }
        }

        private void UpdatePreviewPanel()
        {
            PreviewPanel.Children.Clear();
            var dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
            double iconSize = 64 * dpiScale;
            foreach (var kv in orderedSettings)
            {
                string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img", kv.Value.ImageFilename);
                if (!File.Exists(imgPath)) continue;

                var icon = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri(imgPath)),

                    Width = iconSize,
                    Height = iconSize,
                    Margin = new Thickness(5 * dpiScale),
                    Stretch = Stretch.Uniform // 将 Stretch 设置为 Uniform
                };

                var label = new TextBlock
                {
                    Text = kv.Key,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 12,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                var stack = new StackPanel
                {
                    Width = 80,
                    Margin = new Thickness(5),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Cursor = Cursors.Hand,
                    Tag = kv.Key // 将 Key 附加到 StackPanel 上
                };
                stack.Children.Add(icon);
                stack.Children.Add(label);
                stack.MouseLeftButtonUp += PreviewItem_Click;
                stack.AllowDrop = true;
                stack.Drop += PreviewItem_Drop;
                stack.MouseLeftButtonDown += PreviewItem_MouseLeftButtonDown;

                PreviewPanel.Children.Add(stack);
            }
        }

        private void PreviewPanel_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(string)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }
        private void PreviewPanel_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(string)))
            {
                string droppedKey = e.Data.GetData(typeof(string)) as string;
                if (droppedKey == null || droppedKey == _draggingKey)
                    return;

                Point dropPosition = e.GetPosition(PreviewPanel);
                int targetIndex = -1;

                for (int i = 0; i < PreviewPanel.Children.Count; i++)
                {
                    UIElement child = PreviewPanel.Children[i];
                    Rect childBounds = new Rect(child.TranslatePoint(new Point(0, 0), PreviewPanel), child.RenderSize);
                    if (childBounds.Contains(dropPosition))
                    {
                        targetIndex = i;
                        break;
                    }
                }

                if (targetIndex != -1)
                {
                    int originalIndex = orderedSettings.FindIndex(kv => kv.Key == droppedKey);
                    if (originalIndex != -1 && originalIndex != targetIndex)
                    {
                        var itemToMove = orderedSettings[originalIndex];
                        orderedSettings.RemoveAt(originalIndex);
                        orderedSettings.Insert(targetIndex, itemToMove);

                        RefreshGridPreserveSelection(droppedKey);
                        UpdatePreviewPanel();
                        SaveOrder();
                    }
                }
                _draggingKey = null;
                e.Handled = true;
            }
        }
        private DateTime _lastClick = DateTime.MinValue;
        private void PreviewItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is StackPanel panel && panel.Tag is string key)
            {
                var now = DateTime.Now;
                if ((now - _lastClick).TotalMilliseconds < 400)
                {
                    // 双击
                    EditSettingItem(key);
                }
                _lastClick = now;
                _draggingKey = key; // 开始拖动时记录 Key
                DragDrop.DoDragDrop(panel, key, DragDropEffects.Move); // 立即启动拖放
                e.Handled = true; // 阻止事件继续冒泡
            }
        }

        private void SettingsGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (SettingsGrid.SelectedItem is KeyValuePair<string, Setting> selected)
            {
                ImageBox.Text = selected.Value.ImageFilename;
                PathPrefixBox.Text = selected.Value.Path;
                TooltipBox.Text = selected.Value.Tooltip;
            }
        }

        private void SettingsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SettingsGrid.SelectedItem is KeyValuePair<string, Setting> selected)
            {
                string key = selected.Key;
                var result = MessageBox.Show($"确定要删除 \"{key}\" 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // 删除项
                    configRef.Settings.Remove(key);
                    orderedSettings.RemoveAll(kv => kv.Key == key);

                    // 刷新视图
                    RefreshList();
                    SaveConfigNow();
                }
            }
        }


        private void EditSettingItem(string key)
        {
            var item = orderedSettings.FirstOrDefault(kv => kv.Key == key);
            if (item.Equals(default(KeyValuePair<string, Setting>))) return;

            var dialog = new IconEditDialog(item.Value.ImageFilename, item.Value.Path, item.Value.Tooltip)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                // 更新项
                var updated = new Setting
                {
                    ImageFilename = dialog.ImageFilename,
                    Path = dialog.PathValue,
                    Tooltip = dialog.TooltipValue
                };

                // 更新数据
                configRef.Settings[key] = updated;
                int index = orderedSettings.FindIndex(kv => kv.Key == key);
                if (index >= 0)
                    orderedSettings[index] = new KeyValuePair<string, Setting>(key, updated);

                RefreshGridPreserveSelection(key);
                UpdatePreviewPanel();
                SaveConfigNow();
            }
        }

        private void SaveOrder()
        {
            configRef.Settings.Clear();
            foreach (var kv in orderedSettings)
            {
                configRef.Settings[kv.Key] = kv.Value;
            }
            SaveConfigNow();
        }

        private void PreviewItem_Drop(object sender, DragEventArgs e)
        {
            if (sender is StackPanel panel && panel.Tag is string dropTargetKey)
            {
                if (_draggingKey == null || _draggingKey == dropTargetKey)
                    return;

                int fromIndex = orderedSettings.FindIndex(kv => kv.Key == _draggingKey);
                int toIndex = orderedSettings.FindIndex(kv => kv.Key == dropTargetKey);

                if (fromIndex < 0 || toIndex < 0) return;

                var item = orderedSettings[fromIndex];
                orderedSettings.RemoveAt(fromIndex);
                orderedSettings.Insert(toIndex, item);

                RefreshGridPreserveSelection(item.Key);
                UpdatePreviewPanel();
                SaveOrder(); // 更新到 config.Settings 并保存

                _draggingKey = null;
            }
        }


        private void PreviewItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggingKey != null)
            {
                DragDrop.DoDragDrop((DependencyObject)sender, _draggingKey, DragDropEffects.Move);
            }
        }

        private void SaveConfigNow()
        {
            ((MainWindow)System.Windows.Application.Current.MainWindow).SaveConfig();
        }

        private void PreviewItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is StackPanel panel && panel.Tag is string key)
            {
                var item = orderedSettings.FirstOrDefault(kv => kv.Key == key);
                SettingsGrid.SelectedItem = item;

                // 滚动 DataGrid 使其可见
                SettingsGrid.ScrollIntoView(item);
            }
        }

        private void RefreshGridPreserveSelection(string selectedKey)
        {
            SettingsGrid.ItemsSource = null;
            SettingsGrid.ItemsSource = orderedSettings;
            SettingsGrid.SelectedItem = orderedSettings.FirstOrDefault(kv => kv.Key == selectedKey);
        }



        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshList();
        }
        public void SaveTextAsPng(string filePath, string Text)
        {
            double fsize = FontSize;

            FontSize = 48;

            // 字体样式
            FontStyle fontStyle = FontStyles.Normal;
            FontWeight fontWeight = FontWeights.Medium;

            fontWeight = FontWeights.Bold;
            fontStyle = FontStyles.Italic;

            // Fix for CS0119: Replace "MediaTypeNames.Font" with the correct type for Typeface.  
            // The error occurs because "MediaTypeNames.Font" is not a valid type in this context.  
            // The correct namespace for Typeface is System.Windows.Media.  

            FormattedText formattedText = new FormattedText(
               Text,
               CultureInfo.GetCultureInfo("en-us"),
               FlowDirection.LeftToRight,
               new Typeface(new FontFamily("Segoe UI"), fontStyle, fontWeight, FontStretches.Normal), // Corrected line  
               FontSize,
               Brushes.Black, // Not important, used for generating geometry  
               96 // dpi  
            );

            // 获取文字几何图形
            Geometry textGeometry = formattedText.BuildGeometry(new Point(0, 0));

            // 确定图像大小（可以根据 formattedText 宽高来设置）
            int width = (int)Math.Ceiling(formattedText.Width);
            int height = (int)Math.Ceiling(formattedText.Height);

            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                dc.DrawGeometry(Brushes.Black, null, textGeometry); // 用黑色画文字
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                width, height, // 宽高
                96, 96,        // DPI
                PixelFormats.Pbgra32);

            rtb.Render(drawingVisual);

            // 保存为 PNG
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fs);
            }
            FontSize = fsize;
        }

        private void AddOrUpdate_Click(object sender, RoutedEventArgs e)
        {
            string imagePath = ImageBox.Text.Trim();

            // 如果 ImageBox 是空的，就生成图片
            if (string.IsNullOrEmpty(imagePath))
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string imgDir = System.IO.Path.Combine(baseDir, "img");
                Directory.CreateDirectory(imgDir);

                string filename = DateTime.Now.ToString("yyyyMMddHHmmssfff------") + ".png";
                string fullImagePath = System.IO.Path.Combine(imgDir, filename);

                string text = TooltipBox.Text.Trim(); // 或用其他内容代替

                // 调用 DLL 生成图片
                //write_png_with_text(fullImagePath, text, 2);
                SaveTextAsPng(fullImagePath, text);

                imagePath = filename;
                ImageBox.Text = imagePath;
            }

            var setting = new Setting
            {
                ImageFilename = imagePath,
                Path = PathPrefixBox.Text.Trim(),
                Tooltip = TooltipBox.Text.Trim()
            };

            string key = System.IO.Path.GetFileNameWithoutExtension(imagePath);
            configRef.Settings[key] = setting;
            RefreshList();
            SaveConfigNow();

            // 触发配置变化的事件
            SomeMethodInSetForm();
        }


        private void SomeMethodInSetForm()
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.updateIcon(); // Call the public wshow() method
            }
            else
            {
                // Handle the case where the main window is not of type MainWindow
                MessageBox.Show("Error: Could not access the main window.");
            }
        }


        private void SettingsGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SettingsGrid.SelectedItem is KeyValuePair<string, Setting> selected)
            {

                ImageBox.Text = selected.Value.ImageFilename;
                PathPrefixBox.Text = selected.Value.Path;
                TooltipBox.Text = selected.Value.Tooltip;
            }
        }
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            string key = System.IO.Path.GetFileNameWithoutExtension(ImageBox.Text.Trim());
            if (string.IsNullOrEmpty(key)) return;

            if (MessageBox.Show($"确定删除 '{key}' 吗？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (configRef.Settings.ContainsKey(key))
                {
                    configRef.Settings.Remove(key);
                    RefreshList();
                }
                SaveConfigNow();
                SomeMethodInSetForm();
            }


        }
    }
}
