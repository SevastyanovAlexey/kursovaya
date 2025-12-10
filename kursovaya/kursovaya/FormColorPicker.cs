using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageProcessingApp
{
    public partial class FormColorPicker : Form
    {
        public Color SelectedColor { get; private set; }
        public bool UsePipette { get; private set; }

        private ColorDialog colorDialog;
        private Button btnOk;
        private Button btnCancel;
        private Button btnPipette;

        public FormColorPicker(Color initial)
        {
            InitializeComponent();
            SelectedColor = initial;
            colorDialog.Color = initial;
        }

        private void InitializeComponent()
        {
            Text = "Выбор цвета";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(320, 140);

            colorDialog = new ColorDialog();

            btnOk = new Button
            {
                Text = "ОК",
                Location = new Point(24, 72),
                Size = new Size(88, 28)
            };

            btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(120, 72),
                Size = new Size(88, 28)
            };

            btnPipette = new Button
            {
                Text = "Пипетка",
                Location = new Point(216, 72),
                Size = new Size(88, 28)
            };

            btnOk.Click += BtnOk_Click;
            btnCancel.Click += BtnCancel_Click;
            btnPipette.Click += BtnPipette_Click;

            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            Controls.Add(btnPipette);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (colorDialog.ShowDialog(this) == DialogResult.OK)
                SelectedColor = colorDialog.Color;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void BtnPipette_Click(object sender, EventArgs e)
        {
            UsePipette = true;
            DialogResult = DialogResult.Retry;
            Close();
        }
    }
}