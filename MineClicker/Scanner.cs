using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static OpenCvSharp.Extensions.BitmapConverter;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace MineClicker
{

    class ScanResult
    {
        public Sprites SmileStatus;
        public Point SmileLocation;
        public Rect BoardRect;
        public int width {
            get {
                return mines.GetLength(1);
            }
        }
        public int height {
            get {
                return mines.GetLength(0);
            }
        }

        public int[,] mines; // row, col ==>  h, w
        public int minesLeft;

        public List<Point> newNumbersRevealed = null; // only not null when last result avalible
                                                     // only non zero should be saved

        public ScanResult() { }
        public ScanResult(int[,] mines)
        {
            this.mines = mines;
        }
    }

    class Scanner
    {
        private static List<Sprites> gridItems = new List<Sprites>()
        {
            Sprites.UnsweptNormal,
            Sprites.UnsweptFlagged,
            Sprites.Bomb,
            Sprites.Bomb_red,
            Sprites.Swept_Empty,
            Sprites.Swept_1,
            Sprites.Swept_2,
            Sprites.Swept_3,
            Sprites.Swept_4,
            Sprites.Swept_5,
            Sprites.Swept_6,
            Sprites.Swept_7,
            Sprites.Swept_8,
        };
        private static List<Sprites> smiles = new List<Sprites>()
        {
            Sprites.SmileClear,
            Sprites.SmileDead,
            Sprites.SmileNormal,
            Sprites.SmileClick,
        };
        private static List<Sprites> LEDs = new List<Sprites>()
        {
            Sprites.LED_0,
            Sprites.LED_1,
            Sprites.LED_2,
            Sprites.LED_3,
            Sprites.LED_4,
            Sprites.LED_5,
            Sprites.LED_6,
            Sprites.LED_7,
            Sprites.LED_8,
            Sprites.LED_9,
        };

        private Dictionary<Sprites, MatOfByte3> sprImgs;
        private List<Mat> gridItemMats = new List<Mat>();
        private List<Mat> smileMats = new List<Mat>();
        private List<Mat> LEDMats = new List<Mat>();
        
        public Scanner(Dictionary<Sprites, MatOfByte3> sprImgs)
        {
            this.sprImgs = sprImgs;
            toListMat(gridItems, ref gridItemMats);
            toListMat(smiles, ref smileMats);
            toListMat(LEDs, ref LEDMats);

        }
        private void toListMat(List<Sprites> a, ref List<Mat> b)
        {
            foreach (Sprites s in a)
            {
                b.Add(sprImgs[s]);
            }
        }

        private Rect exeTopInfoRect;
        private Rect boardRect;
        private int w;
        private int h;

        public ScanResult Scan(SolveResult lastSolve = null, int RoundCounter = -1)
        {
            int[,] mines;
            ScanResult lastscan = null;
            if (lastSolve == null) {
                mines = new int[h, w];
            } else
            {
                lastscan = lastSolve.fromScan;
                mines = (int[,])lastscan.mines.Clone();
            }
            ScanResult res = new ScanResult(mines);
            res.BoardRect = boardRect;

            using (Bitmap exeImg = printScreen(exeTopInfoRect))
            using (Bitmap boardImg = printScreen(boardRect + new Size(16, 16)))
            using (MatOfByte3 exeMat = new MatOfByte3(exeImg.ToMat()))
            using (MatOfByte3 BoardMat = new MatOfByte3(boardImg.ToMat()))
            {
                TimeCounter t;
                t = GlobalDebugInfo.ShowTimeCostIfShowMore("Time Spent - ScanSmile:\t\t\t\t");
                ScanSmile(res, exeMat);
                t.Finish();

                t = GlobalDebugInfo.ShowTimeCostIfShowMore("Time Spent - ScanMineStat:\t\t\t");
                ScanMineStat(res, exeMat);
                t.Finish();

                if (res.SmileStatus == Sprites.SmileClear ||
                    res.SmileStatus == Sprites.SmileDead)
                {
                    return res;
                }

                if (lastSolve == null)
                {
                    t = GlobalDebugInfo.ShowTimeCostIfShowMore("Time Spent - ScanBoardFromZero:\t\t\t");
                    ScanBoardFromZero(res, BoardMat);
                } else
                {
                    // check if last solve is click restart
                    if (lastSolve.fromScan.SmileStatus == Sprites.SmileClear ||
                        lastSolve.fromScan.SmileStatus == Sprites.SmileDead)
                    {
                        EmptyMines(res.mines);
                    } else
                    {
                        t = GlobalDebugInfo.ShowTimeCostIfShowMore("Time Spent - ScanBoardFromLastResult:\t\t");
                        res.newNumbersRevealed = new List<Point>();
                        ScanBoardFromLastResult(res, BoardMat, lastscan, lastSolve);
                    }
                }
                t.Finish();
            }
            return res;
        }

        private void EmptyMines(int[,] mines)
        {
            for(int i = 0; i < mines.GetLength(0); i++)
            {
                for(int j = 0; j < mines.GetLength(1); j++)
                {
                    mines[i, j] = -1;
                }
            }
        }

        //DONE 
        private void ScanMineStat(ScanResult res, MatOfByte3 exeMat)
        {
            MatOfByte3 leftHalf = new MatOfByte3(exeMat, new Rect(0, 0, 
                exeMat.Width / 2, exeMat.Height));
            Point LEDPanel = match(exeMat, sprImgs[Sprites.LEDPanel]
                , sprImgs[Sprites.LEDPanelMask]);
            Size panelBorderSize = TemplatesConstants.sprites[Sprites.LEDPanelBorderTopLeft]
                .ToRect().Size;
            Point LED = LEDPanel + panelBorderSize.ToPoint();
            // LED is the top left 
            MatOfByte3 LEDMat = new MatOfByte3(exeMat, new Rect(LED, 
                new Size(
                    TemplatesConstants.sprites[Sprites.LED_0].Width * 3,
                    TemplatesConstants.sprites[Sprites.LED_0].Height
                    )));

            int[,] gMres = closestGridMatches(LEDMat, LEDMats, TemplatesConstants.sprites[Sprites.LED_0].Size.ToOpenCvSize());

            int total = 0;
            for (int c = 0; c < 3; c++)
            {
                total = total * 10 + gMres[0, c];
            }
            res.minesLeft = total;
        }

        //DONE, TODO assert
        private void ScanSmile(ScanResult res, MatOfByte3 exeMat)
        {
            double maxVal;
            Point p;
            res.SmileStatus = smiles[closestMatch(exeMat, smileMats, out p, out maxVal)];
            res.SmileLocation = p;

            if (maxVal < 0.8)
            {
                Debugger.Break();
                //Debug.Assert(false);
                //res.SmileStatus = Sprites.SmileNormal;
                res.SmileStatus = Sprites.SmileDead;
            }
            res.SmileLocation += exeTopInfoRect.Location;
        }
        //private void ScanBoardFromZero(ScanResult res, MatOfByte3 boardMat)
        //{
        //    int[,] indexes = closestGridMatches(boardMat, gridItemMats, new Size(16, 16));

        //    for (int r = 0; r < res.height; r++)
        //    {
        //        for (int c = 0; c < res.width; c++)
        //        {
        //            res.mines[r,c] = (int)gridItems[indexes[r,c]];
        //        }
        //    }
        //}
        private void ScanBoardFromZero(ScanResult res, MatOfByte3 boardMat)
        {
            MatOfFloat[] scanresults = new MatOfFloat[gridItems.Count];
            for(int i = 0; i < gridItems.Count; i++)
            {
                scanresults[i] = new MatOfFloat();
                Cv2.MatchTemplate(boardMat, gridItemMats[i], scanresults[i], TemplateMatchModes.CCoeffNormed);
            }

            int w = res.width;
            int h = res.height;

            for(int r = 0; r < h; r++)
            {
                for(int c = 0; c < w; c++)
                {
                    float maxV = float.NegativeInfinity;
                    for (int k = 0; k < gridItems.Count; k++)
                    {
                        int x = 16 * c;
                        int y = 16 * r;

                        float v = scanresults[k].At<float>(y, x);
                        if (v > maxV)
                        {
                            maxV = v;
                            res.mines[r, c] = (int)gridItems[k];
                        }

                    }
                }
            }


            foreach (MatOfFloat m in scanresults) m.Dispose();
        }

        //DONE 
        private void ScanBoardFromLastResult(ScanResult res, MatOfByte3 boardMat, 
            ScanResult lastscan, SolveResult lastSolve) // save some scanning time
        {
            foreach(Point p in lastSolve.BoardPointsToBeFlagged)
            {
                res.mines[p.Y, p.X] = (int) Sprites.UnsweptFlagged;
            }

            Stack<Point> pointsToBeScan = new Stack<Point>();
            foreach (Point p in lastSolve.BoardPointsToBeClick)
            {
                pointsToBeScan.Push(p);
            }
            //pointsToBeScan.AddRange(lastSolve.BoardPointsToBeClick);
            //pointsToBeScan.AddRange(lastSolve.BoardPointsToBeFlagged);

            while(pointsToBeScan.Count > 0)
            {
                Point p = pointsToBeScan.Pop();

                int num = (int)gridItems[closestGridMatch(boardMat, gridItemMats, new Size(16, 16), p)];
                res.mines[p.Y, p.X] = num;

                if (num == 0)
                {
                    add8If(pointsToBeScan, res.mines, p, e => e == -1);
                } else if(num > 0)
                {
                    res.newNumbersRevealed.Add(new Point(p.X, p.Y));
                }
            }
        }
        private void add8If(Stack<Point> pointsToBeScan, int[,] mines, Point at, Func<int, bool> f)
        {
            int w = mines.GetLength(1);
            int h = mines.GetLength(0);
            for (int ii = -1; ii <= 1; ii++)
            {
                for (int jj = -1; jj <= 1; jj++)
                {
                    int x2 = at.X + ii;
                    int y2 = at.Y + jj;
                    if (x2 >= 0 && x2 < w && y2 >= 0 && y2 < h
                        && !(ii == 0 && jj == 0) && f(mines[y2, x2]))
                    {
                        pointsToBeScan.Push(new Point(x2, y2));
                    }
                }
            }

        }

        //DONE
        public Size? Setup() // find minesweeper exe location
        {
            MatOfByte3 t1 = sprImgs[Sprites.BoardCornerTopLeft];
            MatOfByte3 b2 = sprImgs[Sprites.BoardCornerBottomRight];
            //MatOfByte3 t2 = sprImgs[Sprites.BoardCornerTopRight];
            //MatOfByte3 b1 = sprImgs[Sprites.BoardCornerBottomLeft];

            using (Bitmap screen = printScreen())
            using (MatOfByte3 src = new MatOfByte3(screen.ToMat()))
            {
                Point ptT1 = match(src, t1);
                Point ptB2 = match(src, b2);
                ptT1 += t1.Size().ToPoint();
                boardRect = new Rect(ptT1.X, ptT1.Y, ptB2.X - ptT1.X, ptB2.Y - ptT1.Y);
            }

            exeTopInfoRect = new Rect(boardRect.X, boardRect.Y - 64,
                boardRect.Width, 64);
            
            if (boardRect.Width > 0 && boardRect.Height > 0 &&
                boardRect.Width % 16 == 0 && boardRect.Height % 16 == 0)
            {
                w = boardRect.Width / 16;
                h = boardRect.Height / 16;
                return new Size(w, h);
            } else
            {
                return null;
            }

        }


        //DONE
        private int[,] closestGridMatches(Mat img, List<Mat> templates, Size gridSize) // return int[height, width]
        {
            int h = img.Size().Height / gridSize.Height;
            int w = img.Size().Width / gridSize.Width;
            int[,] res = new int[h, w];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    res[y, x] = closestGridMatch(img, templates, gridSize, new Point(x, y));
                }
            }
            return res;
        }
        //DONE
        private int closestGridMatch(Mat img, List<Mat> templates, Size gridSize, Point at)
        {
            Rect roi = new Rect(new Point(at.X*gridSize.Width, at.Y*gridSize.Height), gridSize);
            
            double maxVal = double.NegativeInfinity;
            int corrI = -1;
            using (Mat m = new Mat(img, roi))
            {
                for (int i = 0; i < templates.Count(); i++)
                {
                    double v;

                    using (Mat res = new Mat())
                    {
                        Cv2.MatchTemplate(m, templates[i], res, TemplateMatchModes.CCoeffNormed);

                        v = res.At<float>(0, 0);
                    }

                    if (v > maxVal)
                    {
                        maxVal = v;
                        corrI = i;
                    }
                }
            }
            return corrI;
        }
        //DONE
        private int closestMatch(InputArray img, List<Mat> templates, out Point pt, out double maxVal) // return index of template list
        {
            maxVal = double.NegativeInfinity;
            int corrI = -1;
            Point corrPt = new Point(-1, -1);
            for (int i = 0; i < templates.Count(); i++)
            {
                double v;
                Point p = match(img, templates[i], out v);
                if (v > maxVal)
                {
                    maxVal = v;
                    corrI = i;
                    corrPt = p;
                }
            }
            pt = corrPt;
            return corrI;
        }

        //DONE
        private Point match(InputArray img, InputArray template, InputArray mask = null)
        {
            return match(img, template, out double _, mask);
        }
        //DONE
        private Point match(InputArray img, InputArray template, out double maxVal, InputArray mask = null)
        {
            if (mask == null)
            {
                using (Mat res = new Mat())
                {
                    Cv2.MatchTemplate(img, template, res, TemplateMatchModes.CCoeffNormed);
                    double minVal = 0;
                    maxVal = 0;
                    Point minLoc = new Point(), maxLoc = new Point();
                    Cv2.MinMaxLoc(res, out minVal, out maxVal, out minLoc, out maxLoc);
                    return maxLoc;
                }
            } else
            {
                using (Mat res = new Mat())
                {
                    Cv2.MatchTemplate(img, template, res, TemplateMatchModes.CCorrNormed, mask);
                    double minVal = 0;
                    maxVal = 0;
                    Point minLoc = new Point(), maxLoc = new Point();
                    Cv2.MinMaxLoc(res, out minVal, out maxVal, out minLoc, out maxLoc);
                    return maxLoc;
                }
            }
        }


        //DONE
        private Bitmap printScreen(Rect r = new Rect())
        {
            if (r == new Rect())
            {
                r = new Rect(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            }
            Bitmap printscreen = new Bitmap(r.Width, r.Height);

            Graphics graphics = Graphics.FromImage(printscreen as Image);

            graphics.CopyFromScreen(r.X, r.Y, 0, 0, printscreen.Size);

            return printscreen;
        }

    }
}
