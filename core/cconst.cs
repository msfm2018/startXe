using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;

namespace WpfApp1
{

    public struct DockController
    {
        public double _snapThresholdPixels;
        public double _hiddenTopPosition;
        public double _shownTopPosition = 0;
        public bool _isDragging = false;
        public DispatcherTimer _positionCheckTimer;
        public bool _isMouseOver = false;


        public int _oldNodeId = -1;

        public string _configFilePath;
        public TMouseEvent EventDef;
        public double MouseMoveThreshold = 2.0;
        public double ItemBaseSize = 50;
        public double MaxMagnifiedSize = 128.0;

        public int ExtraToolTipSpace = 10;

        public int Padding1 = 10;
        public double DecayFactor = 63.82 * 5;
        public int DesiredMargin = 20;
        public int SlowAnimationDuration = 300; // 缓慢动画时长（毫秒）
        public int NormalAnimationDuration = 100; // 正常动画时长（毫秒）

        public List<Node> dockItems = new();
        public Dictionary<Node, (ScaleTransform Transform, Size OriginalSize, bool FirstEnter)> itemData = new(); // 添加 FirstEnter
        public Dictionary<Node, (DoubleAnimation ScaleX, DoubleAnimation ScaleY)> animations = new(); // 修改动画字典

        public string appDirectory;
        public string[] imagePaths;

        public DockController()
        {
        }
    }

}
