using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageProcessingApp
{
    public partial class FormResize : Form
    {
        public int NewWidth { get { return (int)numW.Value; } }
        public int NewHeight { get { return (int)numH.Value; } }

        private NumericUpDown numW;
        private NumericUpDown numH;
        private Button btnOk;
        private Button btnCancel;

        public FormResize(int currentW, int currentH)
        {
            InitializeComponent();

            numW.Value = currentW;
            numH.Value = currentH;
        }

        private void InitializeComponent()
        {
            Text = "Изменить размер";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(280, 140);

            numW = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 10000,
                Value = 800,
                Location = new Point(16, 16),
                Size = new Size(110, 24)
            };

            numH = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 10000,
                Value = 600,
                Location = new Point(148, 16),
                Size = new Size(110, 24)
            };

            btnOk = new Button
            {
                Text = "ОК",
                Location = new Point(48, 72),
                Size = new Size(84, 28)
            };

            btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(148, 72),
                Size = new Size(84, 28)
            };

            btnOk.Click += BtnOk_Click;
            btnCancel.Click += BtnCancel_Click;

            Controls.Add(numW);
            Controls.Add(numH);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
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
