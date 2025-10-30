using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace laba2
{
    public partial class Form1
    {
        private float PixelsPerUnit => basePixelsPerUnit * scale;

        private PointF WorldToScreen(PointF world)
        {
            float px = originPx.X + world.X * PixelsPerUnit;
            float py = originPx.Y - world.Y * PixelsPerUnit;
            return new PointF(px, py);
        }

        private PointF ScreenToWorld(Point screen)
        {
            float wx = (screen.X - originPx.X) / PixelsPerUnit;
            float wy = (originPx.Y - screen.Y) / PixelsPerUnit;
            return new PointF(wx, wy);
        }

        private PointF ScreenToWorld(PointF screen)
        {
            float wx = (screen.X - originPx.X) / PixelsPerUnit;
            float wy = (originPx.Y - screen.Y) / PixelsPerUnit;
            return new PointF(wx, wy);
        }

        private void DrawPanel_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            Render(e.Graphics, (sender as Panel)?.ClientSize ?? this.ClientSize);
        }

        private void Render(Graphics g, Size size)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            DrawBackground(g, size);
            DrawAxes(g, size);
            DrawFunction(g, size);
            DrawUnitCircle(g, size);

            if (Math.Abs(scale - 1f) > 0.0001f)
            {
                var txt = $"Масштаб: {scale:0.00}×";
                var font = new Font("Segoe UI", 9);
                var textSize = g.MeasureString(txt, font);
                var rect = new RectangleF(size.Width - textSize.Width - 8, 8, textSize.Width, textSize.Height);
                using (var brush = new SolidBrush(Color.FromArgb(200, Color.White)))
                {
                    g.FillRectangle(brush, rect);
                }
                g.DrawString(txt, font, Brushes.Black, rect.Location);
            }
        }

        private void DrawBackground(Graphics g, Size size)
        {
            switch (bgStyle.Style)
            {
                case BackgroundFillStyle.SolidColor:
                    using (var b = new SolidBrush(bgStyle.Primary)) g.FillRectangle(b, 0, 0, size.Width, size.Height);
                    break;
                case BackgroundFillStyle.GradientVertical:
                    using (var lg = new LinearGradientBrush(new Rectangle(0, 0, size.Width, size.Height), bgStyle.Primary, bgStyle.Secondary, 90f)) g.FillRectangle(lg, 0, 0, size.Width, size.Height);
                    break;
                case BackgroundFillStyle.GradientHorizontal:
                    using (var lg = new LinearGradientBrush(new Rectangle(0, 0, size.Width, size.Height), bgStyle.Primary, bgStyle.Secondary, 0f)) g.FillRectangle(lg, 0, 0, size.Width, size.Height);
                    break;
                case BackgroundFillStyle.GradientDiagonal:
                    using (var lg = new LinearGradientBrush(new Rectangle(0, 0, size.Width, size.Height), bgStyle.Primary, bgStyle.Secondary, 45f)) g.FillRectangle(lg, 0, 0, size.Width, size.Height);
                    break;
                case BackgroundFillStyle.HatchSmallGrid:
                    using (var hb = new HatchBrush(System.Drawing.Drawing2D.HatchStyle.SmallGrid, bgStyle.Primary, bgStyle.Secondary)) g.FillRectangle(hb, 0, 0, size.Width, size.Height);
                    break;
                case BackgroundFillStyle.HatchCross:
                    using (var hb = new HatchBrush(System.Drawing.Drawing2D.HatchStyle.Cross, bgStyle.Primary, bgStyle.Secondary)) g.FillRectangle(hb, 0, 0, size.Width, size.Height);
                    break;
                case BackgroundFillStyle.Checkerboard:
                    int cell = 30;
                    for (int y = 0; y < size.Height; y += cell)
                        for (int x = 0; x < size.Width; x += cell)
                        {
                            bool dark = ((x / cell) + (y / cell)) % 2 == 0;
                            using (var b = new SolidBrush(dark ? bgStyle.Primary : bgStyle.Secondary))
                                g.FillRectangle(b, x, y, cell, cell);
                        }
                    break;
                default:
                    using (var b = new SolidBrush(Color.White)) g.FillRectangle(b, 0, 0, size.Width, size.Height);
                    break;
            }
        }

        private void DrawAxes(Graphics g, Size size)
        {
            using (var axisPen = new Pen(Color.Black, 1f))
            {
                axisPen.DashStyle = DashStyle.Solid;
                var p1 = WorldToScreen(new PointF(-10000, 0));
                var p2 = WorldToScreen(new PointF(10000, 0));
                g.DrawLine(axisPen, p1, p2);

                var q1 = WorldToScreen(new PointF(0, -10000));
                var q2 = WorldToScreen(new PointF(0, 10000));
                g.DrawLine(axisPen, q1, q2);
            }

            float pxPerUnit = PixelsPerUnit;
            if (pxPerUnit < 6) return;

            PointF topLeft = ScreenToWorld(new PointF(0, 0));
            PointF bottomRight = ScreenToWorld(new PointF(size.Width, size.Height));
            int xStart = (int)Math.Floor(topLeft.X) - 1;
            int xEnd = (int)Math.Ceiling(bottomRight.X) + 1;
            int yStart = (int)Math.Floor(bottomRight.Y) - 1;
            int yEnd = (int)Math.Ceiling(topLeft.Y) + 1;

            using (var pen = new Pen(Color.Gray, 1f))
            {
                pen.DashStyle = DashStyle.Dot;
                for (int xi = xStart; xi <= xEnd; xi++)
                {
                    var a = WorldToScreen(new PointF(xi, topLeft.Y));
                    var b = WorldToScreen(new PointF(xi, bottomRight.Y));
                    g.DrawLine(pen, a.X, a.Y, b.X, b.Y);
                }
                for (int yi = yStart; yi <= yEnd; yi++)
                {
                    var a = WorldToScreen(new PointF(topLeft.X, yi));
                    var b = WorldToScreen(new PointF(bottomRight.X, yi));
                    g.DrawLine(pen, a.X, a.Y, b.X, b.Y);
                }
            }

            using (var font = new Font("Segoe UI", 8))
            using (var brush = new SolidBrush(Color.Black))
            {
                for (int xi = xStart; xi <= xEnd; xi++)
                {
                    var p = WorldToScreen(new PointF(xi, 0));
                    g.DrawString(xi.ToString(), font, brush, p.X + 2, p.Y + 2);
                }
                for (int yi = yStart; yi <= yEnd; yi++)
                {
                    var p = WorldToScreen(new PointF(0, yi));
                    g.DrawString(yi.ToString(), font, brush, p.X + 2, p.Y + 2);
                }
            }
        }

        private void DrawUnitCircle(Graphics g, Size size)
        {
            float r = PixelsPerUnit * 1.0f;
            using (var pen = new Pen(Color.Black, 1.5f))
            {
                pen.DashStyle = DashStyle.Dot;
                var px = originPx.X;
                var py = originPx.Y;
                g.DrawEllipse(pen, px - r, py - r, r * 2, r * 2);
            }
        }

        // Стабильная версия DrawFunction (копируйте сюда финальную версию, что у нас работала).
        private void DrawFunction(Graphics g, Size size)
        {
            if (currentFunction == null) return;

            using (var pen = new Pen(graphColor, 2f))
            using (var asym = new Pen(Color.FromArgb(160, graphColor), 1f))
            {
                pen.LineJoin = LineJoin.Round;
                asym.DashStyle = DashStyle.Dash;

                double leftX = ScreenToWorld(new PointF(0, 0)).X;
                double rightX = ScreenToWorld(new PointF(size.Width, 0)).X;
                double visibleHeightUnits = size.Height / (double)PixelsPerUnit;
                double Yclip = Math.Max(1.0, visibleHeightUnits * 100.0);

                double EvalYSafe(double x)
                {
                    try
                    {
                        double v = currentFunction.Calc(x);
                        if (double.IsNaN(v)) return double.NaN;
                        if (double.IsInfinity(v)) return Math.Sign(v) * (Yclip * 1e3);
                        if (Math.Abs(v) > 1e300) return Math.Sign(v) * 1e300;
                        return v;
                    }
                    catch
                    {
                        return double.NaN;
                    }
                }

                if (currentFunction is TanFunction || (currentFunction.Name != null && currentFunction.Name.ToLower().Contains("tan")))
                {
                    double pi = Math.PI;
                    int kmin = (int)Math.Floor((leftX - pi / 2.0) / pi) - 1;
                    int kmax = (int)Math.Ceiling((rightX - pi / 2.0) / pi) + 1;

                    var asymptotes = new List<double>();
                    for (int k = kmin; k <= kmax; k++)
                    {
                        double ax = pi / 2.0 + k * pi;
                        if (ax >= leftX - 1.0 && ax <= rightX + 1.0) asymptotes.Add(ax);
                    }

                    var boundaries = new List<double>();
                    boundaries.Add(leftX);
                    foreach (var a in asymptotes.OrderBy(v => v)) boundaries.Add(a);
                    boundaries.Add(rightX);

                    for (int i = 0; i < boundaries.Count - 1; i++)
                    {
                        double segL = boundaries[i];
                        double segR = boundaries[i + 1];
                        double eps = Math.Max(1e-9, (segR - segL) * 1e-9);
                        double a = segL + eps;
                        double b = segR - eps;
                        if (a >= b) continue;

                        int n = Math.Max(3, (int)Math.Ceiling((b - a) * PixelsPerUnit));
                        n = Math.Max(3, Math.Min(10000, n));

                        var pts = new List<PointF>(n);
                        for (int j = 0; j < n; j++)
                        {
                            double x = a + (b - a) * j / (n - 1);
                            double y = EvalYSafe(x);
                            if (double.IsNaN(y)) continue;

                            double yClamped = y;
                            if (Math.Abs(y) > Yclip) yClamped = Math.Sign(y) * Yclip;

                            float screenX = WorldToScreen(new PointF((float)x, 0)).X;
                            float screenY = WorldToScreen(new PointF(0, 0)).Y - (float)(yClamped * PixelsPerUnit);
                            pts.Add(new PointF(screenX, screenY));
                        }

                        if (pts.Count >= 2)
                        {
                            g.DrawLines(pen, pts.ToArray());
                        }
                    }

                    foreach (var a in asymptotes)
                    {
                        float sx = WorldToScreen(new PointF((float)a, 0)).X;
                        g.DrawLine(asym, sx, 0, sx, size.Height);
                    }
                    return;
                }

                // Универсальная обработка остальных функций
                int width = size.Width;
                var segment = new List<PointF>();
                for (int px = 0; px < width; px++)
                {
                    PointF worldAtPixel = ScreenToWorld(new PointF(px, 0));
                    double x = worldAtPixel.X;
                    double y = EvalYSafe(x);

                    if (double.IsNaN(y))
                    {
                        if (segment.Count >= 2) g.DrawLines(pen, segment.ToArray());
                        segment.Clear();
                        continue;
                    }

                    double yClamped = y;
                    if (Math.Abs(y) > Yclip) yClamped = Math.Sign(y) * Yclip;

                    float screenX = originPx.X + (float)(x * PixelsPerUnit);
                    float screenY = originPx.Y - (float)(yClamped * PixelsPerUnit);
                    var pt = new PointF(screenX, screenY);

                    segment.Add(pt);

                    if (segment.Count >= 600)
                    {
                        if (segment.Count >= 2) g.DrawLines(pen, segment.ToArray());
                        var last = segment[segment.Count - 1];
                        segment.Clear();
                        segment.Add(last);
                    }
                }

                if (segment.Count >= 2) g.DrawLines(pen, segment.ToArray());
            }
        }
    }
}