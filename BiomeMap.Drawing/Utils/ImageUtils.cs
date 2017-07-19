﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiomeMap.Drawing.Utils
{
    public static class ImageUtils
    {

        public static void SaveToFile(this Bitmap bitmap, string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                using (var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    byte[] bytes = ms.ToArray();
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
        }

        public static Bitmap Tint(this Bitmap bitmap, Color tintColor)
        {
            var b = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var tmp = bitmap.Clone(b, bitmap.PixelFormat);

            for (int x = 0; x < tmp.Width; x++)
            {
                for (int y = 0; y < tmp.Height; y++)
                {
                    tmp.SetPixel(x, y, tmp.GetPixel(x, y).Tint(tintColor));
                }
            }

            return tmp;
        }

        public static Color Tint(this Color source, Color tint)
        {
            //(tint -source)*alpha + source
            var alpha = tint.A / 255d;

            var red = Convert.ToInt32(((tint.R - source.R) * alpha + source.R));
            var blue = Convert.ToInt32(((tint.B - source.B) * alpha + source.B));
            var green = Convert.ToInt32(((tint.G - source.G) * alpha + source.G));
            return Color.FromArgb(255, red, green, blue);
        }

        public static Graphics GetGraphics(this Bitmap bitmap)
        {
            var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.PageUnit = GraphicsUnit.Pixel;
            return g;
        }

    }
}
