using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineClicker
{
    enum Sprites
    {
        None = -128,            //
        Bomb_red = -4,          //
        Bomb,                   //
        UnsweptFlagged,         //
        UnsweptNormal,          //
        Swept_Empty,            //
        Swept_1,                //
        Swept_2,                //
        Swept_3,                //
        Swept_4,                //
        Swept_5,                //
        Swept_6,                //
        Swept_7,                //
        Swept_8,                //
        SmileNormal,            //
        SmileClick,             //
        SmileClear,             //
        SmileDead,              //
        LED_0,                  //
        LED_1,                  //
        LED_2,                  //
        LED_3,                  //
        LED_4,                  //
        LED_5,                  //
        LED_6,                  //
        LED_7,                  //
        LED_8,                  //
        LED_9,                  //
        BoardCornerTopLeft,     //
        BoardCornerTopRight,    //
        BoardCornerBottomLeft,  //
        BoardCornerBottomRight, //
        LEDPanel,               //
        LEDPanelMask,           //
        LEDPanelBorderTopLeft,  //
    }
    static class TemplatesConstants
    {
        public static Dictionary<Sprites, Rectangle> sprites;

        private static int dfsize = 16;
        static TemplatesConstants()
        {
            sprites = new Dictionary<Sprites, Rectangle>()
            {
                {Sprites.Bomb_red,               r(1,1) },
                {Sprites.Bomb,                   r(0,1) },
                {Sprites.UnsweptFlagged,         r(8,1) },
                {Sprites.UnsweptNormal,          r(9,1) },

                {Sprites.Swept_Empty,            r(0,0) },
                {Sprites.Swept_1,                r(1,0) },
                {Sprites.Swept_2,                r(2,0) },
                {Sprites.Swept_3,                r(3,0) },
                {Sprites.Swept_4,                r(4,0) },
                {Sprites.Swept_5,                r(5,0) },
                {Sprites.Swept_6,                r(6,0) },
                {Sprites.Swept_7,                r(7,0) },
                {Sprites.Swept_8,                r(8,0) },

                {Sprites.SmileNormal,            r(10,0, 26, 26 ) },
                {Sprites.SmileClick,             r(12,0, 26, 26 ) },
                {Sprites.SmileClear,             r(10,2, 26, 26 ) },
                {Sprites.SmileDead,              r(12,2, 26, 26 ) },

                {Sprites.LED_0,                  r(0,2, 13, 23 ) },
                {Sprites.LED_1,                  r(1,2, 13, 23 ) },
                {Sprites.LED_2,                  r(2,2, 13, 23 ) },
                {Sprites.LED_3,                  r(3,2, 13, 23 ) },
                {Sprites.LED_4,                  r(4,2, 13, 23 ) },
                {Sprites.LED_5,                  r(5,2, 13, 23 ) },
                {Sprites.LED_6,                  r(6,2, 13, 23 ) },
                {Sprites.LED_7,                  r(7,2, 13, 23 ) },
                {Sprites.LED_8,                  r(8,2, 13, 23 ) },
                {Sprites.LED_9,                  r(9,2, 13, 23 ) },

                {Sprites.BoardCornerTopLeft,     r(0,4, 12, 12 ) },
                {Sprites.BoardCornerTopRight,    r(1,4, 12, 12 ) },
                {Sprites.BoardCornerBottomLeft,  r(0,5, 12, 12 ) },
                {Sprites.BoardCornerBottomRight, r(1,5, 12, 12 ) },

                {Sprites.LEDPanel,               r(2,4, 48, 31 ) },
                {Sprites.LEDPanelMask,           r(5,4, 48, 31 ) },
                {Sprites.LEDPanelBorderTopLeft,  r(2,4, 4, 4 ) },
            }; // x, y
        }


        private static Rectangle r(int x, int y)
        {
            return new Rectangle(x * dfsize, y * dfsize, dfsize, dfsize);
        }
        private static Rectangle r(int x, int y, int w, int h)
        {
            return new Rectangle(x * dfsize, y * dfsize, w, h);
        }

        public static Rect ToRect(this Rectangle r)
        {
            return new Rect(r.X, r.Y, r.Width, r.Height);
        }
        public static Rectangle ToRectangle(this Rect r)
        {
            return new Rectangle(r.X, r.Y, r.Width, r.Height);
        }
        public static OpenCvSharp.Point ToOpenCvPoint(this System.Drawing.Point r)
        {
            return new OpenCvSharp.Point(r.X, r.Y);
        }
        public static System.Drawing.Point ToSystemPoint(this OpenCvSharp.Point r)
        {
            return new System.Drawing.Point(r.X, r.Y);
        }
        public static OpenCvSharp.Point ToPoint(this OpenCvSharp.Size r)
        {
            return new OpenCvSharp.Point(r.Width, r.Height);
        }
        public static OpenCvSharp.Size ToSize(this OpenCvSharp.Point r)
        {
            return new OpenCvSharp.Size(r.X, r.Y);
        }


        public static OpenCvSharp.Size ToOpenCvSize(this System.Drawing.Size r)
        {
            return new OpenCvSharp.Size(r.Width, r.Height);
        }
        public static System.Drawing.Size ToSystemSize(this OpenCvSharp.Size r)
        {
            return new System.Drawing.Size(r.Width, r.Height);
        }
    }
    
}
