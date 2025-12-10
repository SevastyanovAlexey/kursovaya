using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageProcessingApp
{
    public partial class FormTextEditor : Form
    {
        public string TextValue { get { return txtText.Text; } }
        public Color TextColor { get { return btnColor.BackColor; } }
        public float FontScale { get { return (float)numScale.Value; } }
        public int Thickness { get { return (int)numThickness.Value; } }

        private TextBox txtText;
        private Button btnOk;
        private Button btnCancel;
        private Button btnColor;
        private NumericUpDown numScale;
        private NumericUpDown numThickness;

        public FormTextEditor(string initialText, Color initialColor, float scale, int thickness)
        {
            InitializeComponent();

            txtText.Text = initialText;
            btnColor.BackColor = initialColor;
            numScale.Value = (decimal)scale;
            numThickness.Value = thickness;
        }

        private void InitializeComponent()
        {
            Text = "Текст";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(380, 170);

            txtText = new TextBox
            {
                Location = new Point(12, 12),
                Size = new Size(356, 24)
            };

            btnColor = new Button
            {
                Text = "Цвет",
                Location = new Point(12, 48),
                Size = new Size(84, 28)
            };

            numScale = new NumericUpDown
            {
                Minimum = 0.5M,
                Maximum = 5M,
                DecimalPlaces = 1,
                Increment = 0.1M,
                Value = 1M,
                Location = new Point(108, 48),
                Size = new Size(88, 24)
            };

            numThickness = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 20,
                Value = 2,
                Location = new Point(208, 48),
                Size = new Size(88, 24)
            };

            btnOk = new Button
            {
                Text = "ОК",
                Location = new Point(84, 104),
                Size = new Size(88, 28)
            };

            btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(196, 104),
                Size = new Size(88, 28)
            };

            btnColor.Click += BtnColor_Click;
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += BtnCancel_Click;

            Controls.Add(txtText);
            Controls.Add(btnColor);
            Controls.Add(numScale);
            Controls.Add(numThickness);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
        }

        private void BtnColor_Click(object sender, EventArgs e)
        {
            using (var cd = new ColorDialog { Color = btnColor.BackColor })
            {
                if (cd.ShowDialog() == DialogResult.OK)
                    btnColor.BackColor = cd.Color;
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
