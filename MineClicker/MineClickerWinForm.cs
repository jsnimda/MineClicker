using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MouseKeyboardLibrary;
using System.Threading;
using OpenCvSharp;
using static OpenCvSharp.Extensions.BitmapConverter;
using Size = OpenCvSharp.Size;

namespace MineClicker
{
    public partial class MineClickerWinForm : Form
    {
        public MineClickerWinForm()
        {
            InitializeComponent();
        }

        MineClicker mineclicker;
        private void Form1_Load(object sender, EventArgs e)
        {
            GlobalDebugInfo.RichTextBox = richTextBox1;
            GlobalDebugInfo.TaskSc = 
                TaskScheduler.FromCurrentSynchronizationContext();

            mineclicker = new MineClicker();

            keySetup();

        }

        #region //!! key init

        KeyboardHook kh = new KeyboardHook();

        private void keySetup()
        {
            kh.KeyDown += Kh_KeyDown;

            kh.Start();
        }

        private void Kh_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Oem3)
            {
                ClickerStop();
            }
            if (e.KeyCode == Keys.F3)
            {
                ClickerStart();
            }
        }

        #endregion //!! key init end
        //!! mineclicker

        enum MineClickerStatus
        {
            Running,
            Stopping,
            Stopped,
            Awaiting  // when called step by step
        }

        MineClickerStatus status = MineClickerStatus.Stopped;
        private void updateLabelClickStatus()
        {
            switch (status)
            {
                case MineClickerStatus.Running:
                    labelClickerStatus.Text = "Running";
                    labelClickerStatus.BackColor = Color.Lime;
                    break;
                case MineClickerStatus.Stopped:
                    labelClickerStatus.Text = "Stop";
                    labelClickerStatus.BackColor = Color.FromArgb(255, 255, 192);
                    break;
                case MineClickerStatus.Stopping:
                    labelClickerStatus.Text = "Stopping";
                    labelClickerStatus.BackColor = Color.Yellow;
                    break;
                case MineClickerStatus.Awaiting:
                    labelClickerStatus.Text = "Awaiting";
                    labelClickerStatus.BackColor = Color.LightBlue;
                    break;
            }
        }

        private Task<bool> Setup()
        {
            status = MineClickerStatus.Running;
            updateLabelClickStatus();

            GlobalDebugInfo.Clear();
            return Task.Factory.StartNew(() =>
            {
                return mineclicker.Setup();
            }).ContinueWith((e) =>
            {
                bool res = e.Result.HasValue;
                GlobalDebugInfo.ShowMessageDetectMinesweeper(e.Result);

                status = MineClickerStatus.Awaiting;
                updateLabelClickStatus();

                return res;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private Task<bool> OneStep()
        {
            status = MineClickerStatus.Running;
            updateLabelClickStatus();

            return Task.Factory.StartNew(() =>
            {
                return mineclicker.OneStep();
            }
            ).ContinueWith((e) =>
            {
                bool res = e.Result;

                if (status == MineClickerStatus.Stopping)
                {
                    status = MineClickerStatus.Stopped;
                } else
                {
                    status = MineClickerStatus.Awaiting;
                }

                updateLabelClickStatus();

                return res;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }


        private void ClickerStop()
        {
            if (status != MineClickerStatus.Running && status != MineClickerStatus.Awaiting)
            {
                return;
            }
            if (status == MineClickerStatus.Awaiting)
            {
                status = MineClickerStatus.Stopped;
            }
            else
            {
                status = MineClickerStatus.Stopping;
            }
            updateLabelClickStatus();

            timer1.Stop();
            mineclicker.ForceStop();

            MouseSimulator.Position = buttonStart.PointToScreen(System.Drawing.Point.Empty)
                    + new System.Drawing.Size(buttonStart.Width / 2, buttonStart.Height / 2);
        }
        private void ClickerStart()
        {
            if (status != MineClickerStatus.Stopped)
            {
                return;
            }

            Setup().ContinueWith((e) =>{
                bool res = e.Result;
                if (res)
                {
                    timer1.Start();
                }
                else
                {
                    ClickerStop();
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            buttonSetup.Visible = buttonOneStep.Visible = checkBox1.Checked;
            GlobalDebugInfo.ShowMore = checkBox1.Checked;
        }


        private void buttonOneStep_Click(object sender, EventArgs e)
        {
            OneStep();
        }

        private void buttonSetup_Click(object sender, EventArgs e)
        {
            Setup();
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            ClickerStart();
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (status == MineClickerStatus.Awaiting)
            {
                OneStep().ContinueWith((f)=>
                {
                    bool res = f.Result;
                    if (res) // should stop
                    {
                        ClickerStop();
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            //if (running) return;
            //running = true;
            //richTextBox1.AppendText($"==============\n");

            //var scanres = scanner.Scan();
            //var solveres = solver.Solve(scanres);
            //bool res = clicker.Click(solveres);

            //if (!res)
            //{
            //    ClickerStop();

            //    MouseSimulator.Position = buttonStart.PointToScreen(System.Drawing.Point.Empty) 
            //        + new System.Drawing.Size(buttonStart.Width / 2, buttonStart.Height / 2);
            //}

            //running = false;

            ////richTextBox1.AppendText($"{scanres.SmileStatus}\n");
            ////var str = string.Join(",", scanres.mines.OfType<int>()
            ////    .Select((value, index) => new { value, index })
            ////    .GroupBy(x => x.index / scanres.mines.GetLength(1))
            ////    .Select(x => $"{{{string.Join(",", x.Select(y => y.value))}}}"));
            //richTextBox1.AppendText($"{scanres.minesLeft} Mines Left. \n");
            //richTextBox1.AppendText($"{scanres.SmileStatus}\n");
        }

        private void buttonToggleTextbox_Click(object sender, EventArgs e)
        {

            richTextBox1.Dock = richTextBox1.Dock == DockStyle.Fill ? DockStyle.None : DockStyle.Fill;

            if (richTextBox1.Dock == DockStyle.None)
            {
                richTextBox1.Anchor = (AnchorStyles)15;
                richTextBox1.Size = new System.Drawing.Size(ClientSize.Width - 30,
                    ClientSize.Height - richTextBox1.Location.Y - 10);
            }
        }

        private void checkBoxAlwaysOnTop_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = checkBoxAlwaysOnTop.Checked;
        }
    }
}
