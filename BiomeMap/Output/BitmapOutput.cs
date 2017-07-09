﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BiomeMap.Data;
using log4net;

namespace BiomeMap.Output
{
    public class BitmapOutput : IRenderOutput
    {
        public const int TileSize = 256;
        public const int MaxZoom = 9;

        private static readonly ILog Log = LogManager.GetLogger(typeof(BitmapOutput));

        public string OutputPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "RenderMaps");

        public bool ReadOnly { get; set; } = false;

        public BitmapOutput()
        {
            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }
        }

        public void WriteChunk(ChunkData data)
        {
            using (var chunkImg = GetChunkBitmap(data))
            {
                for (int zoom = 0; zoom <= MaxZoom; zoom++)
                {
                    DrawZoomLevel(chunkImg, data.X, data.Z, zoom);
                }
            }

        }

        private void DrawZoomLevel(Bitmap chunkImg, int chunkX, int chunkZ, int zoomLevel)
        {
            var rX = (chunkX >> zoomLevel);
            var rZ = (chunkZ >> zoomLevel);

            var path = Path.Combine(OutputPath, zoomLevel.ToString(), rX + "_" + rZ + ".png");

            var dir = Path.GetDirectoryName(path);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            Image img;

            try
            {
                using (var r = Image.FromFile(path))
                {
                    img = new Bitmap(r);
                }
            }
            catch (FileNotFoundException ex)
            {
                img = new Bitmap(256, 256);

                using (var g = Graphics.FromImage(img))
                {
                    g.Clear(Color.Black);
                }
            }

            using (img)
            {
                using (var g = Graphics.FromImage(img))
                {
                    g.Clear(Color.Black);
                    var size = (TileSize >> zoomLevel);

                    var x = (chunkX - (rX << zoomLevel)) * size;
                    var z = (chunkZ - (rZ << zoomLevel)) * size;

                    var rect = new Rectangle(
                        x < 0 ? TileSize - x - size : x,
                        z < 0 ? TileSize - z - size : z,
                        size,
                        size
                    );
                    Log.InfoFormat("Chunk@{8} {0},{1} => {2}_{3}.png @ ({4},{5}->{6},{7})",
                        chunkX,
                        chunkZ,
                        rX,
                        rZ,
                        rect.X,
                        rect.Y,
                        rect.Width,
                        rect.Height,
                        zoomLevel
                    );
                    g.DrawImage(chunkImg, rect);
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                img.Save(path);
            }
        }

        private Bitmap GetChunkBitmap(ChunkData data)
        {
            var img = new Bitmap(16, 16);


            using (var g = Graphics.FromImage(img))
            {
                g.Clear(Color.Black);
                for (int x = 0; x < 16; x++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        g.FillRectangle(new SolidBrush(data.BlockColors[x * 16 + z]), x, z, 1, 1);
                    }
                }

                //g.DrawString(data.X + "," + data.Z, SystemFonts.SmallCaptionFont, Brushes.White, 1, 1);
            }

            return img;
        }
    }
}
