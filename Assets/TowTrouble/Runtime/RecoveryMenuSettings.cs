using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public static class RecoveryMenuSettings
    {
        public static readonly string[] BindingFields = { "forwardKey", "reverseKey", "leftKey", "rightKey", "brakeKey", "hookKey", "reelKey", "extendKey", "resetKey", "boardKey", "garageKey" };
        public static readonly string[] BindingLabels = { "Accelerate", "Reverse", "Steer left", "Steer right", "Brake", "Attach / detach cable", "Reel in", "Pay out", "Reset vehicle", "Recoveries", "Garage" };
        public static string BindingError(RecoveryProfile profile, string field, Key key)
        {
            if (!BindingFields.Contains(field)) return "Unknown control";
            if (key == Key.None || key == Key.Escape || key == Key.Enter || key == Key.NumpadEnter || key == Key.C || key == Key.UpArrow || key == Key.DownArrow || key == Key.LeftArrow || key == Key.RightArrow)
                return "This key is reserved for menus or the camera";
            for (int i = 0; i < BindingFields.Length; i++)
                if (BindingFields[i] != field && string.Equals((string)typeof(RecoveryProfile).GetField(BindingFields[i]).GetValue(profile), key.ToString(), StringComparison.OrdinalIgnoreCase))
                    return "Already used for " + BindingLabels[i].ToLowerInvariant();
            return null;
        }
        public static RecoveryProfile WithoutProgress(RecoveryProfile source)
        {
            var result = new RecoveryProfile();
            CopyPreferences(source, result);
            return result;
        }
        public static void CopyPreferences(RecoveryProfile source, RecoveryProfile result)
        {
            foreach (string name in BindingFields.Concat(new[] { "name", "master", "effects", "ui", "brightness", "textScale", "cameraSensitivity", "quality", "reducedMotion", "invertCamera", "vibration", "tutorial", "colorHelp", "controllerFamily", "displayWidth", "displayHeight", "displayMode" }))
            { var field = typeof(RecoveryProfile).GetField(name); field.SetValue(result, field.GetValue(source)); }
            result.language = "en";
        }
        public static Vector2Int[] Resolutions(Resolution[] values, int currentWidth, int currentHeight)
            => values.Select(r => new Vector2Int(r.width, r.height)).Append(new Vector2Int(currentWidth, currentHeight))
                .Where(v => v.x >= 800 && v.y >= 600).Distinct().OrderBy(v => v.x).ThenBy(v => v.y).ToArray();
    }
}
