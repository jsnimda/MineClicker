using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MineClicker
{
    class TimeCounter
    {
        string text;
        double startingTime;
        bool doNothing = false;

        public TimeCounter(string text, double startingTime, bool doNothing = false)
        {
            this.text = text;
            this.startingTime = startingTime;
            this.doNothing = doNothing;
        }

        public void Finish()
        {
            if (doNothing) return;
            double spent = GlobalDebugInfo.GetTime() - startingTime;
            GlobalDebugInfo.AppendText($"{text}{Math.Round(spent,3)} ms\n");
        }
    }
    class GlobalDebugInfo
    {
        public static TaskScheduler TaskSc;
        private static Stopwatch sw = new Stopwatch();

        static GlobalDebugInfo()
        {
            sw.Start();
        }

        public static double GetTime()
        {
            return sw.Elapsed.TotalMilliseconds;
        }

        public static RichTextBox RichTextBox;

        public static bool ShowMore = false;

        public static void Clear()
        {
            RichTextBox.Clear();
        }


        public static void AppendText(string s)
        {
            //RichTextBox.Invoke((Action)(()=>
            //{
            //    RichTextBox.AppendText(s);
            //}));
            Task.Factory.StartNew(() =>
            {
                RichTextBox.AppendText(s);
            }, CancellationToken.None, TaskCreationOptions.None,
            TaskSc);
        }

        public static void ShowMessageDetectMinesweeper(Size? s) // null mean not success
        {
            bool success = s.HasValue;
            AppendText(success
                ? "Detected Minesweeper.\n" : "Minesweeper Not Found.\n");
            if (success)
            {
                Size siz = s.Value;
                AppendText($"Game dimensions: {siz.Width}x{siz.Height}.\n");
            }
        }
        

        public static TimeCounter ShowTimeCost(string title)
        {
            return new TimeCounter(title, GetTime());
        }
        public static TimeCounter ShowTimeCostIfShowMore(string title)
        {
            return new TimeCounter(title, GetTime(), !ShowMore);
        }

        public static void ShowMessageIfShowMore(string msg)
        {
            if (!ShowMore) return;
            AppendText(msg);
        }

        public static void ShowScanResult(ScanResult scan)
        {
            AppendText($">> {scan.SmileStatus}, Mines left: {scan.minesLeft}\n");
        }

        public static bool ShowMoreDump = false;





        static Dictionary<int, string> table = new Dictionary<int, string>()
        {
            { -4, "◉" },
            { -3, "▣" },
            { -2, "◬" },
            { -1, "▢" },
            { 0, " " },
            { 1, "1" },
            { 2, "2" },
            { 3, "3" },
            { 4, "4" },
            { 5, "5" },
            { 6, "6" },
            { 7, "7" },
            { 8, "8" },

        };


        public static void ShowInfo(ScanResult scan, SolveResult sol)
        {
            if (!ShowMore) return;

            int[,] mines = scan.mines;
            for (int y = 0; y < mines.GetLength(0); y++)
            {
                for(int x = 0; x < mines.GetLength(1); x++)
                {
                    AppendText(table[mines[y, x]] + "\t");
                }
                AppendText("\n");
            }
        }

    }
}
