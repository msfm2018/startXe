using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    public partial class LaunchSimulator : Window
    {
        public LaunchSimulator(string key, Setting setting)
        {
            InitializeComponent();

            TitleText.Text = $"启动模拟：{key}";
            PathText.Text = $"路径：{setting.Path}";
            TooltipText.Text = $"说明：{setting.Tooltip}";

            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img", setting.ImageFilename);
            if (File.Exists(iconPath))
            {
                IconImage.Source = new BitmapImage(new Uri(iconPath));
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
