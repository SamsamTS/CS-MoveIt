using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

namespace MoveIt
{
    internal class OverlayColorsFactory
    {
        internal static OverlayColors Create()
        {
            return new OverlayColors();
        }
    }

    [Serializable]
    internal class OverlayColors
    {
        private static bool _showSelectors = true;
        internal static bool ShowSelectors { get => _showSelectors; set => _showSelectors = value; }

        [XmlAnyElement]
        internal Color Hover { get => _hover; set => _hover = value; }
        private Color _hover = new Color32(0, 181, 255, 250);
        [XmlAnyElement]
        internal Color Selected { get => _selected; set => _selected = value; }
        private Color _selected = new Color32(95, 166, 0, 244);
        [XmlAnyElement]
        internal Color NodeMerge { get => _nodeMerge; set => _nodeMerge = value; }
        private Color _nodeMerge = new Color32(20, 80, 180, 220);
        [XmlAnyElement]
        internal Color NodeSnap { get => _nodeSnap; set => _nodeMerge = value; }
        private Color _nodeSnap = new Color32(30, 90, 190, 250);
        [XmlAnyElement]
        internal Color Move { get => _move; set => _move = value; }
        private Color _move = new Color32(125, 196, 30, 244);
        [XmlAnyElement]
        internal Color Remove { get => _remove; set => _remove = value; }
        private Color _remove = new Color32(255, 160, 47, 191);
        [XmlAnyElement]
        internal Color Despawn { get => _despawn; set => _despawn = value; }
        private Color _despawn = new Color32(255, 160, 47, 191);
        [XmlAnyElement]
        internal Color Align { get => _align; set => _align = value; }
        private Color _align = new Color32(255, 255, 255, 244);
        [XmlAnyElement]
        internal Color POHover { get => _POhover; set => _POhover = value; }
        private Color _POhover = new Color32(240, 140, 255, 230);
        [XmlAnyElement]
        internal Color POSelected { get => _POselected; set => _POselected = value; }
        private Color _POselected = new Color32(225, 130, 240, 125);
        [XmlAnyElement]
        internal Color POhoverGroup { get => _POhoverGroup; set => _POhoverGroup = value; }
        private Color _POhoverGroup = new Color32(255, 45, 45, 230);
        [XmlAnyElement]
        internal Color POselectedGroup { get => _POselectedGroup; set => _POselectedGroup = value; }
        private Color _POselectedGroup = new Color32(240, 30, 30, 150);

        internal static Color GetAdjusted(Color c)
        {
            if (!ShowSelectors)
            {
                c.a = 0f;
            }
            return c;
        }
    }

    internal static class ColorExtension
    {
        internal static Color Adjusted(this Color c)
        {
            return OverlayColors.GetAdjusted(c);
        }
    }
}
