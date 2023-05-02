using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Windows;

namespace PhotoGenerator.Models.Classes
{
    public static class StaticData
    {

        /// <summary>
        /// Нахождение среднего цвета изображения
        /// </summary>
        /// <param name="Bitmap"></param>
        /// <returns></returns>
        public static Color MeanColor(Bitmap Bitmap)
        {
            int r = 0, g = 0, b = 0;
            int global_r = 0, global_g = 0, global_b = 0;
            int c = 1, j = 1;
            for (c = 0; c < Bitmap.Width; c++)
            {
                for (j = 0; j < Bitmap.Height; j++)
                {
                    r += Bitmap.GetPixel(c, j).R;
                    g += Bitmap.GetPixel(c, j).G;
                    b += Bitmap.GetPixel(c, j).B;
                }
                global_r += r; r = 0;
                global_g += g; g = 0;
                global_b += b; b = 0;
            }

            int countPixels = Math.Max(c, 1) * Math.Max(j, 1);
            global_r /= countPixels;
            global_g /= countPixels;
            global_b /= countPixels;

            Bitmap.Dispose();
            GC.Collect();
            return Color.FromArgb(global_r, global_g, global_b);
        }

        public static (float, float, float) RGBToLab(Color color)
        {
            float[] xyz = new float[3];
            float[] lab = new float[3];
            float[] rgb = new float[] { color.R, color.G, color.B };

            rgb[0] = color.R / 255.0f;
            rgb[1] = color.G / 255.0f;
            rgb[2] = color.B / 255.0f;

            if (rgb[0] > .04045f)
            {
                rgb[0] = (float)Math.Pow((rgb[0] + .055) / 1.055, 2.4);
            }
            else
            {
                rgb[0] = rgb[0] / 12.92f;
            }

            if (rgb[1] > .04045f)
            {
                rgb[1] = (float)Math.Pow((rgb[1] + .055) / 1.055, 2.4);
            }
            else
            {
                rgb[1] = rgb[1] / 12.92f;
            }

            if (rgb[2] > .04045f)
            {
                rgb[2] = (float)Math.Pow((rgb[2] + .055) / 1.055, 2.4);
            }
            else
            {
                rgb[2] = rgb[2] / 12.92f;
            }
            rgb[0] = rgb[0] * 100.0f;
            rgb[1] = rgb[1] * 100.0f;
            rgb[2] = rgb[2] * 100.0f;

            xyz[0] = ((rgb[0] * .412453f) + (rgb[1] * .357580f) + (rgb[2] * .180423f));
            xyz[1] = ((rgb[0] * .212671f) + (rgb[1] * .715160f) + (rgb[2] * .072169f));
            xyz[2] = ((rgb[0] * .019334f) + (rgb[1] * .119193f) + (rgb[2] * .950227f));

            xyz[0] = xyz[0] / 95.047f;
            xyz[1] = xyz[1] / 100.0f;
            xyz[2] = xyz[2] / 108.883f;

            if (xyz[0] > .008856f)
            {
                xyz[0] = (float)Math.Pow(xyz[0], .333f);    // (1.0 / 3.0));
            }
            else
            {
                xyz[0] = (xyz[0] * 7.787f) + .138f; // (16.0f / 116.0f);
            }

            if (xyz[1] > .008856f)
            {
                xyz[1] = (float)Math.Pow(xyz[1], .333f);    // 1.0 / 3.0);
            }
            else
            {
                xyz[1] = (xyz[1] * 7.787f) + .138f; // (16.0f / 116.0f);
            }

            if (xyz[2] > .008856f)
            {
                xyz[2] = (float)Math.Pow(xyz[2], .333f);    // 1.0 / 3.0);
            }
            else
            {
                xyz[2] = (xyz[2] * 7.787f) + .138f; // (16.0f / 116.0f);
            }

            lab[0] = (116.0f * xyz[1]) - 16.0f;
            lab[1] = 500.0f * (xyz[0] - xyz[1]);
            lab[2] = 200.0f * (xyz[1] - xyz[2]);

            return (lab[0], lab[1], lab[2]);
        }

        public static (float, float, float) LabToXYZ((float, float, float) color)
        {
            float[] xyz = new float[3];
            float[] col = new float[] { color.Item1, color.Item2, color.Item3 };

            xyz[1] = (col[0] + 16.0f) / 116.0f;
            xyz[0] = (col[1] / 500.0f) + xyz[1];
            xyz[2] = xyz[1] - (col[2] / 200.0f);

            for (int i = 0; i < 3; i++)
            {
                float pow = xyz[i] * xyz[i] * xyz[i];
                float ratio = (6.0f / 29.0f);
                if (xyz[i] > ratio)
                {
                    xyz[i] = pow;
                }
                else
                {
                    xyz[i] = (3.0f * (6.0f / 29.0f) * (6.0f / 29.0f) * (xyz[i] - (4.0f / 29.0f)));
                }
            }
            xyz[0] = xyz[0] * 95.047f;
            xyz[1] = xyz[1] * 100.0f;
            xyz[2] = xyz[2] * 108.883f;

            return (xyz[0], xyz[1], xyz[2]);
        }

        public static (float, float, float) XYZToRGB((float, float, float) color)
        {
            float[] rgb = new float[3];
            float[] xyz = new float[3];
            float[] col = new float[] { color.Item1, color.Item2, color.Item3 };



            for (int i = 0; i < 3; i++)
            {
                xyz[i] = col[i] / 100.0f;
            }

            rgb[0] = (xyz[0] * 3.240479f) + (xyz[1] * -1.537150f) + (xyz[2] * -.498535f);
            rgb[1] = (xyz[0] * -.969256f) + (xyz[1] * 1.875992f) + (xyz[2] * .041556f);
            rgb[2] = (xyz[0] * .055648f) + (xyz[1] * -.204043f) + (xyz[2] * 1.057311f);

            for (int i = 0; i < 3; i++)
            {
                if (rgb[i] > .0031308f)
                {
                    rgb[i] = (1.055f * (float)Math.Pow(rgb[i], (1.0f / 2.4f))) - .055f;
                }
                else
                {
                    rgb[i] = rgb[i] * 12.92f;
                }
            }

            rgb[0] = rgb[0] * 255.0f;
            rgb[1] = rgb[1] * 255.0f;
            rgb[2] = rgb[2] * 255.0f;

            return (rgb[0], rgb[1], rgb[2]);
        }

        public static Color LabToRGB((float, float, float) color)
        {
            var xyz = LabToXYZ(color);
            var rgb = XYZToRGB(xyz);

            return Color.FromArgb((int)rgb.Item1, (int)rgb.Item2, (int)rgb.Item3);
        }

        public static float GetDistance((float, float, float) lab1, (float, float, float) lab2)
        {
            var deltaL = lab1.Item1 - lab2.Item1;
            var deltaA = lab1.Item2 - lab2.Item2;
            var deltaB = lab1.Item3 - lab2.Item3;

            var finalResult = (float)Math.Sqrt(deltaL * deltaL + deltaA * deltaA + deltaB * deltaB);

            return finalResult;
        }
    }
}
