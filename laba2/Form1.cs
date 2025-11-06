using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace laba2
{
    public partial class Form1 : Form
    {
    
        private List<IFunction> functions = new List<IFunction>();
        private IFunction currentFunction;
        private Color graphColor = Color.Red;
        private BackgroundStyle bgStyle = new BackgroundStyle();
        private readonly float basePixelsPerUnit = 50f;
        private float scale = 1f;
        private PointF originPx = new PointF();
        private bool dragging = false;
        private Point lastMouse;

        public Form1()
        {
            InitializeComponent();

            InitializeFunctions();
            SetupDrawPanelBuffering();
            InitFunctionsList();
            ResetView();
        }

        private void InitializeFunctions()
        {
            functions.Add(new SinFunction());
            functions.Add(new SquareFunction());
            functions.Add(new TanFunction());
            functions.Add(new CubeFunction());
            functions.Add(new LinearFunction());
            currentFunction = functions.First();
        }

        private void SetupDrawPanelBuffering()
        {
            var dp = this.Controls.Find("drawPanel", true).FirstOrDefault() as Panel;
            if (dp != null)
            {
                var pi = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
                pi?.SetValue(dp, true, null);

                dp.Paint += DrawPanel_Paint;
                dp.MouseDown += DrawPanel_MouseDown;
                dp.MouseMove += DrawPanel_MouseMove;
                dp.MouseUp += DrawPanel_MouseUp;
                dp.MouseWheel += DrawPanel_MouseWheel;
                dp.MouseEnter += (s, e) => dp.Focus();
            }
            else
            {
                var panel = new Panel();
                panel.Name = "drawPanel";
                panel.Dock = DockStyle.Fill;
                var pi = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
                pi?.SetValue(panel, true, null);
                this.Controls.Add(panel);
                panel.BringToFront();

                panel.Paint += DrawPanel_Paint;
                panel.MouseDown += DrawPanel_MouseDown;
                panel.MouseMove += DrawPanel_MouseMove;
                panel.MouseUp += DrawPanel_MouseUp;
                panel.MouseWheel += DrawPanel_MouseWheel;
                panel.MouseEnter += (s, e) => panel.Focus();
            }
        }

        private Panel GetDrawPanel() => this.Controls.Find("drawPanel", true).FirstOrDefault() as Panel;
        private CheckedListBox GetFunctionsList() => this.Controls.Find("listFunctions", true).FirstOrDefault() as CheckedListBox;

        private void InitFunctionsList()
        {
            var clb = GetFunctionsList();
            if (clb == null)
            {
                clb = new CheckedListBox();
                clb.Name = "listFunctions";
                clb.CheckOnClick = true;
                clb.Width = 160;
                clb.Height = 120;
                clb.Left = 12;
                clb.Top = 12;
                this.Controls.Add(clb);
                clb.BringToFront();
            }

            clb.Items.Clear();
            foreach (var f in functions) clb.Items.Add(f.Name, false); 

            
            clb.ItemCheck -= ListFunctions_ItemCheck;
            clb.ItemCheck += ListFunctions_ItemCheck;
        }

        private void ListFunctions_ItemCheck(object sender, ItemCheckEventArgs e)
        {
           
            this.BeginInvoke(new Action(InvalidateDrawPanel));
        }

        private void ResetView()
        {
            scale = 1f;
            var dp = GetDrawPanel();
            if (dp != null) originPx = new PointF(dp.ClientSize.Width / 2f, dp.ClientSize.Height / 2f);
            InvalidateDrawPanel();
        }

        private void InvalidateDrawPanel() => GetDrawPanel()?.Invalidate();

        private System.Collections.Generic.IEnumerable<IFunction> EnumerateSelectedFunctions()
        {
            var clb = GetFunctionsList();
            if (clb == null) yield break;
            foreach (int idx in clb.CheckedIndices)
            {
                if (idx >= 0 && idx < functions.Count) yield return functions[idx];
            }
        }

        #region Button handlers
        private void btnRandom_Click(object sender, EventArgs e)
        {
            var rnd = new Random();
            var clb = GetFunctionsList();
            if (clb != null)
            {
                int idx = rnd.Next(functions.Count);
                bool currentlyChecked = clb.GetItemChecked(idx);
                clb.SetItemChecked(idx, !currentlyChecked);
                InvalidateDrawPanel();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var dp = GetDrawPanel();
            if (dp == null) return;

            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "PNG Image|*.png";
                dlg.DefaultExt = "png";
                dlg.AddExtension = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    SavePanelToImage(dlg.FileName);
                    MessageBox.Show("Сохранено: " + dlg.FileName, "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void btnColor_Click(object sender, EventArgs e)
        {
            using (var dlg = new ColorDialog())
            {
                dlg.Color = graphColor;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    graphColor = dlg.Color;
                    InvalidateDrawPanel();
                }
            }
        }

        private void btnFon_Click(object sender, EventArgs e)
        {
            using (var dlg = new BackgroundStyleDialog(bgStyle))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    bgStyle = dlg.ResultStyle;
                    InvalidateDrawPanel();
                }
            }
        }

        private void btnReset_Click(object sender, EventArgs e) => ResetView();
        #endregion

        #region Mouse & Wheel
        private void DrawPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                lastMouse = e.Location;
                (sender as Panel).Cursor = Cursors.Hand;
            }
        }

        private void DrawPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                var dx = e.X - lastMouse.X;
                var dy = e.Y - lastMouse.Y;
                originPx = new PointF(originPx.X + dx, originPx.Y + dy);
                lastMouse = e.Location;
                InvalidateDrawPanel();
            }
        }

        private void DrawPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && dragging)
            {
                dragging = false;
                (sender as Panel).Cursor = Cursors.Default;
            }
        }

        private void DrawPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            float delta = e.Delta / 120f;
            float zoomFactor = (float)Math.Pow(1.2, delta);
            float newScale = scale * zoomFactor;
            if (newScale < 0.20f) newScale = 0.20f;
            if (newScale > 100f) newScale = 100f;

            var panel = sender as Panel;
            Point mouse = panel != null ? panel.PointToClient(Cursor.Position) : this.PointToClient(Cursor.Position);
            PointF worldBefore = ScreenToWorld(mouse);
            scale = newScale;
            PointF worldAfter = ScreenToWorld(mouse);

            originPx = new PointF(
                originPx.X + (worldAfter.X - worldBefore.X) * PixelsPerUnit,
                originPx.Y + (worldAfter.Y - worldBefore.Y) * PixelsPerUnit
            );

            InvalidateDrawPanel();
        }
        #endregion

        private void SavePanelToImage(string path)
        {
            var dp = GetDrawPanel();
            if (dp == null) return;

            var size = dp.ClientSize;
            using (var bmp = new Bitmap(Math.Max(1, size.Width), Math.Max(1, size.Height)))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                var method = typeof(Form1).GetMethod("Render", BindingFlags.Instance | BindingFlags.NonPublic);
                if (method != null) method.Invoke(this, new object[] { g, size });
                bmp.Save(path, ImageFormat.Png);
            }
        }
    }
}