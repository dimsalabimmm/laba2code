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
            var selected = new System.Collections.Generic.List<IFunction>(EnumerateSelectedFunctions());
            foreach (var f in selected)
            {
                DrawFunctionFor(g, size, f);
            }
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

        private void DrawFunctionFor(Graphics g, Size size, IFunction func)
        {
            if (func == null) return;

            using (var pen = new Pen(graphColor, 2f))
            using (var asym = new Pen(Color.FromArgb(80, graphColor), 1f))
            {
                pen.LineJoin = LineJoin.Round;
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                asym.DashStyle = DashStyle.Solid;

                double leftX = ScreenToWorld(new PointF(0, 0)).X;
                double rightX = ScreenToWorld(new PointF(size.Width, 0)).X;
                double visibleHeightUnits = size.Height / (double)PixelsPerUnit;
                double Yclip = Math.Max(1.0, visibleHeightUnits * 100.0);

                double EvalYSafe(double x)
                {
                    try
                    {
                        double v = func.Calc(x);
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

               
                int minSamples = size.Width * 3; 
                int adaptiveSamples = (int)(Math.Abs(rightX - leftX) * PixelsPerUnit * 3);
                int samples = Math.Max(minSamples, adaptiveSamples);
                samples = Math.Min(samples, 50000);

                var segment = new List<PointF>();
                var asymptotes = new HashSet<double>();

                PointF? prevPoint = null;
                bool prevWasValid = false;
                double? prevY = null;

                for (int i = 0; i <= samples; i++)
                {
                    double x = leftX + (rightX - leftX) * i / samples;
                    double y = EvalYSafe(x);

                    
                    bool yIsBad = double.IsNaN(y) || double.IsInfinity(y);
                    bool prevNearInf = prevY.HasValue && Math.Abs(prevY.Value) > Yclip * 0.9;
                    bool currNearInf = !yIsBad && Math.Abs(y) > Yclip * 0.9;

                   
                    if (yIsBad)
                    {
                        if (prevPoint.HasValue && prevY.HasValue)
                        {
                            float edgeYPrev = prevY.Value > 0 ? -1f : (size.Height + 1f);
                            segment.Add(new PointF(prevPoint.Value.X, edgeYPrev));
                            if (segment.Count >= 2) g.DrawLines(pen, segment.ToArray());
                        }
                        segment.Clear();
                        prevY = null;
                        prevPoint = null;
                        prevWasValid = false;
                        continue;
                    }

                    
                    double yClamped = y;
                    if (Math.Abs(y) > Yclip) yClamped = Math.Sign(y) * Yclip;

                    float screenX = originPx.X + (float)(x * PixelsPerUnit);
                    float screenY = originPx.Y - (float)(yClamped * PixelsPerUnit);
                    var pt = new PointF(screenX, screenY);

                    
                    bool hasDiscontinuity = false;
                    if (prevWasValid && prevPoint.HasValue && prevY.HasValue)
                    {
                        float dx = Math.Abs(pt.X - prevPoint.Value.X);
                        float dy = Math.Abs(pt.Y - prevPoint.Value.Y);
                        
                        bool isOppositeInf = prevNearInf && currNearInf && Math.Sign(prevY.Value) != Math.Sign(y);
                        bool isRawJump = Math.Abs(y - prevY.Value) > Yclip * 5.0; 
                        bool isVerticalJump = dy > size.Height * 2f && dx < 30f;

                        if (isOppositeInf || isRawJump || isVerticalJump)
                        {
                            hasDiscontinuity = true;
                            double asymptoteX = (x + ScreenToWorld(prevPoint.Value).X) / 2.0;
                            asymptotes.Add(asymptoteX);
                        }
                    }

                    
                    if (hasDiscontinuity)
                    {
                        if (prevPoint.HasValue && prevY.HasValue)
                        {
                            
                            float edgeYPrev = prevY.Value > 0 ? -1f : (size.Height + 1f);
                            segment.Add(new PointF(prevPoint.Value.X, edgeYPrev));
                        }
                        if (segment.Count >= 2) g.DrawLines(pen, segment.ToArray());
                        segment.Clear();

                        
                        float edgeYCur = y > 0 ? -1f : (size.Height + 1f);
                        segment.Add(new PointF(pt.X, edgeYCur));
                        segment.Add(pt);

                        prevY = y;
                        prevPoint = pt;
                        prevWasValid = true;
                        continue;
                    }

            
                    segment.Add(pt);
                    prevY = y;
                    prevPoint = pt;
                    prevWasValid = true;

                   
                    if (segment.Count >= 2000)
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