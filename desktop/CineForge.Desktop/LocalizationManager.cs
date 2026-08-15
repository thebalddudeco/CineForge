using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace CineForge.Desktop;

internal static class LocalizationManager
{
    internal static readonly string[] SupportedLanguages = ["en", "ko", "ja"];
    internal static string CurrentLanguage { get; private set; } = "en";
    private static bool _languageMetadataInitialized;

    private static string SettingsRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CineForge");
    private static string PreferencesPath => Path.Combine(SettingsRoot, "preferences.json");
    private static string InstallPointerPath => Path.Combine(SettingsRoot, "install.json");

    internal static string DetectLanguage()
    {
        foreach (var path in new[] { PreferencesPath, InstallPointerPath })
        {
            try
            {
                if (!File.Exists(path)) continue;
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                if (document.RootElement.TryGetProperty("language", out var value))
                {
                    var language = Normalize(value.GetString());
                    if (SupportedLanguages.Contains(language)) return language;
                }
            }
            catch { /* A malformed preference must never prevent CineForge from launching. */ }
        }
        return Normalize(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }

    internal static void Apply(string requestedLanguage, bool persist)
    {
        var language = Normalize(requestedLanguage);
        CurrentLanguage = language;
        var app = Application.Current;

        var previous = app.Resources.MergedDictionaries.FirstOrDefault(dictionary =>
            dictionary.Source?.OriginalString.Contains("Localization/Strings.", StringComparison.OrdinalIgnoreCase) == true);
        if (previous is not null) app.Resources.MergedDictionaries.Remove(previous);
        app.Resources.MergedDictionaries.Insert(0, new ResourceDictionary
        {
            Source = new Uri($"Localization/Strings.{language}.xaml", UriKind.Relative)
        });

        var fontRoot = "pack://application:,,,/Assets/Fonts/";
        var fonts = language switch
        {
            "ko" => ("Gugi", "Orbit", "Orbit", "IBM Plex Sans KR"),
            "ja" => ("M PLUS 1", "Zen Kurenaido", "Zen Kurenaido", "Zen Kaku Gothic Antique"),
            _ => ("Anta", "Saira Condensed", "Cutive Mono", "Inter Tight")
        };
        app.Resources["TitleFont"] = new FontFamily(fontRoot + "#" + fonts.Item1);
        app.Resources["OperationalFont"] = new FontFamily(fontRoot + "#" + fonts.Item2);
        app.Resources["MicroFont"] = new FontFamily(fontRoot + "#" + fonts.Item3);
        app.Resources["BodyFont"] = new FontFamily(fontRoot + "#" + fonts.Item4);

        var culture = CultureInfo.GetCultureInfo(language switch { "ko" => "ko-KR", "ja" => "ja-JP", _ => "en-US" });
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        if (!_languageMetadataInitialized)
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
            _languageMetadataInitialized = true;
        }

        if (!persist) return;
        Directory.CreateDirectory(SettingsRoot);
        File.WriteAllText(PreferencesPath, JsonSerializer.Serialize(new { language }, new JsonSerializerOptions { WriteIndented = true }));
    }

    internal static string Text(string key) => Application.Current.TryFindResource(key) as string ?? key;

    private static string Normalize(string? language)
    {
        var shortName = (language ?? "en").Trim().ToLowerInvariant().Split('-', '_')[0];
        return SupportedLanguages.Contains(shortName) ? shortName : "en";
    }
}
