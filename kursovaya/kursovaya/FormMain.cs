using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

using SDPoint = System.Drawing.Point;
using SDSize = System.Drawing.Size;
using CVPoint = OpenCvSharp.Point;
using CVSize = OpenCvSharp.Size;

namespace ImageProcessingApp
{
    public partial class FormMain : Form
    {
        private Mat originalMat;
        private Mat currentMat;

        private int angle;
        private bool flipH;
        private bool flipV;

        private float displayZoom = 1.0f;
        private const float ZOOM_STEP = 1.25f;

        private bool cropMode;
        private SDPoint cropStart;
        private SDPoint cropEnd;
        private Rectangle cropRectScreen;

        private bool drawMode;
        private bool isDrawing;
        private SDPoint drawPrev;
        private Color drawColor = Color.Red;

        private bool placingText;
        private string placingTextValue = "";
        private Color placingTextColor = Color.White;
        private float placingTextScale = 1.0f;
        private int placingTextThickness = 2;
        private SDPoint placeStart;
        private SDPoint placeEnd;

        private bool pipetteMode;
        private Color pickedColor = Color.White;

        private readonly Stack<Mat> undoStack = new Stack<Mat>();
        private readonly Stack<Mat> redoStack = new Stack<Mat>();
        private const int MAX_UNDO = 40;

        private Timer toneTimer = new Timer { Interval = 160 };
        private bool toneChangePending;

        public FormMain()
        {
            InitializeComponent();
            WireEvents();
            PopulateEffects();
            ApplyTheme();

            Text = "Обработчик изображений";

            toneTimer.Tick += (s, e) =>
            {
                toneTimer.Stop();
                toneChangePending = false;
                ApplyToneAdjustments();
                UpdateUndoRedoButtons();
            };
        }

        private void ApplyTheme()
        {
            var bg = Color.FromArgb(45, 48, 52);
            var panelBg = Color.FromArgb(38, 40, 44);
            var fg = Color.FromArgb(220, 220, 220);

            BackColor = bg;

            foreach (Control c in Controls)
            {
                if (c is Panel)
                {
                    c.BackColor = panelBg;
                    c.ForeColor = fg;
                }
                else
                {
                    c.BackColor = bg;
                    c.ForeColor = fg;
                }

                var b = c as Button;
                if (b != null)
                {
                    b.FlatStyle = FlatStyle.Flat;
                    b.BackColor = Color.FromArgb(60, 63, 68);
                    b.ForeColor = fg;
                }
            }

            try
            {
                statusStrip1.BackColor = panelBg;
                statusLabel.ForeColor = Color.LightGray;
                pbImage.BackColor = Color.FromArgb(30, 30, 30);
            }
            catch { }
        }

        private void WireEvents()
        {
            if (pbImage == null)
                throw new InvalidOperationException("pbImage not found");

            btnLoad.Click += BtnLoad_Click;
            btnSave.Click += BtnSave_Click;

            btnRotate90.Click += (s, e) =>
            {
                PushUndo();
                angle = (angle + 90) % 360;
                ApplyGeometry();
                Redraw();
                UpdateUndoRedoButtons();
            };

            btnFlipH.Click += (s, e) =>
            {
                PushUndo();
                flipH = !flipH;
                ApplyGeometry();
                Redraw();
                UpdateUndoRedoButtons();
            };

            btnFlipV.Click += (s, e) =>
            {
                PushUndo();
                flipV = !flipV;
                ApplyGeometry();
                Redraw();
                UpdateUndoRedoButtons();
            };

            btnResetTransform.Click += (s, e) =>
            {
                PushUndo();
                angle = 0;
                flipH = false;
                flipV = false;
                ApplyGeometry();
                Redraw();
                UpdateUndoRedoButtons();
            };

            btnZoomIn.Click += (s, e) =>
            {
                displayZoom *= ZOOM_STEP;
                UpdatePictureBoxZoom();
            };

            btnZoomOut.Click += (s, e) =>
            {
                displayZoom /= ZOOM_STEP;
                UpdatePictureBoxZoom();
            };

            btnZoomFit.Click += (s, e) =>
            {
                displayZoom = 1.0f;
                UpdatePictureBoxZoom();
            };

            btnCrop.Click += (s, e) =>
            {
                cropMode = !cropMode;
                statusLabel.Text = cropMode ? "Режим обрезки" : "Обрезка выключена";

                if (!cropMode)
                {
                    cropRectScreen = Rectangle.Empty;
                    Redraw();
                }
            };

            btnApplyCrop.Click += (s, e) =>
            {
                PushUndo();
                ApplyCrop();
                UpdateUndoRedoButtons();
            };

            btnResize.Click += (s, e) =>
            {
                if (currentMat == null)
                {
                    MessageBox.Show("Откройте изображение.");
                    return;
                }

                using (var dlg = new FormResize(currentMat.Width, currentMat.Height))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        PushUndo();
                        ResizeImage(dlg.NewWidth, dlg.NewHeight);
                        Redraw();
                        UpdateUndoRedoButtons();
                    }
                }
            };

            var toneTracks = new[]
            {
                trackBrightness,
                trackContrast,
                trackSaturation,
                trackSharpness,
                trackWarmth,
                trackHue
            };

            foreach (var t in toneTracks)
            {
                if (t != null)
                    t.MouseDown += (s, e) => StartToneChange();
            }

            if (trackBrightness != null)
                trackBrightness.Scroll += (s, e) =>
                {
                    numBrightness.Value = trackBrightness.Value;
                    toneTimerRestart();
                };

            if (numBrightness != null)
                numBrightness.ValueChanged += (s, e) =>
                {
                    trackBrightness.Value = (int)numBrightness.Value;
                    toneTimerRestart();
                };

            if (trackContrast != null)
                trackContrast.Scroll += (s, e) =>
                {
                    numContrast.Value = trackContrast.Value;
                    toneTimerRestart();
                };

            if (numContrast != null)
                numContrast.ValueChanged += (s, e) =>
                {
                    trackContrast.Value = (int)numContrast.Value;
                    toneTimerRestart();
                };

            if (trackSaturation != null)
                trackSaturation.Scroll += (s, e) =>
                {
                    numSaturation.Value = trackSaturation.Value;
                    toneTimerRestart();
                };

            if (numSaturation != null)
                numSaturation.ValueChanged += (s, e) =>
                {
                    trackSaturation.Value = (int)numSaturation.Value;
                    toneTimerRestart();
                };

            if (trackSharpness != null)
                trackSharpness.Scroll += (s, e) =>
                {
                    numSharpness.Value = trackSharpness.Value;
                    toneTimerRestart();
                };

            if (numSharpness != null)
                numSharpness.ValueChanged += (s, e) =>
                {
                    trackSharpness.Value = (int)numSharpness.Value;
                    toneTimerRestart();
                };

            if (trackWarmth != null)
                trackWarmth.Scroll += (s, e) =>
                {
                    numWarmth.Value = trackWarmth.Value;
                    toneTimerRestart();
                };

            if (numWarmth != null)
                numWarmth.ValueChanged += (s, e) =>
                {
                    trackWarmth.Value = (int)numWarmth.Value;
                    toneTimerRestart();
                };

            if (trackHue != null)
                trackHue.Scroll += (s, e) =>
                {
                    numHue.Value = trackHue.Value;
                    toneTimerRestart();
                };

            if (numHue != null)
                numHue.ValueChanged += (s, e) =>
                {
                    trackHue.Value = (int)numHue.Value;
                    toneTimerRestart();
                };

            btnDrawMode.Click += (s, e) =>
            {
                StopDrawing();
                drawMode = !drawMode;
                statusLabel.Text = drawMode ? "Рисование" : "Рисование выкл";
            };

            btnPickDrawColor.Click += (s, e) =>
            {
                using (var cd = new ColorDialog { Color = drawColor })
                {
                    if (cd.ShowDialog() == DialogResult.OK)
                        drawColor = cd.Color;
                }
            };

            pbImage.MouseDown += PbImage_MouseDown;
            pbImage.MouseMove += PbImage_MouseMove;
            pbImage.MouseUp += PbImage_MouseUp;
            pbImage.MouseClick += PbImage_MouseClick;

            btnTextMode.Click += (s, e) =>
            {
                StopDrawing();
                AddText();
            };

            btnColorAdjust.Click += BtnChangeColor_Click;
            btnColorFilter.Click += BtnColorFilter_Click;

            if (trackColorMix != null)
                trackColorMix.Scroll += (s, e) =>
                {
                    numColorMix.Value = trackColorMix.Value;
                };

            if (numColorMix != null)
                numColorMix.ValueChanged += (s, e) =>
                {
                    trackColorMix.Value = (int)numColorMix.Value;
                };

            btnApplyEffect.Click += BtnApplyEffect_Click;

            btnUndo.Click += (s, e) => Undo();
            btnRedo.Click += (s, e) => Redo();

            btnCopy.Click += (s, e) =>
            {
                if (currentMat == null) return;
                Clipboard.SetImage(currentMat.ToBitmap());
                statusLabel.Text = "Скопировано в буфер";
            };
        }

        private void toneTimerRestart()
        {
            toneTimer.Stop();
            toneTimer.Start();
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            StopDrawing();

            using (var ofd = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff"
            })
            {
                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                ClearUndoRedo();

                if (originalMat != null) originalMat.Dispose();
                if (currentMat != null) currentMat.Dispose();

                originalMat = Cv2.ImRead(ofd.FileName, ImreadModes.Color);
                currentMat = originalMat.Clone();

                angle = 0;
                flipH = false;
                flipV = false;
                displayZoom = 1.0f;

                ResetToneUI();
                ApplyGeometry();
                Redraw();

                statusLabel.Text =
                    "Открыто: " + Path.GetFileName(ofd.FileName) +
                    " (" + currentMat.Width + "x" + currentMat.Height + ")";

                UpdateUndoRedoButtons();
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            StopDrawing();

            if (currentMat == null)
            {
                MessageBox.Show("Нет изображения для сохранения.");
                return;
            }

            using (var sfd = new SaveFileDialog
            {
                Filter = "PNG|*.png|JPEG|*.jpg;*.jpeg|BMP|*.bmp|TIFF|*.tif;*.tiff"
            })
            {
                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                Cv2.ImWrite(sfd.FileName, currentMat);
                statusLabel.Text = "Сохранено: " + Path.GetFileName(sfd.FileName);
            }
        }

        private void ApplyGeometry()
        {
            if (originalMat == null)
                return;

            var src = originalMat.Clone();

            if (angle == 90)
                Cv2.Rotate(src, src, RotateFlags.Rotate90Clockwise);
            else if (angle == 180)
                Cv2.Rotate(src, src, RotateFlags.Rotate180);
            else if (angle == 270)
                Cv2.Rotate(src, src, RotateFlags.Rotate90Counterclockwise);

            if (flipH) Cv2.Flip(src, src, FlipMode.Y);
            if (flipV) Cv2.Flip(src, src, FlipMode.X);

            if (currentMat != null) currentMat.Dispose();
            currentMat = src;
        }

        private void ResetToneUI()
        {
            try
            {
                trackBrightness.Value = 0;
                numBrightness.Value = 0;
                trackContrast.Value = 0;
                numContrast.Value = 0;
                trackSaturation.Value = 0;
                numSaturation.Value = 0;
                trackSharpness.Value = 0;
                numSharpness.Value = 0;
                trackWarmth.Value = 0;
                numWarmth.Value = 0;
                trackHue.Value = 0;
                numHue.Value = 0;

                if (trackColorMix != null) trackColorMix.Value = 90;
                if (numColorMix != null) numColorMix.Value = 90;
            }
            catch { }
        }

        private void StartToneChange()
        {
            if (toneChangePending) return;
            PushUndo();
            toneChangePending = true;
        }

        private void ApplyToneAdjustments()
        {
            if (originalMat == null)
                return;

            var baseImg = originalMat.Clone();

            if (angle == 90)
                Cv2.Rotate(baseImg, baseImg, RotateFlags.Rotate90Clockwise);
            else if (angle == 180)
                Cv2.Rotate(baseImg, baseImg, RotateFlags.Rotate180);
            else if (angle == 270)
                Cv2.Rotate(baseImg, baseImg, RotateFlags.Rotate90Counterclockwise);

            if (flipH) Cv2.Flip(baseImg, baseImg, FlipMode.Y);
            if (flipV) Cv2.Flip(baseImg, baseImg, FlipMode.X);

            double beta = trackBrightness.Value;
            double alpha = Math.Pow(2.0, trackContrast.Value / 50.0);

            var bc = new Mat();
            baseImg.ConvertTo(bc, MatType.CV_32FC3, alpha, beta);

            var bc8 = new Mat();
            bc.ConvertTo(bc8, MatType.CV_8UC3);

            var hsv = bc8.CvtColor(ColorConversionCodes.BGR2HSV);
            var ch = Cv2.Split(hsv);

            if (trackHue.Value != 0)
                Cv2.Add(ch[0], new Scalar(trackHue.Value / 2.0), ch[0]);

            Cv2.Add(ch[1], new Scalar(trackSaturation.Value), ch[1]);

            int warmth = trackWarmth.Value;

            if (Math.Abs(warmth) > 0)
            {
                double wNorm = warmth / 50.0;
                double hShift, sDelta, vDelta;

                if (wNorm < 0)
                {
                    hShift = (-wNorm) * 40.0;
                    sDelta = wNorm * 8.0;
                    vDelta = wNorm * 6.0;
                }
                else
                {
                    hShift = -wNorm * 30.0;
                    sDelta = wNorm * 12.0;
                    vDelta = wNorm * 8.0;
                }

                Cv2.Add(ch[0], new Scalar(hShift), ch[0]);
                Cv2.Add(ch[1], new Scalar(sDelta), ch[1]);
                Cv2.Add(ch[2], new Scalar(vDelta), ch[2]);
            }

            Cv2.Merge(ch, hsv);
            var satAdjusted = hsv.CvtColor(ColorConversionCodes.HSV2BGR);

            foreach (var c in ch) c.Dispose();
            hsv.Dispose();
            bc.Dispose();
            bc8.Dispose();
            baseImg.Dispose();

            var result = satAdjusted;

            if (trackSharpness.Value > 0)
            {
                var blurred = new Mat();
                Cv2.GaussianBlur(satAdjusted, blurred, new CVSize(0, 0), 1.2);

                double sharpK = 1.0 + (trackSharpness.Value / 100.0);
                var sharp = new Mat();

                Cv2.AddWeighted(satAdjusted, sharpK, blurred, -(sharpK - 1.0), 0, sharp);

                result.Dispose();
                blurred.Dispose();

                result = sharp;
            }

            if (currentMat != null) currentMat.Dispose();
            currentMat = result;

            Redraw();

            statusLabel.Text =
                "Ярк:" + trackBrightness.Value + " " +
                "Кон:" + trackContrast.Value + " " +
                "Нас:" + trackSaturation.Value + " " +
                "Чёт:" + trackSharpness.Value + " " +
                "Тепл:" + trackWarmth.Value + " " +
                "Отт:" + trackHue.Value;
        }

        private void BtnChangeColor_Click(object sender, EventArgs e)
        {
            if (currentMat == null) return;

            using (var cd = new ColorDialog { Color = pickedColor })
            {
                if (cd.ShowDialog() != DialogResult.OK)
                    return;

                PushUndo();

                pickedColor = cd.Color;

                double hueDeg, sDummy, vDummy;
                ColorToHsv(pickedColor, out hueDeg, out sDummy, out vDummy);
                double targetH = hueDeg / 2.0;

                var src8 = Ensure8U(currentMat, 3);
                var hsv = src8.CvtColor(ColorConversionCodes.BGR2HSV);
                var ch = Cv2.Split(hsv);

                double mix =
                    trackColorMix != null
                        ? trackColorMix.Value / 100.0
                        : (double)numColorMix.Value / 100.0;

                var hFloat = new Mat();
                ch[0].ConvertTo(hFloat, MatType.CV_32F);

                Cv2.Multiply(hFloat, new Scalar(1.0 - mix), hFloat);

                var addMat = new Mat(hFloat.Size(), MatType.CV_32F, new Scalar(targetH * mix));
                Cv2.Add(hFloat, addMat, hFloat);

                var hOut = new Mat();
                hFloat.ConvertTo(hOut, MatType.CV_8U);

                ch[0].Dispose();
                hFloat.Dispose();
                addMat.Dispose();

                ch[0] = hOut;

                Cv2.Add(ch[1], new Scalar(30 * mix), ch[1]);
                Cv2.Merge(ch, hsv);

                var recolored = hsv.CvtColor(ColorConversionCodes.HSV2BGR);

                foreach (var c in ch) c.Dispose();
                hsv.Dispose();
                src8.Dispose();

                currentMat.Dispose();
                currentMat = recolored;

                Redraw();
                UpdateUndoRedoButtons();

                statusLabel.Text =
                    "Замена цвета: R" + pickedColor.R +
                    " G" + pickedColor.G +
                    " B" + pickedColor.B +
                    " (инт.: " + Math.Round(mix * 100) + "%)";
            }
        }

        private void BtnColorFilter_Click(object sender, EventArgs e)
        {
            StopDrawing();

            if (currentMat == null)
                return;

            using (var picker = new FormColorPicker(pickedColor))
            {
                var res = picker.ShowDialog(this);

                if (res == DialogResult.Retry || picker.UsePipette)
                {
                    pipetteMode = true;
                    pbImage.MouseClick += PbImage_PipetteClickOnce;
                    statusLabel.Text = "Пипетка включена";
                    return;
                }

                if (res != DialogResult.OK)
                    return;

                PushUndo();

                pickedColor = picker.SelectedColor;

                double hue, sDummy, vDummy;
                ColorToHsv(pickedColor, out hue, out sDummy, out vDummy);

                double h = hue / 2.0;
                double deltaH = 12;

                var src8 = Ensure8U(currentMat, 3);
                var hsv = src8.CvtColor(ColorConversionCodes.BGR2HSV);

                var mask = new Mat();
                Cv2.InRange(
                    hsv,
                    new Scalar(Math.Max(0, h - deltaH), 50, 50),
                    new Scalar(Math.Min(180, h + deltaH), 255, 255),
                    mask
                );

                var inv = new Mat();
                Cv2.BitwiseNot(mask, inv);

                var colored = src8.Clone();

                var gray = src8
                    .CvtColor(ColorConversionCodes.BGR2GRAY)
                    .CvtColor(ColorConversionCodes.GRAY2BGR);

                var fg = new Mat();
                Cv2.BitwiseAnd(colored, colored, fg, mask);

                var bg = new Mat();
                Cv2.BitwiseAnd(gray, gray, bg, inv);

                var filtered = new Mat();
                Cv2.Add(fg, bg, filtered);

                src8.Dispose();
                hsv.Dispose();
                mask.Dispose();
                inv.Dispose();
                colored.Dispose();
                gray.Dispose();
                fg.Dispose();
                bg.Dispose();

                currentMat.Dispose();
                currentMat = filtered;

                Redraw();
                UpdateUndoRedoButtons();

                statusLabel.Text = "Цветовой фильтр применён";
            }
        }

        private void PbImage_PipetteClickOnce(object sender, MouseEventArgs e)
        {
            if (!pipetteMode) return;

            int x, y;
            MapToImageCoords(pbImage, out x, out y, e.Location);

            var bgr = currentMat.At<Vec3b>(y, x);
            pickedColor = Color.FromArgb(bgr.Item2, bgr.Item1, bgr.Item0);

            pipetteMode = false;
            pbImage.MouseClick -= PbImage_PipetteClickOnce;

            statusLabel.Text =
                "Пипетка: R" + pickedColor.R +
                " G" + pickedColor.G +
                " B" + pickedColor.B;
        }

        private void PopulateEffects()
        {
            try
            {
                comboEffects.Items.Clear();

                comboEffects.Items.AddRange(new object[]
                {
                    "Чёрно‑белый",
                    "Карандаш",
                    "Пикселизация",
                    "Пазл",
                    "HDR усиление",
                    "Тёплый тон",
                    "Холодный тон",
                    "Резкость",
                    "Размытие (мягко)",
                    "Сдвиг оттенка +30",
                    "Сдвиг оттенка -30",
                    "Сепия",
                    "Дуотон (синий → оранжевый)",
                    "Цианотип (холодный синий)",
                    "Кросс‑процесс",
                    "Инфракрасный",
                    "Выделение цвета",
                    "Пленочный шум",
                    "Винтажная плёнка",
                    "Тёплый портрет",
                    "Bleach Bypass",
                    "Matte (мягкая матовость)",
                    "High Key",
                    "Low Key"
                });

                comboEffects.SelectedIndex = 0;
            }
            catch { }
        }

        private void BtnApplyEffect_Click(object sender, EventArgs e)
        {
            StopDrawing();

            if (currentMat == null) return;

            var name = comboEffects.SelectedItem != null
                ? comboEffects.SelectedItem.ToString()
                : null;

            if (string.IsNullOrEmpty(name)) return;

            PushUndo();

            switch (name)
            {
                case "Чёрно‑белый":
                    EffectHighContrastBW();
                    break;
                case "Карандаш":
                    EffectPencilSketch();
                    break;
                case "Пикселизация":
                    EffectPixelate(12);
                    break;
                case "Пазл":
                    EffectPuzzle(4);
                    break;
                case "HDR усиление":
                    EffectHdrBoost();
                    break;
                case "Тёплый тон":
                    EffectWarmTone();
                    break;
                case "Холодный тон":
                    EffectColdTone();
                    break;
                case "Резкость":
                    EffectSharpen();
                    break;
                case "Размытие (мягко)":
                    EffectSoftBlur();
                    break;
                case "Сдвиг оттенка +30":
                    EffectHueShift(+30);
                    break;
                case "Сдвиг оттенка -30":
                    EffectHueShift(-30);
                    break;
                case "Сепия":
                    EffectSepia();
                    break;
                case "Дуотон (синий → оранжевый)":
                    EffectDuotone(new Scalar(100, 60, 20), new Scalar(10, 120, 200));
                    break;
                case "Цианотип (холодный синий)":
                    EffectCyanotype();
                    break;
                case "Кросс‑процесс":
                    EffectCrossProcess();
                    break;
                case "Инфракрасный":
                    EffectInfrared();
                    break;
                case "Выделение цвета":
                    EffectColorPop(pickedColor);
                    break;
                case "Пленочный шум":
                    EffectFilmGrain();
                    break;
                case "Винтажная плёнка":
                    EffectVintageFilm();
                    break;
                case "Тёплый портрет":
                    EffectWarmPortrait();
                    break;
                case "Bleach Bypass":
                    EffectBleachBypass();
                    break;
                case "Matte (мягкая матовость)":
                    EffectMatte();
                    break;
                case "High Key":
                    EffectHighKey();
                    break;
                case "Low Key":
                    EffectLowKey();
                    break;
            }

            Redraw();
            statusLabel.Text = "Эффект применён: " + name;
            UpdateUndoRedoButtons();
        }

        private void EffectHighContrastBW()
        {
            if (currentMat == null) return;

            var gray = Ensure8U(
                currentMat.CvtColor(ColorConversionCodes.BGR2GRAY), 1
            );

            var clahe = Cv2.CreateCLAHE(2.0, new CVSize(8, 8));

            var enhanced = new Mat();
            clahe.Apply(gray, enhanced);

            var contrasted = new Mat();
            enhanced.ConvertTo(contrasted, MatType.CV_8UC1, 1.15, -10);

            var result = contrasted.CvtColor(ColorConversionCodes.GRAY2BGR);

            gray.Dispose();
            enhanced.Dispose();
            contrasted.Dispose();
            clahe.Dispose();

            currentMat.Dispose();
            currentMat = result;
        }

        private void EffectPencilSketch()
        {
            if (currentMat == null) return;

            var gray = Ensure8U(
                currentMat.CvtColor(ColorConversionCodes.BGR2GRAY), 1
            );

            var inv = new Mat();
            Cv2.BitwiseNot(gray, inv);

            var blur = new Mat();
            Cv2.GaussianBlur(inv, blur, new CVSize(21, 21), 0);

            var invBlur = new Mat();
            Cv2.BitwiseNot(blur, invBlur);

            var blend = new Mat();
            Cv2.Divide(gray, invBlur, blend, scale: 256.0);

            var sketch = blend.CvtColor(ColorConversionCodes.GRAY2BGR);

            gray.Dispose();
            inv.Dispose();
            blur.Dispose();
            invBlur.Dispose();
            blend.Dispose();

            currentMat.Dispose();
            currentMat = sketch;
        }

        private void EffectPixelate(int pixelSize = 10)
        {
            if (currentMat == null) return;

            var small = new Mat();
            Cv2.Resize(
                currentMat, small,
                new CVSize(
                    Math.Max(1, currentMat.Width / pixelSize),
                    Math.Max(1, currentMat.Height / pixelSize)
                ),
                0, 0, InterpolationFlags.Area
            );

            var up = new Mat();
            Cv2.Resize(
                small, up,
                new CVSize(currentMat.Width, currentMat.Height),
                0, 0, InterpolationFlags.Nearest
            );

            small.Dispose();
            currentMat.Dispose();
            currentMat = up;
        }

        private void EffectPuzzle(int grid = 4)
        {
            if (currentMat == null) return;

            var src = currentMat.Clone();

            int w = src.Width;
            int h = src.Height;

            int cellW = Math.Max(1, w / grid);
            int cellH = Math.Max(1, h / grid);

            var tiles = new List<Mat>();

            for (int gy = 0; gy < grid; gy++)
                for (int gx = 0; gx < grid; gx++)
                {
                    var rect = new Rect(gx * cellW, gy * cellH, cellW, cellH);
                    tiles.Add(new Mat(src, rect).Clone());
                }

            var rnd = new Random();
            var order = Enumerable
                .Range(0, tiles.Count)
                .OrderBy(_ => rnd.Next())
                .ToArray();

            var dst = new Mat(src.Size(), src.Type());

            int idx = 0;

            for (int gy = 0; gy < grid; gy++)
                for (int gx = 0; gx < grid; gx++)
                {
                    var rect = new Rect(gx * cellW, gy * cellH, cellW, cellH);
                    var tile = tiles[order[idx++]];
                    tile.CopyTo(new Mat(dst, rect));
                    tile.Dispose();
                }

            foreach (var t in tiles) t.Dispose();
            src.Dispose();

            currentMat.Dispose();
            currentMat = dst;
        }

        private void EffectHdrBoost()
        {
            if (currentMat == null) return;

            var bc = new Mat();
            currentMat.ConvertTo(bc, -1, 1.15, 0);

            var blurred = new Mat();
            Cv2.GaussianBlur(bc, blurred, new CVSize(0, 0), 1.2);

            var sharp = new Mat();
            Cv2.AddWeighted(bc, 1.25, blurred, -0.25, 0, sharp);

            bc.Dispose();
            blurred.Dispose();

            currentMat.Dispose();
            currentMat = sharp;
        }

        private void EffectWarmTone() { ApplyHSVShift(-5, 10, 5); }
        private void EffectColdTone() { ApplyHSVShift(8, -8, -3); }

        private void EffectSharpen()
        {
            if (currentMat == null) return;

            var kernel = new Mat(3, 3, MatType.CV_32F);

            float[,] k =
            {
                { 0, -1, 0 },
                { -1, 5, -1 },
                { 0, -1, 0 }
            };

            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    kernel.Set(r, c, k[r, c]);

            var dst = new Mat();
            Cv2.Filter2D(currentMat, dst, currentMat.Type(), kernel);

            kernel.Dispose();

            currentMat.Dispose();
            currentMat = dst;
        }

        private void EffectSoftBlur()
        {
            if (currentMat == null) return;

            var dst = new Mat();
            Cv2.GaussianBlur(currentMat, dst, new CVSize(7, 7), 2.0);

            currentMat.Dispose();
            currentMat = dst;
        }

        private void EffectHueShift(int shiftDegrees)
        {
            if (currentMat == null) return;

            var src = Ensure8U(currentMat, 3);
            var hsv = src.CvtColor(ColorConversionCodes.BGR2HSV);
            var ch = Cv2.Split(hsv);

            Cv2.Add(ch[0], new Scalar(shiftDegrees / 2.0), ch[0]);
            Cv2.Merge(ch, hsv);

            var res = hsv.CvtColor(ColorConversionCodes.HSV2BGR);

            foreach (var c in ch) c.Dispose();
            hsv.Dispose();
            src.Dispose();

            currentMat.Dispose();
            currentMat = res;
        }

        private void EffectSepia()
        {
            if (currentMat == null) return;

            var src = Ensure8U(currentMat, 3);

            var kernel = new Mat(3, 3, MatType.CV_32F);

            float[,] k =
            {
                { 0.272f, 0.534f, 0.131f },
                { 0.349f, 0.686f, 0.168f },
                { 0.393f, 0.769f, 0.189f }
            };

            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    kernel.Set(r, c, k[r, c]);

            var dst = new Mat();
            Cv2.Transform(src, dst, kernel);

            kernel.Dispose();
            src.Dispose();

            currentMat.Dispose();
            currentMat = dst;
        }

        private void EffectDuotone(Scalar c1, Scalar c2)
        {
            if (currentMat == null) return;

            var gray = Ensure8U(
                currentMat.CvtColor(ColorConversionCodes.BGR2GRAY), 1
            );

            var dst = new Mat(currentMat.Size(), MatType.CV_8UC3);

            for (int y = 0; y < gray.Rows; y++)
                for (int x = 0; x < gray.Cols; x++)
                {
                    byte v = gray.At<byte>(y, x);
                    double t = v / 255.0;

                    byte b = (byte)Math.Min(255, Math.Round(c1.Val0 * (1 - t) + c2.Val0 * t));
                    byte g = (byte)Math.Min(255, Math.Round(c1.Val1 * (1 - t) + c2.Val1 * t));
                    byte r = (byte)Math.Min(255, Math.Round(c1.Val2 * (1 - t) + c2.Val2 * t));

                    dst.Set(y, x, new Vec3b(b, g, r));
                }

            gray.Dispose();

            currentMat.Dispose();
            currentMat = dst;
        }

        private void EffectCyanotype()
        {
            EffectDuotone(new Scalar(20, 40, 80), new Scalar(180, 220, 255));
        }

        private void EffectCrossProcess()
        {
            if (currentMat == null) return;

            var src = Ensure8U(currentMat, 3);
            var hsv = src.CvtColor(ColorConversionCodes.BGR2HSV);
            var ch = Cv2.Split(hsv);

            Cv2.Add(ch[0], new Scalar(8), ch[0]);
            Cv2.Add(ch[1], new Scalar(20), ch[1]);
            Cv2.Add(ch[2], new Scalar(-10), ch[2]);

            Cv2.Merge(ch, hsv);

            var res = hsv.CvtColor(ColorConversionCodes.HSV2BGR);

            foreach (var c in ch) c.Dispose();
            hsv.Dispose();
            src.Dispose();

            currentMat.Dispose();
            currentMat = res;
        }

        private void EffectInfrared()
        {
            if (currentMat == null) return;

            var src = Ensure8U(currentMat, 3);
            var lab = src.CvtColor(ColorConversionCodes.BGR2Lab);
            var ch = Cv2.Split(lab);

            Cv2.Add(ch[0], new Scalar(30), ch[0]);
            Cv2.Add(ch[1], new Scalar(-10), ch[1]);
            Cv2.Add(ch[2], new Scalar(10), ch[2]);

            Cv2.Merge(ch, lab);

            var res = lab.CvtColor(ColorConversionCodes.Lab2BGR);

            foreach (var c in ch) c.Dispose();
            lab.Dispose();
            src.Dispose();

            currentMat.Dispose();
            currentMat = res;
        }

        private void EffectColorPop(Color target)
        {
            if (currentMat == null) return;

            var src = Ensure8U(currentMat, 3);

            var hsv = src.CvtColor(ColorConversionCodes.BGR2HSV);
            var ch = Cv2.Split(hsv);

            double hDeg, sDummy, vDummy;
            ColorToHsv(target, out hDeg, out sDummy, out vDummy);
            double h = hDeg / 2.0;

            var mask = new Mat();
            Cv2.InRange(
                hsv,
                new Scalar(Math.Max(0, h - 8), 50, 50),
                new Scalar(Math.Min(180, h + 8), 255, 255),
                mask
            );

            var gray = src
                .CvtColor(ColorConversionCodes.BGR2GRAY)
                .CvtColor(ColorConversionCodes.GRAY2BGR);

            var fg = new Mat();
            Cv2.BitwiseAnd(src, src, fg, mask);

            var inv = new Mat();
            Cv2.BitwiseNot(mask, inv);

            var bg = new Mat();
            Cv2.BitwiseAnd(gray, gray, bg, inv);

            var outMat = new Mat();
            Cv2.Add(fg, bg, outMat);

            src.Dispose();
            hsv.Dispose();
            foreach (var c in ch) c.Dispose();
            mask.Dispose();
            gray.Dispose();
            fg.Dispose();
            inv.Dispose();
            bg.Dispose();

            currentMat.Dispose();
            currentMat = outMat;
        }

        private void EffectFilmGrain()
        {
            if (currentMat == null) return;

            var src = Ensure8U(currentMat, 3);
            var noise = new Mat(src.Size(), MatType.CV_8UC3);

            Cv2.Randu(noise, new Scalar(0, 0, 0), new Scalar(255, 255, 255));

            var blended = new Mat();
            Cv2.AddWeighted(src, 0.95, noise, 0.05, 0, blended);

            noise.Dispose();
            src.Dispose();

            currentMat.Dispose();
            currentMat = blended;
        }

        private void EffectVintageFilm()
        {
            if (currentMat == null) return;

            var src = Ensure8U(currentMat, 3);
            var hsv = src.CvtColor(ColorConversionCodes.BGR2HSV);
            var ch = Cv2.Split(hsv);

            Cv2.Add(ch[0], new Scalar(-6), ch[0]);
            Cv2.Add(ch[1], new Scalar(-10), ch[1]);
            Cv2.Add(ch[2], new Scalar(-4), ch[2]);

            Cv2.Merge(ch, hsv);

            var res = hsv.CvtColor(ColorConversionCodes.HSV2BGR);

            foreach (var c in ch) c.Dispose();
            hsv.Dispose();
            src.Dispose();

            currentMat.Dispose();
            currentMat = res;
        }

        private void EffectWarmPortrait()
        {
            if (currentMat == null) return;

            var src = Ensure8U(currentMat, 3);
            var hsv = src.CvtColor(ColorConversionCodes.BGR2HSV);
            var ch = Cv2.Split(hsv);

            Cv2.Add(ch[0], new Scalar(-8), ch[0]);
            Cv2.Add(ch[1], new Scalar(15), ch[1]);
            Cv2.Add(ch[2], new Scalar(6), ch[2]);

            Cv2.Merge(ch, hsv);

            var res = hsv.CvtColor(ColorConversionCodes.HSV2BGR);

            foreach (var c in ch) c.Dispose();
            hsv.Dispose();
            src.Dispose();

            currentMat.Dispose();
            currentMat = res;
        }

        private void EffectBleachBypass()
        {
            if (currentMat == null) return;

            var src = Ensure8U(currentMat, 3);
            var lab = src.CvtColor(ColorConversionCodes.BGR2Lab);
            var ch = Cv2.Split(lab);

            Cv2.Add(ch[0], new Scalar(20), ch[0]);
            Cv2.Add(ch[1], new Scalar(-10), ch[1]);
            Cv2.Add(ch[2], new Scalar(-10), ch[2]);

            Cv2.Merge(ch, lab);

            var res = lab.CvtColor(ColorConversionCodes.Lab2BGR);

            foreach (var c in ch) c.Dispose();
            lab.Dispose();
            src.Dispose();

            currentMat.Dispose();
            currentMat = res;
        }

        private void EffectMatte()
        {
            if (currentMat == null) return;

            var src = Ensure8U(currentMat, 3);

            var blurred = new Mat();
            Cv2.GaussianBlur(src, blurred, new CVSize(21, 21), 0);

            var dst = new Mat();
            Cv2.AddWeighted(src, 0.85, blurred, 0.15, 0, dst);

            blurred.Dispose();
            src.Dispose();

            currentMat.Dispose();
            currentMat = dst;
        }

        private void EffectHighKey()
        {
            if (currentMat == null) return;

            var src = Ensure8U(currentMat, 3);
            var hsv = src.CvtColor(ColorConversionCodes.BGR2HSV);
            var ch = Cv2.Split(hsv);

            Cv2.Add(ch[2], new Scalar(30), ch[2]);
            Cv2.Merge(ch, hsv);

            var res = hsv.CvtColor(ColorConversionCodes.HSV2BGR);

            foreach (var c in ch) c.Dispose();
            hsv.Dispose();
            src.Dispose();

            currentMat.Dispose();
            currentMat = res;
        }

        private void EffectLowKey()
        {
            if (currentMat == null) return;

            var src = Ensure8U(currentMat, 3);
            var hsv = src.CvtColor(ColorConversionCodes.BGR2HSV);
            var ch = Cv2.Split(hsv);

            Cv2.Add(ch[2], new Scalar(-40), ch[2]);
            Cv2.Add(ch[1], new Scalar(8), ch[1]);

            Cv2.Merge(ch, hsv);

            var res = hsv.CvtColor(ColorConversionCodes.HSV2BGR);

            foreach (var c in ch) c.Dispose();
            hsv.Dispose();
            src.Dispose();

            currentMat.Dispose();
            currentMat = res;
        }

        private void PbImage_MouseDown(object sender, MouseEventArgs e)
        {
            if (placingText) return;

            if (cropMode)
            {
                cropStart = e.Location;
                cropEnd = e.Location;
                cropRectScreen = Rectangle.Empty;
                return;
            }

            if (drawMode && e.Button == MouseButtons.Left)
            {
                PushUndo();
                isDrawing = true;
                drawPrev = e.Location;
                UpdateUndoRedoButtons();
            }
        }

        private void PbImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (cropMode && e.Button == MouseButtons.Left)
            {
                cropEnd = e.Location;
                cropRectScreen = GetScreenRect(cropStart, cropEnd);
                DrawCropPreview();
                return;
            }

            if (placingText) return;
            if (!drawMode || !isDrawing || currentMat == null) return;

            int x1, y1, x2, y2;
            MapToImageCoords(pbImage, out x1, out y1, drawPrev);
            MapToImageCoords(pbImage, out x2, out y2, e.Location);

            Cv2.Line(
                currentMat,
                new CVPoint(x1, y1),
                new CVPoint(x2, y2),
                new Scalar(drawColor.B, drawColor.G, drawColor.R),
                (int)numDrawThickness.Value,
                LineTypes.AntiAlias
            );

            drawPrev = e.Location;
            Redraw();
        }

        private void PbImage_MouseUp(object sender, MouseEventArgs e)
        {
            if (cropMode)
            {
                cropEnd = e.Location;
                cropRectScreen = GetScreenRect(cropStart, cropEnd);
                DrawCropPreview();
                return;
            }

            if (placingText) return;

            if (drawMode && isDrawing)
            {
                isDrawing = false;
                statusLabel.Text = "Рисование применено";
                UpdateUndoRedoButtons();
            }
        }

        private void PbImage_MouseClick(object sender, MouseEventArgs e)
        {
            if (pipetteMode)
            {
                PbImage_PipetteClickOnce(sender, e);
                UpdateUndoRedoButtons();
            }
        }

        private void DrawCropPreview()
        {
            if (currentMat == null) return;

            var bmp = currentMat.ToBitmap();

            using (var g = Graphics.FromImage(bmp))
            {
                if (!cropRectScreen.IsEmpty)
                {
                    var rect = ScreenRectToImageRect(
                        cropRectScreen, bmp.Size, pbImage
                    );

                    using (var pen = new Pen(Color.Lime, 3))
                    {
                        g.DrawRectangle(pen, rect);
                    }
                }
            }

            if (pbImage.Image != null) pbImage.Image.Dispose();
            pbImage.Image = bmp;
        }

        private Rectangle GetScreenRect(SDPoint a, SDPoint b)
        {
            return new Rectangle(
                Math.Min(a.X, b.X),
                Math.Min(a.Y, b.Y),
                Math.Abs(a.X - b.X),
                Math.Abs(a.Y - b.Y)
            );
        }

        private Rectangle ScreenRectToImageRect(
            Rectangle screenRect, SDSize bmpSize, PictureBox pb
        )
        {
            int x1, y1, x2, y2;

            MapToImageCoords(
                pb, out x1, out y1,
                new SDPoint(screenRect.Left, screenRect.Top)
            );

            MapToImageCoords(
                pb, out x2, out y2,
                new SDPoint(screenRect.Right, screenRect.Bottom)
            );

            int left = Math.Min(x1, x2);
            int top = Math.Min(y1, y2);
            int w = Math.Max(1, Math.Abs(x2 - x1));
            int h = Math.Max(1, Math.Abs(y2 - y1));

            return new Rectangle(left, top, w, h);
        }

        private void ApplyCrop()
        {
            if (!cropMode || currentMat == null) return;

            if (cropRectScreen.IsEmpty)
            {
                statusLabel.Text = "Выделите область";
                return;
            }

            var bmp = currentMat.ToBitmap();

            var imgRect = ScreenRectToImageRect(
                cropRectScreen, bmp.Size, pbImage
            );

            if (imgRect.Width <= 0 || imgRect.Height <= 0)
            {
                statusLabel.Text = "Неверная область";
                return;
            }

            var roi = new Rect(imgRect.Left, imgRect.Top, imgRect.Width, imgRect.Height);
            var cropped = new Mat(currentMat, roi).Clone();

            currentMat.Dispose();
            currentMat = cropped;

            cropMode = false;
            cropRectScreen = Rectangle.Empty;

            pbImage.Cursor = Cursors.Default;

            Redraw();
            UpdateUndoRedoButtons();
        }

        private void AddText()
        {
            if (currentMat == null) return;

            using (var dlg = new FormTextEditor("Текст", Color.White, 1.0f, 2))
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                placingTextValue = dlg.TextValue;
                placingTextColor = dlg.TextColor;
                placingTextScale = dlg.FontScale;
                placingTextThickness = dlg.Thickness;

                placingText = true;
                statusLabel.Text = "Разместите текст";
                pbImage.Cursor = Cursors.Cross;

                pbImage.MouseDown += PbImage_TextPlace_MouseDown;
                pbImage.MouseMove += PbImage_TextPlace_MouseMove;
                pbImage.MouseUp += PbImage_TextPlace_MouseUp;
            }
        }

        private void PbImage_TextPlace_MouseDown(object sender, MouseEventArgs e)
        {
            if (!placingText) return;
            placeStart = e.Location;
            placeEnd = e.Location;
        }

        private void PbImage_TextPlace_MouseMove(object sender, MouseEventArgs e)
        {
            if (!placingText || e.Button != MouseButtons.Left) return;

            placeEnd = e.Location;

            var previewBmp = currentMat.ToBitmap();

            using (var g = Graphics.FromImage(previewBmp))
            {
                var rect = GetPlacementRect(
                    placeStart, placeEnd, previewBmp.Size, pbImage
                );

                using (var pen = new Pen(Color.Yellow, 2))
                {
                    g.DrawRectangle(pen, rect);
                }

                int fontSize = Math.Max(
                    8, (int)(12 * placingTextScale * (rect.Width / 200.0))
                );

                using (var font = new Font(
                    "Arial", fontSize, FontStyle.Bold, GraphicsUnit.Pixel
                ))
                using (var brush = new SolidBrush(placingTextColor))
                {
                    g.DrawString(placingTextValue, font, brush, rect.Location);
                }
            }

            if (pbImage.Image != null) pbImage.Image.Dispose();
            pbImage.Image = previewBmp;
        }

        private void PbImage_TextPlace_MouseUp(object sender, MouseEventArgs e)
        {
            if (!placingText) return;

            placeEnd = e.Location;

            PushUndo();

            var bmp = currentMat.ToBitmap();

            var rect = GetPlacementRect(
                placeStart, placeEnd, bmp.Size, pbImage
            );

            using (var g = Graphics.FromImage(bmp))
            {
                g.TextRenderingHint =
                    System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                int fontSize = Math.Max(
                    8, (int)(12 * placingTextScale * (rect.Width / 200.0))
                );

                using (var font = new Font(
                    "Arial", fontSize, FontStyle.Bold, GraphicsUnit.Pixel
                ))
                using (var brush = new SolidBrush(placingTextColor))
                {
                    g.DrawString(placingTextValue, font, brush, rect.Location);
                }
            }

            currentMat.Dispose();
            currentMat = BitmapConverter.ToMat(bmp);

            placingText = false;

            pbImage.MouseDown -= PbImage_TextPlace_MouseDown;
            pbImage.MouseMove -= PbImage_TextPlace_MouseMove;
            pbImage.MouseUp -= PbImage_TextPlace_MouseUp;

            pbImage.Cursor = Cursors.Default;

            Redraw();
            UpdateUndoRedoButtons();
        }

        private Rectangle GetPlacementRect(
            SDPoint a, SDPoint b, SDSize bmpSize, PictureBox pb
        )
        {
            int x1, y1, x2, y2;

            MapToImageCoords(pb, out x1, out y1, a);
            MapToImageCoords(pb, out x2, out y2, b);

            int left = Math.Min(x1, x2);
            int top = Math.Min(y1, y2);
            int w = Math.Max(1, Math.Abs(x2 - x1));
            int h = Math.Max(1, Math.Abs(y2 - y1));

            return new Rectangle(left, top, w, h);
        }

        private void PushUndo()
        {
            if (currentMat == null) return;

            try
            {
                undoStack.Push(currentMat.Clone());

                while (undoStack.Count > MAX_UNDO)
                {
                    var m = undoStack.Pop();
                    m.Dispose();
                }

                foreach (var m in redoStack) m.Dispose();
                redoStack.Clear();
            }
            catch { }

            UpdateUndoRedoButtons();
        }

        private void Undo()
        {
            if (undoStack.Count == 0)
            {
                statusLabel.Text = "Нечего отменять";
                return;
            }

            try
            {
                var prev = undoStack.Pop();

                redoStack.Push(currentMat.Clone());

                currentMat.Dispose();
                currentMat = prev;

                Redraw();
                statusLabel.Text = "Отмена";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Ошибка Undo: " + ex.Message;
            }

            UpdateUndoRedoButtons();
        }

        private void Redo()
        {
            if (redoStack.Count == 0)
            {
                statusLabel.Text = "Нечего вернуть";
                return;
            }

            try
            {
                var next = redoStack.Pop();

                undoStack.Push(currentMat.Clone());

                currentMat.Dispose();
                currentMat = next;

                Redraw();
                statusLabel.Text = "Возврат";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Ошибка Redo: " + ex.Message;
            }

            UpdateUndoRedoButtons();
        }

        private void ClearUndoRedo()
        {
            foreach (var m in undoStack) m.Dispose();
            undoStack.Clear();

            foreach (var m in redoStack) m.Dispose();
            redoStack.Clear();

            UpdateUndoRedoButtons();
        }

        private void UpdateUndoRedoButtons()
        {
            try
            {
                btnUndo.Enabled = undoStack.Count > 0;
                btnRedo.Enabled = redoStack.Count > 0;
            }
            catch { }
        }

        private void StopDrawing()
        {
            drawMode = false;
            isDrawing = false;
            placingText = false;
            pipetteMode = false;
            cropMode = false;

            try { pbImage.Cursor = Cursors.Default; } catch { }

            try
            {
                pbImage.MouseDown -= PbImage_TextPlace_MouseDown;
                pbImage.MouseMove -= PbImage_TextPlace_MouseMove;
                pbImage.MouseUp -= PbImage_TextPlace_MouseUp;
                pbImage.MouseClick -= PbImage_PipetteClickOnce;
            }
            catch { }
        }

        private void MapToImageCoords(PictureBox pb, out int x, out int y, SDPoint p)
        {
            x = 0;
            y = 0;

            if (currentMat == null) return;

            int imgW = currentMat.Width;
            int imgH = currentMat.Height;

            int pbW = pb.ClientSize.Width;
            int pbH = pb.ClientSize.Height;

            float imgRatio = (float)imgW / imgH;
            float boxRatio = (float)pbW / pbH;

            int drawW, drawH;

            if (imgRatio > boxRatio)
            {
                drawW = pbW;
                drawH = (int)(pbW / imgRatio);
            }
            else
            {
                drawH = pbH;
                drawW = (int)(pbH * imgRatio);
            }

            int offsetX = (pbW - drawW) / 2;
            int offsetY = (pbH - drawH) / 2;

            int px = p.X - offsetX;
            int py = p.Y - offsetY;

            if (px < 0) px = 0;
            if (py < 0) py = 0;
            if (px >= drawW) px = drawW - 1;
            if (py >= drawH) py = drawH - 1;

            float scaleX = (float)imgW / drawW;
            float scaleY = (float)imgH / drawH;

            x = (int)(px * scaleX);
            y = (int)(py * scaleY);

            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (x >= imgW) x = imgW - 1;
            if (y >= imgH) y = imgH - 1;
        }

        private void UpdatePictureBoxZoom()
        {
            if (currentMat == null) return;

            try
            {
                var bmp = currentMat.ToBitmap();

                int newW = (int)(bmp.Width * displayZoom);
                int newH = (int)(bmp.Height * displayZoom);

                var scaled = new Bitmap(bmp, new SDSize(newW, newH));

                if (pbImage.Image != null) pbImage.Image.Dispose();

                pbImage.SizeMode = PictureBoxSizeMode.Normal;
                pbImage.Image = scaled;

                bmp.Dispose();
            }
            catch { }
        }

        private void Redraw()
        {
            try
            {
                if (pbImage.Image != null) pbImage.Image.Dispose();

                if (currentMat == null) return;

                if (Math.Abs(displayZoom - 1.0f) > 0.001f)
                {
                    UpdatePictureBoxZoom();
                }
                else
                {
                    pbImage.SizeMode = PictureBoxSizeMode.Zoom;
                    pbImage.Image = currentMat.ToBitmap();
                }
            }
            catch { }
        }

        private void ResizeImage(int newW, int newH)
        {
            if (currentMat == null) return;

            var dst = new Mat();

            Cv2.Resize(
                currentMat, dst,
                new CVSize(Math.Max(1, newW), Math.Max(1, newH)),
                0, 0,
                InterpolationFlags.Lanczos4
            );

            currentMat.Dispose();
            currentMat = dst;
        }

        private Mat Ensure8U(Mat src, int channels)
        {
            if (src == null) return null;

            if (channels == 1)
            {
                if (src.Type() == MatType.CV_8UC1)
                    return src.Clone();

                var dst = new Mat();
                src.ConvertTo(dst, MatType.CV_8UC1);
                return dst;
            }

            if (src.Type() == MatType.CV_8UC3)
                return src.Clone();

            var dst3 = new Mat();
            src.ConvertTo(dst3, MatType.CV_8UC3);
            return dst3;
        }

        private void ColorToHsv(Color color, out double h, out double s, out double v)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            v = max;
            s = max == 0 ? 0 : 255.0 * (max - min) / max;

            if (max == min)
            {
                h = 0;
            }
            else if (max == color.R)
            {
                h = 60 * (color.G - color.B) / (double)(max - min);
            }
            else if (max == color.G)
            {
                h = 60 * (2 + (color.B - color.R) / (double)(max - min));
            }
            else
            {
                h = 60 * (4 + (color.R - color.G) / (double)(max - min));
            }

            if (h < 0) h += 360;
        }

        private void ApplyHSVShift(double hShift, double sDelta, double vDelta)
        {
            if (currentMat == null) return;

            var src = Ensure8U(currentMat, 3);
            var hsv = src.CvtColor(ColorConversionCodes.BGR2HSV);
            var ch = Cv2.Split(hsv);

            Cv2.Add(ch[0], new Scalar(hShift), ch[0]);
            Cv2.Add(ch[1], new Scalar(sDelta), ch[1]);
            Cv2.Add(ch[2], new Scalar(vDelta), ch[2]);

            Cv2.Merge(ch, hsv);

            var res = hsv.CvtColor(ColorConversionCodes.HSV2BGR);

            foreach (var c in ch) c.Dispose();
            hsv.Dispose();
            src.Dispose();

            currentMat.Dispose();
            currentMat = res;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (pbImage.Image != null) pbImage.Image.Dispose();
            if (originalMat != null) originalMat.Dispose();
            if (currentMat != null) currentMat.Dispose();

            ClearUndoRedo();
        }
    }

    internal static class MatExtensions
    {
        public static Mat CvtColor(this Mat src, ColorConversionCodes code)
        {
            var dst = new Mat();
            Cv2.CvtColor(src, dst, code);
            return dst;
        }
    }
}