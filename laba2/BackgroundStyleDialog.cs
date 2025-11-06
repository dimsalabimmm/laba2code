using System;
using System.Drawing;
using System.Windows.Forms;

namespace laba2
{
    public class BackgroundStyleDialog : Form
    {
        private RadioButton rbSolid, rbV, rbH, rbD, rbHatch, rbCross, rbChecker;
        private Button btnPrimary, btnSecondary;
        private Button btnOk, btnCancel;
        private BackgroundStyle style;
        public BackgroundStyle ResultStyle => style;

        public BackgroundStyleDialog(BackgroundStyle current)
        {
            style = new BackgroundStyle()
            {
                Style = current.Style,
                Primary = current.Primary,
                Secondary = current.Secondary,
                Hatch = current.Hatch
            };

            Text = "Выбор стиля фона";
            Width = 420;
            Height = 330;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            int left = 12, top = 12;
            int gap = 26;

            rbSolid = new RadioButton() { Left = left, Top = top, Width = 340, Text = "Сплошной цвет (только основной)" };
            rbV = new RadioButton() { Left = left, Top = top += gap, Width = 340, Text = "Вертикальный градиент" };
            rbH = new RadioButton() { Left = left, Top = top += gap, Width = 340, Text = "Горизонтальный градиент" };
            rbD = new RadioButton() { Left = left, Top = top += gap, Width = 340, Text = "Диагональный градиент" };
            rbHatch = new RadioButton() { Left = left, Top = top += gap, Width = 340, Text = "Штриховка (SmallGrid)" };
            rbCross = new RadioButton() { Left = left, Top = top += gap, Width = 340, Text = "Штриховка (Cross)" };
            rbChecker = new RadioButton() { Left = left, Top = top += gap, Width = 340, Text = "Шахматная доска" };

            Controls.AddRange(new Control[] { rbSolid, rbV, rbH, rbD, rbHatch, rbCross, rbChecker });

            btnPrimary = new Button() { Left = 12, Top = top + 40, Width = 160, Text = "Основной цвет" };
            btnSecondary = new Button() { Left = 190, Top = top + 40, Width = 160, Text = "Дополнительный цвет" };
            btnPrimary.Click += (s, e) => { using (var dlg = new ColorDialog() { Color = style.Primary }) if (dlg.ShowDialog() == DialogResult.OK) style.Primary = dlg.Color; };
            btnSecondary.Click += (s, e) => { using (var dlg = new ColorDialog() { Color = style.Secondary }) if (dlg.ShowDialog() == DialogResult.OK) style.Secondary = dlg.Color; };
            Controls.Add(btnPrimary);
            Controls.Add(btnSecondary);

            btnOk = new Button() { Left = 80, Top = Height - 90, Width = 100, Text = "OK", DialogResult = DialogResult.OK };
            btnCancel = new Button() { Left = 200, Top = Height - 90, Width = 100, Text = "Отмена", DialogResult = DialogResult.Cancel };
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            switch (style.Style)
            {
                case BackgroundFillStyle.SolidColor: rbSolid.Checked = true; break;
                case BackgroundFillStyle.GradientVertical: rbV.Checked = true; break;
                case BackgroundFillStyle.GradientHorizontal: rbH.Checked = true; break;
                case BackgroundFillStyle.GradientDiagonal: rbD.Checked = true; break;
                case BackgroundFillStyle.HatchSmallGrid: rbHatch.Checked = true; break;
                case BackgroundFillStyle.HatchCross: rbCross.Checked = true; break;
                case BackgroundFillStyle.Checkerboard: rbChecker.Checked = true; break;
                default: rbSolid.Checked = true; break;
            }

            btnOk.Click += (s, e) =>
            {
                if (rbSolid.Checked) style.Style = BackgroundFillStyle.SolidColor;
                else if (rbV.Checked) style.Style = BackgroundFillStyle.GradientVertical;
                else if (rbH.Checked) style.Style = BackgroundFillStyle.GradientHorizontal;
                else if (rbD.Checked) style.Style = BackgroundFillStyle.GradientDiagonal;
                else if (rbHatch.Checked) style.Style = BackgroundFillStyle.HatchSmallGrid;
                else if (rbCross.Checked) style.Style = BackgroundFillStyle.HatchCross;
                else if (rbChecker.Checked) style.Style = BackgroundFillStyle.Checkerboard;
                DialogResult = DialogResult.OK;
                Close();
            };
        }
    }
}