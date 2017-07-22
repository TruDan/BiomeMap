﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BiomeMap.Drawing.Data;

namespace BiomeMap.Drawing.Renderers
{
    public interface IPostProcessor
    {

        void PostProcess(MapRegionLayer layer, Graphics graphics, BlockColumnMeta block);

    }
}
