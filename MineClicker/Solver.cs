using MouseKeyboardLibrary;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineClicker
{
    class Clicks
    {
        public Point xy;
        public MouseButton button;

        public Clicks(Point xy, MouseButton button)
        {
            this.xy = xy;
            this.button = button;
        }
    }
    class SolveResult
    {
        public ScanResult fromScan;
        public List<Clicks> pointsToBeClick = new List<Clicks>(); // in screen xy
        public List<Point> BoardPointsToBeClick = new List<Point>(); // in grid xy
        public List<Point> BoardPointsToBeFlagged = new List<Point>(); // in grid xy
        public bool shouldStop = false;
        public bool isGuess = false;

        public SolveResult(ScanResult fromScan)
        {
            this.fromScan = fromScan;
        }
    }
    class Solver
    {
        public SolveResult Solve(ScanResult scan, SolveResult lastSolve = null, int RoundCounter = -1)
        {
            GlobalDebugInfo.ShowMessageIfShowMore("\n");

            SolveResult res = new SolveResult(scan);

            Point bd = scan.BoardRect.Location;

            if (scan.SmileStatus == Sprites.SmileNormal || scan.SmileStatus == Sprites.SmileClick)
            {
                determineNextClicks(res, scan, lastSolve);
                foreach (Point p in res.BoardPointsToBeFlagged)
                {
                    res.pointsToBeClick.Add(new Clicks(bd + p * 16 + new Point(8, 8), MouseButton.Right));
                }
                foreach (Point p in res.BoardPointsToBeClick)
                {
                    res.pointsToBeClick.Add(new Clicks(bd + p * 16 + new Point(8, 8), MouseButton.Left));
                }
            } else if (RoundCounter == 0)
            {
                // click smile
                res.pointsToBeClick.Add(new Clicks(new Point(
                    scan.SmileLocation.X + TemplatesConstants.sprites[Sprites.SmileNormal].Width / 2
                    ,scan.SmileLocation.Y + TemplatesConstants.sprites[Sprites.SmileNormal].Height / 2
                    ), MouseButton.Left));
            } else
            {
                res.shouldStop = true;
            }

            return res;
        }

        // new numbers from scan result
        List<Point> AliveNumbers = new List<Point>();
        List<Point> DeadNumbers = new List<Point>(); // do not contain zero


        public void Setup()
        {
            AliveNumbers.Clear();
            DeadNumbers.Clear();
        }

        private void determineNextClicks(SolveResult res, ScanResult scan, SolveResult lastSolve)
        {
            solveByAlgebra(res, scan, lastSolve);
        }

        private void solveByAlgebra(SolveResult res, ScanResult scan, SolveResult lastSolve = null)
        {

            TimeCounter t = GlobalDebugInfo.ShowTimeCostIfShowMore("Solve - Setting up algebra equations\t\t");


            List<Point> pts = res.BoardPointsToBeClick;
            List<Point> flags = res.BoardPointsToBeFlagged;
            int w = scan.width;
            int h = scan.height;
            int[,] mines = scan.mines;
            List<Point> pp = new List<Point>(); // all point that is -1
            Dictionary<Point, int> setOfUnknown = new Dictionary<Point, int>();
            HashSet<Point> setOfKnownNearbyUnknown = new HashSet<Point>();

            //if (lastSolve == null || scan.newNumbersRevealed == null)
            //{
            //    AliveNumbers.Clear();
            //    DeadNumbers.Clear();
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (mines[y, x] > 0)
                    {
                        if (addNearby(setOfUnknown, mines, x, y, e => e == -1) >= 1)
                        {
                            setOfKnownNearbyUnknown.Add(new Point(x, y));
                            //AliveNumbers.Add(new Point(x, y));
                        }
                        else
                        {
                            //DeadNumbers.Add(new Point(x, y));
                        }
                    }
                    if (mines[y, x] == -1)
                    {
                        pp.Add(new Point(x, y));
                    }
                }
            }
            if (setOfUnknown.Count == 0)
            {
                //if (pp.Count != 0)
                //    pts.Add(pp[(pp.Count / 2 + h / 2) % pp.Count]);
                //return;
                if (pp.Count != 0)
                    pts.Add(pp[0]);
                return;
            }
            //} else
            //{
            //    foreach (Point p in scan.newNumbersRevealed)
            //    {
            //        AliveNumbers.Add(p);
            //    }
            //    List<Point> AliveNumbersRemain = new List<Point>();
            //    foreach (Point p in AliveNumbers)
            //    {
            //        int x = p.X;
            //        int y = p.Y;
            //        if (addNearby(setOfUnknown, mines, x, y, e => e == -1) >= 1)
            //        {
            //            setOfKnownNearbyUnknown.Add(p);
            //            AliveNumbersRemain.Add(p);
            //        }
            //        else
            //        {
            //            DeadNumbers.Add(p);
            //        }
            //    }
            //    AliveNumbers = AliveNumbersRemain;
            //}
            List<Point> setOfUnknownOrderById = new List<Point>();
            foreach(KeyValuePair<Point, int> item in setOfUnknown)
            {
                //setOfUnknown[item.Key] = setOfUnknownOrderById.Count;
                setOfUnknownOrderById.Add(item.Key);
            }
            for(int i = 0; i < setOfUnknownOrderById.Count; i++)
            {
                setOfUnknown[setOfUnknownOrderById[i]] = i;
            }
            // now they are both referenced each other
            List<int[]> algebras = new List<int[]>(); // last element of int[] is the sum
            int unknowns = setOfUnknownOrderById.Count;
            foreach (Point p in setOfKnownNearbyUnknown)
            {
                int[] alg = new int[unknowns + 1];
                alg[unknowns] = mines[p.Y, p.X] - getNearby(mines, p.X, p.Y, e => e == -2).Count;
                foreach(Point unp in getNearby(mines, p.X, p.Y, e => e == -1))
                {
                    alg[setOfUnknown[unp]]++;
                }
                algebras.Add(alg);
            }

            //
            //
            // !!! Equations setup done!
            //
            //

            t.Finish();

            int a = unknowns;
            int b = pp.Count - a;
            int c = scan.minesLeft;

            int min = c <= b ? 0 : c-b;
            int max = c >= a ? a : c;

            Equations eq = new Equations(algebras, min, max);

            if (GlobalDebugInfo.ShowMoreDump)
            {
                GlobalDebugInfo.ShowInfo(scan, res);

            //int[] eq_res = AlgebraSolver.SolveEquations(Row.ToRowList(algebras), min, max);

                GlobalDebugInfo.AppendText($">> Before >> \n");
                foreach (Row aa in eq.rows)
                {
                    GlobalDebugInfo.AppendText($"{string.Join(",", aa.Coeff)}   |   {aa.Sum}\n");
                }
            }

            int[] eq_res = eq.Solve();

            if (GlobalDebugInfo.ShowMoreDump)
            {
                GlobalDebugInfo.AppendText($"eq_res >> {string.Join(",", eq_res)} \n");
                GlobalDebugInfo.AppendText($"pts >> {string.Join(",", pts)} \n");
                GlobalDebugInfo.AppendText($"flags >> {string.Join(",", flags)} \n");
                GlobalDebugInfo.AppendText($"setOfUnknownOrderById >> {string.Join(",", setOfUnknownOrderById)} \n");
                GlobalDebugInfo.AppendText($"setOfKnownNearbyUnknown >> {string.Join(",", setOfKnownNearbyUnknown)} \n");
                GlobalDebugInfo.AppendText($">> After >> \n");
                foreach (Row aa in eq.rows)
                {
                    GlobalDebugInfo.AppendText($"{string.Join(",", aa.Coeff)}   |   {aa.Sum}\n");
                }
            }


            for (int i = 0; i < eq_res.Length; i++)
            {
                if (eq_res[i] == 0)
                {
                    pts.Add(setOfUnknownOrderById[i]);
                } else if (eq_res[i] == 1)
                {
                    flags.Add(setOfUnknownOrderById[i]);
                }
            }
            
            if (pts.Count == 0)
            {
                res.isGuess = true;
                //breakpoint
                int maxV = int.MinValue; // maxV mean least chance is bomb
                Point pt = new Point(-1, -1);

                for (int i = 0; i < eq_res.Length; i++)
                {
                    if (eq_res[i] > maxV)
                    {
                        maxV = eq_res[i];
                        pt = setOfUnknownOrderById[i];
                    }
                }

                if (pt != new Point(-1, -1))
                {
                    pts.Add(pt);
                }

                //GlobalDebugInfo.ShowInfo(scan, res);
                //GlobalDebugInfo.RichTextBox.AppendText(">> Guess By Chance!! \n");
                //GlobalDebugInfo.RichTextBox.AppendText($"eq_res >> {string.Join(",", eq_res)} \n");
                //GlobalDebugInfo.RichTextBox.AppendText($"pts >> {string.Join(",", pts)} \n");
                //GlobalDebugInfo.RichTextBox.AppendText($"flags >> {string.Join(",", flags)} \n");
                //GlobalDebugInfo.RichTextBox.AppendText($"setOfUnknownOrderById >> {string.Join(",", setOfUnknownOrderById)} \n");
                //GlobalDebugInfo.RichTextBox.AppendText($"setOfKnownNearbyUnknown >> {string.Join(",", setOfKnownNearbyUnknown)} \n");
                //GlobalDebugInfo.RichTextBox.AppendText($">> Before >> \n");
                //foreach (int[] a in algebras_before)
                //{
                //    GlobalDebugInfo.RichTextBox.AppendText($"{string.Join(",", a)} \n");
                //}
                //GlobalDebugInfo.RichTextBox.AppendText($">> After >> \n");
                //foreach (int[] a in algebras)
                //{
                //    GlobalDebugInfo.RichTextBox.AppendText($"{string.Join(",", a)} \n");
                //}

            }


            GlobalDebugInfo.ShowMessageIfShowMore($">> Flag: {flags.Count}, Click: {pts.Count}\n");

        }

        
        
    
        private List<Point> getNearby(int[,] mines, int x, int y, Func<int, bool> p)
        {
            List<Point> list = new List<Point>();
            int w = mines.GetLength(1);
            int h = mines.GetLength(0);
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    int x2 = x + i;
                    int y2 = y + j;
                    if (x2 >= 0 && x2 < w && y2 >= 0 && y2 < h
                        && !(x2 == x && y2 == y) && p(mines[y2, x2]))
                    {
                        list.Add(new Point(x2, y2));
                    }
                }
            }
            return list;
        }

        private int addNearby(Dictionary<Point, int> set, int[,] mines, int x, int y, Func<int, bool> p)
        {
            int count = 0;
            int w = mines.GetLength(1);
            int h = mines.GetLength(0);
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    int x2 = x + i;
                    int y2 = y + j;
                    if (x2 >= 0 && x2 < w && y2 >= 0 && y2 < h
                        && !(x2 == x && y2 == y) && p(mines[y2, x2]))
                    {
                        count++;
                        if(!set.ContainsKey(new Point(x2, y2)))
                            set.Add(new Point(x2, y2), -1);
                    }
                }
            }
            return count;
        }
        

    }
}
