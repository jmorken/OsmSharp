﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OsmSharp.UI.Rendering.MapCSS.v0_2.Domain
{
    /// <summary>
    /// Strongly typed MapCSS v0.2 Declaration class.
    /// </summary>
    public class DeclarationInt : Declaration<DeclarationIntEnum, int>
    {

    }

    /// <summary>
    /// Strongly typed MapCSS v0.2 Declaration class.
    /// </summary>
    public enum DeclarationIntEnum
    {
        FillColor,
        ZIndex,
        Color,
        CasingWidth,
        Extrude,
        ExtrudeEdgeColor,
        ExtrudeFaceColor,
        IconWidth,
        IconHeight,
        FontSize,
        TextColor,
        TextOffset,
        MaxWidth,
        TextHaloColor,
        TextHaloRadius
    }
}