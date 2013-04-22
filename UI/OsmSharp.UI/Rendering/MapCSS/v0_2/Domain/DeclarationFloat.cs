﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OsmSharp.UI.Rendering.MapCSS.v0_2.Domain
{
    /// <summary>
    /// Strongly typed MapCSS v0.2 Declaration class.
    /// </summary>
    public class DeclarationFloat : Declaration<DeclarationFloatEnum, float>
    {

    }

    /// <summary>
    /// Strongly typed MapCSS v0.2 Declaration class.
    /// </summary>
    public enum DeclarationFloatEnum
    {
        Width,
        FillOpacity,
        Opacity,
        FillColor,
        CasingOpacity,
        ExtrudeEdgeOpacity,
        ExtrudeFaceOpacity,
        ExtrudeEdgeWidth,
        IconOpacity,
        TextOpacity
    }
}