using System.IO;
using System.Windows;

namespace ENB_project
{
    public enum ExceptionCategory
    {
        IO,
        Data,
        Navigation,
        Validation,
        Unknown
    }

    public enum ExceptionSeverity
    {
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Custom application exception with categorization, severity level and automatic
    /// file logging. Shows a MessageBox for Critical severity.
    /// </summary>
    public class MyExceptions : Exception
    {
        private static readonly string LogPath = Path.Combine(
            AppContext.BaseDirectory, "app_data", "log.txt");

        public ExceptionCategory Category { get; }
        public ExceptionSeverity Severity { get; }

        public MyExceptions(
            string message,
            string location,
            ExceptionCategory category = ExceptionCategory.Unknown,
            ExceptionSeverity severity = ExceptionSeverity.Error,
            Exception? inner = null)
            : base(message, inner)
        {
            Category = category;
            Severity = severity;

            Log(message, location, category, severity, inner);

            if (severity == ExceptionSeverity.Critical)
                MessageBox.Show(
                    $"Critical error: {message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
        }

        private static void Log(
            string message,
            string location,
            ExceptionCategory category,
            ExceptionSeverity severity,
            Exception? inner)
        {
            try
            {
                var dir = Path.GetDirectoryName(LogPath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using var writer = File.AppendText(LogPath);

                writer.WriteLine(
                    $"[ {DateTime.Now:yyyy-MM-dd HH:mm:ss} | " +
                    $"{severity} | " +
                    $"{category} | " +
                    $"Location: {location} | " +
                    $"Message: {message}" +
                    (inner != null ? $" | Caused by: {inner.Message}" : "") +
                    " ]");
            }
            catch
            {
                // логгер не должен ломать приложение
            }
        }

        public static MyExceptions IO(string message, string location, Exception? inner = null)
            => new(message, location, ExceptionCategory.IO, ExceptionSeverity.Error, inner);

        public static MyExceptions Data(string message, string location, Exception? inner = null)
            => new(message, location, ExceptionCategory.Data, ExceptionSeverity.Error, inner);

        public static MyExceptions Navigation(string message, string location)
            => new(message, location, ExceptionCategory.Navigation, ExceptionSeverity.Warning);

        public static MyExceptions Validation(string message, string location)
            => new(message, location, ExceptionCategory.Validation, ExceptionSeverity.Warning);

        public static MyExceptions Critical(string message, string location, Exception? inner = null)
            => new(message, location, ExceptionCategory.Unknown, ExceptionSeverity.Critical, inner);
    }
}