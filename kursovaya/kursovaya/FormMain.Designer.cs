using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageProcessingApp
{
    partial class FormMain
    {
        private System.ComponentModel.IContainer components;
        private TableLayoutPanel tableLayoutPanel;
        private FlowLayoutPanel leftPanel;
        private PictureBox pbImage;

        private Button btnLoad;
        private Button btnSave;
        private Button btnRotate90;
        private Button btnFlipH;
        private Button btnFlipV;
        private Button btnResetTransform;

        private Button btnZoomIn;
        private Button btnZoomOut;
        private Button btnZoomFit;

        private Button btnCrop;
        private Button btnApplyCrop;
        private Button btnResize;

        private Label lblBrightness;
        private Label lblContrast;
        private Label lblSaturation;
        private Label lblSharpness;
        private Label lblWarmth;
        private Label lblHue;
        private Label lblColorMix;

        private TrackBar trackBrightness;
        private TrackBar trackContrast;
        private TrackBar trackSaturation;
        private TrackBar trackSharpness;
        private TrackBar trackWarmth;
        private TrackBar trackHue;
        private TrackBar trackColorMix;

        private NumericUpDown numBrightness;
        private NumericUpDown numContrast;
        private NumericUpDown numSaturation;
        private NumericUpDown numSharpness;
        private NumericUpDown numWarmth;
        private NumericUpDown numHue;
        private NumericUpDown numColorMix;

        private Button btnDrawMode;
        private Button btnPickDrawColor;
        private NumericUpDown numDrawThickness;

        private Button btnTextMode;
        private Button btnColorAdjust;
        private Button btnColorFilter;

        private ComboBox comboEffects;
        private Button btnApplyEffect;

        private Button btnUndo;
        private Button btnRedo;
        private Button btnCopy;

        private StatusStrip statusStrip1;
        private ToolStripStatusLabel statusLabel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            ClientSize = new Size(1200, 760);
            Text = "Обработчик изображений";
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 600);
            MaximizeBox = true;

            tableLayoutPanel = new TableLayoutPanel();
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.ColumnCount = 2;
            tableLayoutPanel.RowCount = 1;
            tableLayoutPanel.ColumnStyles.Clear();
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260F));
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            Controls.Add(tableLayoutPanel);

            leftPanel = new FlowLayoutPanel();
            leftPanel.FlowDirection = FlowDirection.TopDown;
            leftPanel.WrapContents = false;
            leftPanel.AutoScroll = true;
            leftPanel.Padding = new Padding(8);
            leftPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.Controls.Add(leftPanel, 0, 0);

            pbImage = new PictureBox();
            pbImage.Dock = DockStyle.Fill;
            pbImage.SizeMode = PictureBoxSizeMode.Zoom;
            pbImage.BorderStyle = BorderStyle.FixedSingle;
            tableLayoutPanel.Controls.Add(pbImage, 1, 0);

            int xw = 220;

            btnLoad = new Button { Text = "Открыть", Width = xw, Height = 30 };
            btnSave = new Button { Text = "Сохранить", Width = xw, Height = 30 };
            btnRotate90 = new Button { Text = "Поворот 90°", Width = xw, Height = 30 };
            btnFlipH = new Button { Text = "Зеркало H", Width = xw, Height = 30 };
            btnFlipV = new Button { Text = "Зеркало V", Width = xw, Height = 30 };
            btnResetTransform = new Button { Text = "Сброс трансф.", Width = xw, Height = 30 };

            btnZoomIn = new Button { Text = "Увеличить", Width = xw / 2 - 3, Height = 30 };
            btnZoomOut = new Button { Text = "Уменьшить", Width = xw / 2 - 3, Height = 30 };
            btnZoomFit = new Button { Text = "По размеру", Width = xw, Height = 30 };

            btnCrop = new Button { Text = "Обрезка", Width = xw / 2 - 3, Height = 30 };
            btnApplyCrop = new Button { Text = "Применить", Width = xw / 2 - 3, Height = 30 };
            btnResize = new Button { Text = "Изменить размер", Width = xw, Height = 30 };

            lblBrightness = new Label { Text = "Яркость", Width = xw };
            trackBrightness = new TrackBar
            {
                Minimum = -100,
                Maximum = 100,
                Value = 0,
                Width = xw,
                TickStyle = TickStyle.None
            };
            numBrightness = new NumericUpDown
            {
                Minimum = -100,
                Maximum = 100,
                Value = 0,
                Width = 60
            };

            lblContrast = new Label { Text = "Контраст", Width = xw };
            trackContrast = new TrackBar
            {
                Minimum = -100,
                Maximum = 100,
                Value = 0,
                Width = xw,
                TickStyle = TickStyle.None
            };
            numContrast = new NumericUpDown
            {
                Minimum = -100,
                Maximum = 100,
                Value = 0,
                Width = 60
            };

            lblSaturation = new Label { Text = "Насыщенность", Width = xw };
            trackSaturation = new TrackBar
            {
                Minimum = -100,
                Maximum = 100,
                Value = 0,
                Width = xw,
                TickStyle = TickStyle.None
            };
            numSaturation = new NumericUpDown
            {
                Minimum = -100,
                Maximum = 100,
                Value = 0,
                Width = 60
            };

            lblSharpness = new Label { Text = "Чёткость", Width = xw };
            trackSharpness = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Width = xw,
                TickStyle = TickStyle.None
            };
            numSharpness = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Width = 60
            };

            lblWarmth = new Label { Text = "Теплота", Width = xw };
            trackWarmth = new TrackBar
            {
                Minimum = -50,
                Maximum = 50,
                Value = 0,
                Width = xw,
                TickStyle = TickStyle.None
            };
            numWarmth = new NumericUpDown
            {
                Minimum = -50,
                Maximum = 50,
                Value = 0,
                Width = 60
            };

            lblHue = new Label { Text = "Оттенок", Width = xw };
            trackHue = new TrackBar
            {
                Minimum = -90,
                Maximum = 90,
                Value = 0,
                Width = xw,
                TickStyle = TickStyle.None
            };
            numHue = new NumericUpDown
            {
                Minimum = -90,
                Maximum = 90,
                Value = 0,
                Width = 60
            };

            btnDrawMode = new Button { Text = "Рисование", Width = xw / 2 - 3, Height = 30 };
            btnPickDrawColor = new Button { Text = "Цвет", Width = xw / 2 - 3, Height = 30 };
            numDrawThickness = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 50,
                Value = 3,
                Width = xw / 2 - 3
            };

            btnTextMode = new Button { Text = "Текст", Width = xw, Height = 30 };

            btnColorAdjust = new Button { Text = "Замена цвета", Width = xw, Height = 30 };
            lblColorMix = new Label { Text = "Интенсивность замены", Width = xw };
            trackColorMix = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 90,
                Width = xw,
                TickStyle = TickStyle.None
            };
            numColorMix = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                Value = 90,
                Width = 60
            };
            btnColorFilter = new Button { Text = "Фильтр по цвету", Width = xw, Height = 30 };

            comboEffects = new ComboBox
            {
                Width = xw,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            btnApplyEffect = new Button { Text = "Применить эффект", Width = xw, Height = 30 };

            btnUndo = new Button { Text = "Отменить", Width = xw / 2 - 3, Height = 30 };
            btnRedo = new Button { Text = "Вернуть", Width = xw / 2 - 3, Height = 30 };
            btnCopy = new Button { Text = "Копировать", Width = xw, Height = 30 };

            statusStrip1 = new StatusStrip();
            statusLabel = new ToolStripStatusLabel { Text = "Готово" };
            statusStrip1.Items.Add(statusLabel);
            statusStrip1.Dock = DockStyle.Bottom;
            Controls.Add(statusStrip1);

            leftPanel.Controls.Add(btnLoad);
            leftPanel.Controls.Add(btnSave);
            leftPanel.Controls.Add(btnRotate90);
            leftPanel.Controls.Add(btnFlipH);
            leftPanel.Controls.Add(btnFlipV);
            leftPanel.Controls.Add(btnResetTransform);

            leftPanel.Controls.Add(btnZoomIn);
            leftPanel.Controls.Add(btnZoomOut);
            leftPanel.Controls.Add(btnZoomFit);

            leftPanel.Controls.Add(btnCrop);
            leftPanel.Controls.Add(btnApplyCrop);
            leftPanel.Controls.Add(btnResize);

            leftPanel.Controls.Add(lblBrightness);
            leftPanel.Controls.Add(trackBrightness);
            leftPanel.Controls.Add(numBrightness);

            leftPanel.Controls.Add(lblContrast);
            leftPanel.Controls.Add(trackContrast);
            leftPanel.Controls.Add(numContrast);

            leftPanel.Controls.Add(lblSaturation);
            leftPanel.Controls.Add(trackSaturation);
            leftPanel.Controls.Add(numSaturation);

            leftPanel.Controls.Add(lblSharpness);
            leftPanel.Controls.Add(trackSharpness);
            leftPanel.Controls.Add(numSharpness);

            leftPanel.Controls.Add(lblWarmth);
            leftPanel.Controls.Add(trackWarmth);
            leftPanel.Controls.Add(numWarmth);

            leftPanel.Controls.Add(lblHue);
            leftPanel.Controls.Add(trackHue);
            leftPanel.Controls.Add(numHue);

            leftPanel.Controls.Add(btnDrawMode);
            leftPanel.Controls.Add(btnPickDrawColor);
            leftPanel.Controls.Add(numDrawThickness);
            leftPanel.Controls.Add(btnTextMode);

            leftPanel.Controls.Add(btnColorAdjust);
            leftPanel.Controls.Add(lblColorMix);
            leftPanel.Controls.Add(trackColorMix);
            leftPanel.Controls.Add(numColorMix);
            leftPanel.Controls.Add(btnColorFilter);

            leftPanel.Controls.Add(comboEffects);
            leftPanel.Controls.Add(btnApplyEffect);

            leftPanel.Controls.Add(btnUndo);
            leftPanel.Controls.Add(btnRedo);
            leftPanel.Controls.Add(btnCopy);
        }
    }
}

