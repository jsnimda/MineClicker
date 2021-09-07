using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineClicker
{
    class ShouldStopToken
    {
        public bool ShouldStop = false;
    }
    class Clicker
    {
        public bool Click(SolveResult res, ShouldStopToken t = null)
        {
            GlobalDebugInfo.ShowMessageIfShowMore("\n");


            foreach (Clicks c in res.pointsToBeClick)
            {
                if (t.ShouldStop)
                {
                    return true;
                }

                MouseKeyboardLibrary.MouseSimulator.Position = new Point(c.xy.X, c.xy.Y);
                //System.Threading.Thread.Sleep(10);
                MouseKeyboardLibrary.MouseSimulator.Click(c.button);
                //System.Threading.Thread.Sleep(30);
            }

            if (t.ShouldStop)
            {
                return true;
            }

            if (res.fromScan.SmileStatus == Sprites.SmileClear || res.fromScan.SmileStatus == Sprites.SmileDead)
                System.Threading.Thread.Sleep(100);

            System.Threading.Thread.Sleep(30);

            return res.shouldStop;
        }
    }
}
