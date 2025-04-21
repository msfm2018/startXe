using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Drawing; // 引入 System.Drawing 以使用 Point 和 Size

namespace WpfApp1
{
    public class Node : Image
    {
        public string Key;
        public string app_path;
        public int Id;
        public required string filePath;
        public int OriginalSizeX;
        public int OriginalSizeY;
        public double CenterPointX;
        public double CenterPointY;

    }
}
