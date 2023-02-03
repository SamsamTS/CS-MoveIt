using ColossalFramework;
using ColossalFramework.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using System.Xml;

namespace MoveIt
{
    internal class OverlayColorsFactory
    {
        internal static OverlayColors Create()
        {
            OverlayColors result = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(OverlayColors));

                using (var reader = new StreamReader(GetFileName()))
                {
                    result = (OverlayColors)serializer.Deserialize(reader);
                }
                Log.Debug($"AAA01 Hover:{result.Hover} Selected:{result.Selected}");
            }
            catch
            {
                result = new OverlayColors();
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(OverlayColors));

                    using (var writer = new StreamWriter(GetFileName()))
                    {
                        serializer.Serialize(writer, result);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to create file \"{GetFileName()}\"\n{e}");
                }
            }

            return result;
        }

        private static string GetFileName()
        {
            return Path.Combine(DataLocation.localApplicationData, "MoveItColors.xml");
        }
    }

    [Serializable, XmlRoot("OverlayColors")]
    public class OverlayColors
    {
        private static bool _showSelectors = true;
        public static bool ShowSelectors { get => _showSelectors; set => _showSelectors = value; }

        [XmlElement]
        public string Description = "Move It overlay colours. Missing overlays will use default colour. Missing sub-elements (r/g/b/a) will be zero. Modify at your own risk, delete file to reset.";

        [XmlElement]
        public Color Hover { get => _hover; set { Log.Debug($"AAA02 Hover:{_hover}->{value}"); _hover = value; } } // => _hover = value; }
        private Color _hover = new Color32(0, 181, 255, 250);
        [XmlElement]
        public Color Selected { get => _selected; set { Log.Debug($"AAA03 Selected:{_selected}->{value}"); _selected = value; } } // => _selected = value; }
        private Color _selected = new Color32(95, 166, 0, 244);
        [XmlElement]
        public Color NodeMerge { get => _nodeMerge; set => _nodeMerge = value; }
        private Color _nodeMerge = new Color32(20, 80, 180, 220);
        [XmlElement]
        public Color NodeSnap { get => _nodeSnap; set => _nodeMerge = value; }
        private Color _nodeSnap = new Color32(30, 90, 190, 250);
        [XmlElement]
        public Color Move { get => _move; set => _move = value; }
        private Color _move = new Color32(125, 196, 30, 244);
        [XmlElement]
        public Color Remove { get => _remove; set => _remove = value; }
        private Color _remove = new Color32(255, 160, 47, 191);
        [XmlElement]
        public Color Despawn { get => _despawn; set => _despawn = value; }
        private Color _despawn = new Color32(255, 160, 47, 191);
        [XmlElement]
        public Color Align { get => _align; set => _align = value; }
        private Color _align = new Color32(255, 255, 255, 244);
        [XmlElement]
        public Color POHover { get => _POhover; set => _POhover = value; }
        private Color _POhover = new Color32(240, 140, 255, 230);
        [XmlElement]
        public Color POSelected { get => _POselected; set => _POselected = value; }
        private Color _POselected = new Color32(225, 130, 240, 125);
        [XmlElement]
        public Color POhoverGroup { get => _POhoverGroup; set => _POhoverGroup = value; }
        private Color _POhoverGroup = new Color32(255, 45, 45, 230);
        [XmlElement]
        public Color POselectedGroup { get => _POselectedGroup; set => _POselectedGroup = value; }
        private Color _POselectedGroup = new Color32(240, 30, 30, 150);

        public static Color GetAdjusted(Color c)
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
