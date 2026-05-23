using System.Collections.Generic;
using System.Windows.Media;

namespace CrystalFolders.Themes
{
    public static class Colors
    {
        private static SolidColorBrush GetBrush(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

        public static Dictionary<string, Brush> GetDarkColors()
        {
            return new Dictionary<string, Brush>
            {
                { "AppBackground", GetBrush("#111827") },
                { "ListBackground", GetBrush("#1F2937") },
                { "ItemBackground", GetBrush("#374151") },
                { "BorderColor", GetBrush("#4B5563") },
                { "AccentColor", GetBrush("#0EA5E9") },
                { "TextPrimary", GetBrush("#F9FAFB") },
                { "TextSecondary", GetBrush("#9CA3AF") },
                { "EmptyIconBg", GetBrush("#1F2937") }
            };
        }

        public static Dictionary<string, Brush> GetLightColors()
        {
            return new Dictionary<string, Brush>
            {
                { "AppBackground", GetBrush("#F3F4F6") },
                { "ListBackground", GetBrush("#FFFFFF") },
                { "ItemBackground", GetBrush("#F9FAFB") },
                { "BorderColor", GetBrush("#E5E7EB") },
                { "AccentColor", GetBrush("#0284C7") },
                { "TextPrimary", GetBrush("#111827") },
                { "TextSecondary", GetBrush("#6B7280") },
                { "EmptyIconBg", GetBrush("#F3F4F6") }
            };
        }
    }
}