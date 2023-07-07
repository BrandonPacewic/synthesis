using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Synthesis.Util;
using UnityEngine;

namespace Utilities.ColorManager {
    public static class ColorManager
    {
        private static readonly Color32 UNASSIGNED_COLOR = new Color32(200, 255, 0, 255);
        
        private static readonly string PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                              Path.AltDirectorySeparatorChar + "Autodesk" +
                                              Path.AltDirectorySeparatorChar + "Synthesis";

        private static readonly (SynthesisColor, Color)[] _defaultColors =
        {
            (SynthesisColor.SynthesisOrange, new Color32(250, 162, 27, 255)),
            (SynthesisColor.SynthesisOrangeAccent, new Color32(204, 124, 0, 255)),
            (SynthesisColor.SynthesisBlack, new Color32(33, 37, 41, 255)),
            (SynthesisColor.SynthesisBlackAccent, new Color32(52, 58, 64, 255)),
            (SynthesisColor.SynthesisWhite, new Color32(248, 249, 250, 255)),
            (SynthesisColor.SynthesisWhiteAccent, new Color32(213, 216, 223, 255)),
            (SynthesisColor.SynthesisAccept, new Color32(34, 139, 230, 255)),
            (SynthesisColor.SynthesisCancel, new Color32(250, 82, 82, 255)),
            (SynthesisColor.SynthesisOrangeContrastText, new Color32(0, 0, 0, 255)),
            (SynthesisColor.SynthesisIcon, new Color32(255, 255, 255, 255)),
            (SynthesisColor.SynthesisIconAlt, new Color32(0, 0, 0, 255)),
            (SynthesisColor.SynthesisHighlightHover, new Color32(89, 255, 133, 255)),
            (SynthesisColor.SynthesisHighlightSelect, new Color32(255, 89, 133, 255))
        };

        private static Dictionary<SynthesisColor, Color32> _loadedColors = new();


        static ColorManager()
        {
            LoadTheme("test_theme");
        }

        private static void LoadTheme(string themeName)
        {
            string themePath = PATH + Path.AltDirectorySeparatorChar + themeName + ".json";
            
            var dir = Path.GetFullPath(themePath).Replace(Path.GetFileName(themePath), "");
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
                return;
            } else if (!File.Exists(themePath)) {
                return;
            }

            var jsonColors = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(PATH));

            jsonColors.ForEach(x => { 
                _loadedColors.Add(Enum.Parse<SynthesisColor>(x.Key), x.Value.ColorToHex()); 
            });
        }

        public enum SynthesisColor
        {
            SynthesisOrange,
            SynthesisOrangeAccent,
            SynthesisBlack,
            SynthesisBlackAccent,
            SynthesisWhite,
            SynthesisWhiteAccent,
            SynthesisAccept,
            SynthesisCancel,
            SynthesisOrangeContrastText,
            SynthesisIcon,
            SynthesisIconAlt,
            SynthesisHighlightHover,
            SynthesisHighlightSelect
        }
    }
}