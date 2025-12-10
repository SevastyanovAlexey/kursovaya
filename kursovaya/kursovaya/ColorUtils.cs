using System.Drawing;
using OpenCvSharp;

namespace ImageProcessingApp
{
    public static class ColorUtils
    {
        // Возвращает HSV компоненты (H:0..179, S:0..255, V:0..255) для System.Drawing.Color
        public static System.Tuple<int, int, int> ToHsv(Color c)
        {
            Mat bgr = null;
            Mat hsv = null;

            try
            {
                bgr = new Mat(1, 1, MatType.CV_8UC3, new Scalar(c.B, c.G, c.R));
                hsv = bgr.CvtColor(ColorConversionCodes.BGR2HSV);

                Vec3b v = hsv.At<Vec3b>(0, 0);
                return System.Tuple.Create((int)v[0], (int)v[1], (int)v[2]);
            }
            finally
            {
                if (bgr != null) bgr.Dispose();
                if (hsv != null) hsv.Dispose();
            }
        }

        // Сдвигает канал H (0..179) на shift (может быть отрицательным).
        // Возвращает новый Mat с результатом (тип тот же, что и hueChannel).
        public static Mat ShiftHue(Mat hueChannel, int shift)
        {
            Mat result = null;
            Mat add = null;

            try
            {
                result = hueChannel.Clone();
                add = new Mat(hueChannel.Rows, hueChannel.Cols, hueChannel.Type(), new Scalar(shift));
                Cv2.Add(result, add, result);

                // Простейшая нормализация: значения >179 обрезаем до 179.
                // Для корректного циклического сдвига можно применить модульную арифметику,
                // но этот вариант прост и безопасен.
                Cv2.Threshold(result, result, 179, 179, ThresholdTypes.Trunc);

                return result;
            }
            finally
            {
                if (add != null) add.Dispose();
            }
        }
    }
}
