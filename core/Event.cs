using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{

    public struct TMouseEvent
    {
        public bool IsLeftClick;
        public int Y;
        public int X;

        public TMouseEvent(bool isLeftClick, int x, int y)
        {
            IsLeftClick = isLeftClick;
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return $"IsLeftClick: {IsLeftClick}, X: {X}, Y: {Y}";
        }
    }
}
