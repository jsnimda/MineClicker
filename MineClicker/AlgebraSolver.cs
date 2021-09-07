using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineClicker
{
    class Row
    {
        public int[] Coeff; // coef
        public int Sum; // 

        public static List<Row> ToRowList(List<int[]> algebras)
        {
            List<Row> l = new List<Row>();
            foreach (int[] row in algebras)
            {
                Row r = new Row();
                r.Coeff = new int[row.Length - 1];
                Array.Copy(row, 0, r.Coeff, 0, row.Length - 1);
                r.Sum = row[row.Length - 1];
                l.Add(r);
            }
            return l;
        }

        public bool SubtractByAToZeroAndSet(Row A, int coefIndex) // auto set positive // return if modified (if at index is 0 return false
        {
            if (Coeff[coefIndex] == 0) return false; // no need to subtract
            if (A.Coeff[coefIndex] != 1)
            {
                Debugger.Break();
                //Debug.Assert(false);
            }

            int mul = Coeff[coefIndex];

            for (int i = 0; i < Coeff.Length; i++)
            {
                Coeff[i] -= mul * A.Coeff[i];
            }
            Sum -= mul * A.Sum;

            MakeFirstNonZeroPositive();

            return true;
        }

        public void TryToReplaceKnown(int[] result)
        {
            for (int i = 0; i < Coeff.Length; i++)
            {
                if (result[i] >= 0)
                {
                    Sum -= result[i] * Coeff[i];
                    Coeff[i] = 0;
                }
            }
        }

        public bool TryToSolveSingle(int[] result) // assume -1 0 1 only
        {
            // only -1 0 1
            int count;
            int countNeg;
            int sign = Math.Sign(Sum);
            if (Sum != 0) // if that is not 0, than if count*sign == Sum has unique solution
            {
                count = CoefCountMatch(e => e == sign*1);
            }
            else
            { // case Sum = 0
                count = CoefCountMatch(e => e == 1);
                countNeg = CoefCountMatch(e => e == -1);
                if (count != 0 && countNeg != 0) // contain both increase and decrease coef
                {
                    return false;
                }
                for (int i = 0; i < Coeff.Length; i++)
                {
                    if (Coeff[i] != 0)
                    {
                        result[i] = 0;
                    }
                }
                return true;
            }
            if (count * sign == Sum)
            {
                for (int i = 0; i < Coeff.Length; i++)
                {
                    if (Coeff[i] != 0)
                    {
                        if (Math.Sign(Coeff[i]) == sign) // constructive
                        {
                            result[i] = 1;
                        }
                        else
                        {
                            result[i] = 0;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public void MakeFirstNonZeroPositive()
        {
            int i = GetFirstNonZeroIndex();
            if (i == -1) return;
            if (Coeff[i] < 0)
            {
                Revert();
            }
        }
        public void Revert()
        {
            for (int i = 0; i < Coeff.Length; i++)
            {
                Coeff[i] = -Coeff[i];
            }
            Sum = -Sum;
        }

        public bool IsZeroRow()
        {
            return GetFirstNonZeroIndex() == -1;
        }
        public int GetFirstNonZeroIndex()
        {
            return CoefFirstMatchPos(e => e != 0);
        }

        public int CoefFirstMatchPos(Func<int, bool> p)
        {
            for (int i = 0; i < Coeff.Length; i++)
            {
                if (p(Coeff[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public bool CoefAllMatch(Func<int, bool> p) 
        {
            for (int i = 0; i < Coeff.Length; i++)
            {
                if (!p(Coeff[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public int CoefCountMatch(Func<int, bool> p)
        {
            int c = 0;
            for (int i = 0; i < Coeff.Length; i++)
            {
                if (p(Coeff[i]))
                {
                    c++;
                }
            }
            return c;
        }
    }

    class Equations
    {
        public int unknownsThatWeCareOnly;
        public int unknowns;
        public int min;
        public int max;

        public List<Row> rows;
        public Equations(List<int[]> algebras, int min = -1, int max = -1) : this(Row.ToRowList(algebras), min, max)
        {
        }
        public Equations(List<Row> algebras, int min = -1, int max = -1)
        {
            rows = algebras;
            unknowns = algebras[0].Coeff.Length;
            unknownsThatWeCareOnly = unknowns;
            if (min == 0 && max == unknowns)
            {
                this.min = -1;
                this.max = -1;
            } else
            {
                this.min = min;
                this.max = max;
            }
        }

        

        public int[] Solve()
        {
            GlobalDebugInfo.ShowMessageIfShowMore(">> Solving by algebra. Info:\n");
            GlobalDebugInfo.ShowMessageIfShowMore($"unknowns: {unknowns}, equation: {rows.Count}, min: {min}, max: {max}\n");

            GlobalDebugInfo.ShowMessageIfShowMore(">> SolveA (ignore minesLeft)...\n");
            TimeCounter t;
            t = GlobalDebugInfo.ShowTimeCostIfShowMore("... cost \t\t\t\t\t");

            // reduction without considering min and max first...
            int[] result = new int[unknowns]; // arr of -1
            for (int i = 0; i < result.Length; i++) result[i] = -1;
            SolveA(result);
            t.Finish();

            if (!AlgebraSolver.StillUnknown(result))
            {
                GlobalDebugInfo.ShowMessageIfShowMore(">> Success (SolveA). \n");
                GlobalDebugInfo.ShowMessageIfShowMore($"equation: {rows.Count}\n");
                return result;
            }

            GlobalDebugInfo.ShowMessageIfShowMore(">> Fail (SolveA). \n");
            GlobalDebugInfo.ShowMessageIfShowMore($"equation: {rows.Count}\n");

            if (min != -1 && max != -1)
            {
                GlobalDebugInfo.ShowMessageIfShowMore(">> SolveB (with mineLefts)...\n");
                t = GlobalDebugInfo.ShowTimeCostIfShowMore("... cost \t\t\t\t\t");

                t.Finish();
                if (!AlgebraSolver.StillUnknown(result))
                {
                    GlobalDebugInfo.ShowMessageIfShowMore(">> Success (SolveB). \n");
                    return result;
                }

                GlobalDebugInfo.ShowMessageIfShowMore(">> Fail (SolveB). \n");
            } else
            {
                GlobalDebugInfo.ShowMessageIfShowMore("Skipped SolveB. \n");
            }

            GlobalDebugInfo.ShowMessageIfShowMore("We can only guess... \n");
            GlobalDebugInfo.ShowMessageIfShowMore("Calculating probability...\n");
            t = GlobalDebugInfo.ShowTimeCostIfShowMore("... cost \t\t\t\t\t");

            t.Finish();

            return result;
        }

        private void SolveA(int[] result)
        {    // reduction without considering min and max first...
            bool flag = true;
            int RoundCounter = 0;
            while (flag)
            {
                flag = false;

                GlobalDebugInfo.ShowMessageIfShowMore($"   >> r {RoundCounter}\n");

                TimeCounter t = GlobalDebugInfo.ShowTimeCostIfShowMore($"   TryToSolveSingle \t\t");

                flag = true;
                while (flag) // TryToSolveSingle
                {
                    flag = false;
                    List<Row> newRows = new List<Row>();

                    for (int i = 0; i < rows.Count; i++)
                    {
                        Row row = rows[i];
                        row.TryToReplaceKnown(result);

                        if (row.IsZeroRow())
                        {
                            //rows.RemoveAt(i);
                            //i--;
                            flag = true;
                            continue;
                        }

                        if (row.CoefAllMatch(e => e == -1 || e == 0 || e == 1)
                            && row.TryToSolveSingle(result))
                        {
                            //rows.RemoveAt(i);
                            //i--;
                            //TryToAllReplaceKnown(result);
                            flag = true;
                            continue;
                        }

                        newRows.Add(row);
                    }

                    if (flag)
                    {
                        rows = newRows;
                        //TryToAllReplaceKnown(result);
                    }
                }

                t.Finish();
                t = GlobalDebugInfo.ShowTimeCostIfShowMore($"   MakeFirstNonZeroPositive \t\t");

                // sort and eli
                for (int i = 0; i < rows.Count; i++)
                {
                    rows[i].MakeFirstNonZeroPositive();
                }

                t.Finish();
                t = GlobalDebugInfo.ShowTimeCostIfShowMore($"   DoGaussianJordanEliminationOnce \t\t");

                for (int i = 0; i < rows.Count; i++)
                {
                    SortEquationsBelow(i);
                    
                    if (DoGaussianJordanEliminationOnce(i))
                    {
                        flag = true;
                        break;
                    };
                }
                t.Finish();

                RoundCounter++;


            }


            GlobalDebugInfo.ShowMessageIfShowMore($"   >> RoundCounter {RoundCounter} \n");

        }

        private bool DoGaussianJordanEliminationOnce(int at) // return if any change
        {
            Row row = rows[at];
            int coefPos = row.GetFirstNonZeroIndex();
            if (coefPos == -1) return false;
            if (row.Coeff[coefPos] != 1)
            {
                Debugger.Break();
                //Debug.Assert(false);
            }

            bool flag = false;
            for (int i = 0; i < rows.Count; i++)
            {
                if (i == at) continue;
                if (rows[i].SubtractByAToZeroAndSet(row, coefPos))
                {
                    flag = true;
                }
            }

            return flag;
        }


        private void TryToAllReplaceKnown(int[] result)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].TryToReplaceKnown(result);
            }
        }



        private void SortEquationsBelow(int start)
        {
            foreach (Row r in rows) r.MakeFirstNonZeroPositive();
            int length = rows.Count - start;
            rows.Sort(start, length, Comparer<Row>.Create(
                (ra, rb) =>
                {
                    int[] a = ra.Coeff;
                    int[] b = rb.Coeff;
                    for (int i = 0; i < a.Length; i++)
                    {
                        if (a[i] != 0 || b[i] != 0)
                        {
                            return a[i] - b[i];
                        }
                    }
                    return 0;
                    //int[] a = ra.Coeff;
                    //int[] b = rb.Coeff;
                    //for (int i = 0; i < a.Length; i++)
                    //{
                    //    if (a[i] != b[i])
                    //    {
                    //        return b[i] - a[i];
                    //    }
                    //}
                    //return 0;
                }));
        }

        public string tostr()
        {
            string s = "";
            foreach (Row aa in rows)
            {
                s+=($"{string.Join(",\t", aa.Coeff)}\t\t|   {aa.Sum}\n");
            }
            return s;
        }
    }



    //https://quantum-p.livejournal.com/19616.html
    class AlgebraSolver
    {

        public static bool StillUnknown(int[] result) // return true if all is < 0
        {
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] >= 0)
                {
                    return false;
                }
            }
            return true;
        }
        
    }
}
