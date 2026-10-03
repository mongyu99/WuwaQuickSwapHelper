using System.Windows;
using System.Windows.Media;

namespace WuwaQuickSwapHelper.Services;

// 라이트 / 다크 테마. App.xaml의 색상 리소스를 바꿔 끼우면 DynamicResource로 연결된 화면이 바로 바뀝니다.
public static class ThemeService
{
    public const string Dark = "dark";
    public const string Light = "light";

    private static readonly Dictionary<string, Dictionary<string, string>> Palettes = new()
    {
        [Dark] = new()
        {
            ["WindowBgBrush"] = "#F21B1B1D",
            ["LineBrush"] = "#3A3A3E",
            ["SurfaceBrush"] = "#0DFFFFFF",
            ["CardBrush"] = "#14FFFFFF",
            ["ButtonBrush"] = "#2C2C30",
            ["ButtonHoverBrush"] = "#3A3A3F",
            ["AccentBrush"] = "#E2B144",
            ["OnAccentBrush"] = "#1B1B1D",
            ["TextBrush"] = "#ECECEC",
            ["SubTextBrush"] = "#9A9A9F",
            ["InputBrush"] = "#232326",
            ["InputBorderBrush"] = "#3A3A3E",
            ["StepBrush"] = "#323236",
            ["CurrentLineBgBrush"] = "#2648A9C9",
            ["CurrentLineBorderBrush"] = "#5FB3D3",
        },
        [Light] = new()
        {
            ["WindowBgBrush"] = "#F7F6F4F1",
            ["LineBrush"] = "#DAD8D3",
            ["SurfaceBrush"] = "#0A000000",
            ["CardBrush"] = "#0F000000",
            ["ButtonBrush"] = "#E7E5E0",
            ["ButtonHoverBrush"] = "#DAD8D2",
            ["AccentBrush"] = "#C78F1E",
            ["OnAccentBrush"] = "#FFFFFF",
            ["TextBrush"] = "#1F1F21",
            ["SubTextBrush"] = "#6E6D6A",
            ["InputBrush"] = "#FFFFFF",
            ["InputBorderBrush"] = "#D3D1CC",
            ["StepBrush"] = "#E4E2DD",
            ["CurrentLineBgBrush"] = "#2648A9C9",
            ["CurrentLineBorderBrush"] = "#3E95B8",
        },
    };

    public static string Current { get; private set; } = Dark;

    public static void Apply(string theme)
    {
        if (!Palettes.TryGetValue(theme, out var palette))
        {
            theme = Dark;
            palette = Palettes[Dark];
        }

        Current = theme;

        var resources = Application.Current.Resources;

        foreach (var (key, hex) in palette)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            resources[key] = brush;
        }
    }

    public static Brush Get(string key) => (Brush)Application.Current.Resources[key];
}
