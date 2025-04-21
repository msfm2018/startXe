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


namespace WpfApp1
{
    public partial class IconEditDialog : Window
    {
        public string ImageFilename { get; private set; }
        public string PathValue { get; private set; }
        public string TooltipValue { get; private set; }

        public IconEditDialog(string image, string path, string tooltip)
        {
            InitializeComponent();
            ImageBox.Text = image;
            PathBox.Text = path;
            TooltipBox.Text = tooltip;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ImageFilename = ImageBox.Text.Trim();
            PathValue = PathBox.Text.Trim();
            TooltipValue = TooltipBox.Text.Trim();
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
