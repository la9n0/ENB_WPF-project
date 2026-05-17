using System.Windows;

namespace ENB_project
{
    /// <summary>
    /// Глобальные утилиты приложения: применение темы и языка через ResourceDictionary.
    /// Уведомляет подписчиков об изменениях через события ThemeChanged и LanguageChanged.
    /// </summary>
    public static class EnbFunctional
    {
        public static event Action<string>? ThemeChanged;
        public static event Action<string>? LanguageChanged;
        
        public static void ApplyTheme(string themeName)
        {
            var app = Application.Current;

            for (int i = app.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                var src = app.Resources.MergedDictionaries[i].Source?.OriginalString;
                if (src != null && (src.Contains("LightTheme.xaml") || src.Contains("DarkTheme.xaml")))
                    app.Resources.MergedDictionaries.RemoveAt(i);
            }

            string path = themeName == "Light"
                ? "Resources/Themes/LightTheme.xaml"
                : "Resources/Themes/DarkTheme.xaml";

            app.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(path, UriKind.Relative)
            });

            ThemeChanged?.Invoke(themeName);
        }

        /// <summary>
        /// Заменяет активный словарь языка на ru или en.
        /// Удаляет предыдущий перед добавлением нового, чтобы избежать дублирования.
        /// </summary>
        public static void ApplyLanguage(string lang)
        {
            var app = Application.Current;

            for (int i = app.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                var src = app.Resources.MergedDictionaries[i].Source?.OriginalString;
                if (src != null && (src.Contains("RuLang.xaml") || src.Contains("EnLang.xaml")))
                    app.Resources.MergedDictionaries.RemoveAt(i);
            }

            string path = lang == "ru"
                ? "Resources/Languages/RuLang.xaml"
                : "Resources/Languages/EnLang.xaml";

            app.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(path, UriKind.Relative)
            });

            LanguageChanged?.Invoke(lang);
        }
    }
}