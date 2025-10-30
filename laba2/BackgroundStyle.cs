using System.Drawing;
using System.Drawing.Drawing2D;

namespace laba2
{
    public enum BackgroundFillStyle
    {
        SolidColor,
        GradientVertical,
        GradientHorizontal,
        GradientDiagonal,
        HatchSmallGrid,
        HatchCross,
        Checkerboard
    }

    public class BackgroundStyle
    {
        public BackgroundFillStyle Style { get; set; } = BackgroundFillStyle.SolidColor;
        public Color Primary { get; set; } = Color.White;
        public Color Secondary { get; set; } = Color.LightGray;
        public HatchStyle Hatch { get; set; } = HatchStyle.SmallCheckerBoard;
    }
}