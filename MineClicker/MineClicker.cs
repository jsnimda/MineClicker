using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OpenCvSharp.Extensions.BitmapConverter;
using Size = OpenCvSharp.Size;

namespace MineClicker
{
    class MineClicker
    {
        //
        // Clicker,  scan --> solve --> click
        //
        //!! init

        Bitmap templates = Properties.Resources.templates;
        MatOfByte3 tMat;
        Dictionary<Sprites, MatOfByte3> sprImgs = new Dictionary<Sprites, MatOfByte3>();

        Scanner scanner;
        Solver solver = new Solver();
        Clicker clicker = new Clicker();

        public MineClicker()
        {
            initClicker();
        }

        private void initClicker()
        {
            tMat = new MatOfByte3(templates.ToMat());
            foreach (KeyValuePair<Sprites, Rectangle> item in TemplatesConstants.sprites)
            {
                sprImgs.Add(item.Key, new MatOfByte3(tMat, item.Value.ToRect()));
            }
            scanner = new Scanner(sprImgs);
        }
        //!! init end

        //!! setup and one step

        ShouldStopToken shouldStopToken = new ShouldStopToken();

        int RoundCounter = 0;
        int GuessCounter = 0;

        //ScanResult lastScan = null;
        SolveResult lastSolve = null; // SolveResult contains ScanResult

        public Size? Setup() // return if minesweeper detected
        {
            var t = GlobalDebugInfo.ShowTimeCost("Time Spent - Setup:\t\t\t\t");
            shouldStopToken.ShouldStop = false;
            RoundCounter = 0;
            GuessCounter = 0;
            lastSolve = null;

            Size? res = scanner.Setup();
            solver.Setup();

            t.Finish();
            GlobalDebugInfo.AppendText("\n");
            return res;
        }
        public bool OneStep() // return should stop
        {
            shouldStopToken.ShouldStop = false;


            GlobalDebugInfo.AppendText("=========\n");
            GlobalDebugInfo.AppendText($">> Round {RoundCounter} (Guess Count {GuessCounter})\n\n");

            TimeCounter t;
            t = GlobalDebugInfo.ShowTimeCost("Total Time Spent: Scanning \t\t=>\t");
            ScanResult scan = scanner.Scan(lastSolve, RoundCounter);
            t.Finish();

            if (shouldStopToken.ShouldStop)
            {
                GlobalDebugInfo.AppendText("\n");
                GlobalDebugInfo.ShowScanResult(scan);
                return true;
            }

            t = GlobalDebugInfo.ShowTimeCost("Total Time Spent: Solving \t\t=>\t");
            SolveResult solve = solver.Solve(scan, lastSolve, RoundCounter);
            t.Finish();

            if (shouldStopToken.ShouldStop)
            {
                GlobalDebugInfo.AppendText("\n");
                GlobalDebugInfo.ShowScanResult(scan);
                return true;
            }

            t = GlobalDebugInfo.ShowTimeCost("Total Time Spent: Clicking \t\t=>\t");
            bool res = clicker.Click(solve, shouldStopToken);
            t.Finish();

            lastSolve = solve;
            //if (scan.SmileStatus == Sprites.SmileNormal || scan.SmileStatus == Sprites.SmileClick)
            //{
            //    lastSolve = solve;
            //} else
            //{
            //    lastSolve = null;
            //}

            RoundCounter++;
            if (solve.isGuess)
            {
                GuessCounter++;
            }

            GlobalDebugInfo.AppendText("\n");
            GlobalDebugInfo.ShowScanResult(scan);

            return res;
        }
        public void ForceStop()
        {
            shouldStopToken.ShouldStop = true;
        }


    }
}
