﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BiomeMap.Drawing.Data;
using BiomeMap.Drawing.Utils;
using log4net;
using Size = BiomeMap.Drawing.Data.Size;

namespace BiomeMap.Drawing
{
    public class TileScaler
    {

        private TileScalerRunner[] _runners;

        public TileScaler(string basePath, Size renderScale, Size tileSize, int minZoom, int maxZoom)
        {
            var size = maxZoom - minZoom + 1;
            _runners = new TileScalerRunner[size];

            var i = 0;
            for (int z = minZoom; z <= maxZoom; z++)
            {
                _runners[i] = new TileScalerRunner(basePath, z, renderScale, tileSize);
                i++;
            }
        }

        public void Enqueue(RegionPosition regionPos, MapRegionLayer region)
        {
            var entry = new TileScaleEntry(regionPos, region);
            foreach (var runner in _runners)
            {
                runner.Enqueue(entry);
            }
        }

        class TileScaleEntry
        {
            public RegionPosition Position { get; }

            private MapRegionLayer _region;

            private Bitmap _bitmap;
            private readonly object _bitmapSync = new object();

            public TileScaleEntry(RegionPosition regionPos, MapRegionLayer region)
            {
                Position = regionPos;
                _region = region;
            }

            public Bitmap GetBitmap()
            {
                if (_bitmap == null) _bitmap = _region.GetBitmap();
                lock (_bitmapSync)
                {
                    return (Bitmap) _bitmap.Clone();
                }
            }
        }

        class TileScalerRunner
        {
            private static readonly ILog Log = LogManager.GetLogger(typeof(TileScalerRunner));

            public const int ExecInterval = 500;

            public int Zoom { get; }

            private int Scale { get; }

            private string BasePath { get; }

            private Size RegionTileSize { get; }

            private Size TileSize { get; }

            private readonly Queue<TileScaleEntry> _updates = new Queue<TileScaleEntry>();

            private readonly List<TileScaleEntry> _processing = new List<TileScaleEntry>();

            private readonly object _queueSync = new object();

            private Timer _timer;
            private readonly object _taskSync = new object();

            public TileScalerRunner(string basePath, int zoom, Size renderScale, Size tileSize)
            {
                BasePath = basePath;
                Zoom = zoom;
                Scale = 1 << Zoom;
                RegionTileSize = new Size((renderScale.Width * (1 << 9)) / Scale, (renderScale.Height * (1 << 9)) / Scale);
                TileSize = tileSize;

                _timer = new Timer(TimerCallback, null, ExecInterval, ExecInterval);
            }

            public void Enqueue(TileScaleEntry entry)
            {
                lock (_queueSync)
                {
                    if (_updates.Contains(entry)) return;
                    _updates.Enqueue(entry);
                }
            }

            private void TimerCallback(object state)
            {
                if (!Monitor.TryEnter(_taskSync))
                {
                    return;
                }

                try
                {
                    DoTask();
                }
                finally
                {
                    Monitor.Exit(_taskSync);
                }
            }

            private void DoTask()
            {
                while (_updates.Count > 0)
                {
                    TileScaleEntry entry;

                    lock (_queueSync)
                    {
                        entry = _updates.Peek();

                        if (_processing.Contains(entry))
                        {
                            _updates.Enqueue(_updates.Dequeue());
                            if(_updates.Peek() == entry)
                                return;
                        }

                        _updates.Dequeue();
                        _processing.Add(entry);
                    }

                    ThreadPool.QueueUserWorkItem((o) =>
                    {
                        ScaleRegion(entry);

                        lock (_queueSync)
                        {
                            _processing.Remove(entry);
                        }
                    });
                }
            }

            private void ScaleRegion(TileScaleEntry entry)
            {
                var sw = Stopwatch.StartNew();
                var regionPos = entry.Position;

                var baseImg = entry.GetBitmap();
                var format = baseImg.PixelFormat;
                var sync = new object();

                //Parallel.For(0, Scale, tX =>
                for(int tX=0;tX<Scale;tX++)
                {
                    //Parallel.For(0, Scale, tZ =>
                    for (int tZ = 0; tZ < Scale; tZ++)
                    {
                        Bitmap img;
                        lock (sync)
                        {
                            img = baseImg.Clone(new Rectangle(tX * RegionTileSize.Width, tZ * RegionTileSize.Height, RegionTileSize.Width, RegionTileSize.Height), format);
                        }

                        DrawTile(regionPos, img, tX, tZ);
                    }//);
                }//);

                baseImg.Dispose();

                Log.InfoFormat("Scaled Region {0} to Zoom Level {1} in {2}ms", regionPos, Zoom, sw.ElapsedMilliseconds);
            }

            private void DrawTile(RegionPosition regionPos, Bitmap baseBitmap, int tX, int tZ)
            {
                using (baseBitmap)
                {
                    using (var tileImg = new Bitmap(TileSize.Width, TileSize.Height))
                    {
                        using (var g = tileImg.GetGraphics())
                        {
                            g.DrawImage(baseBitmap, new Rectangle(0, 0, TileSize.Width, TileSize.Height));
                        }

                        var tilePath = Path.Combine(BasePath, Zoom.ToString(),
                            ((regionPos.X << Zoom) + tX) + "_" + ((regionPos.Z << Zoom) + tZ) + ".png");
                        tileImg.SaveToFile(tilePath);
                    }
                }
            }
        }
    }
}
