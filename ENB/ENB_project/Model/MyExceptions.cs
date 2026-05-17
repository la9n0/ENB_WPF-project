using System.IO;

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

    public class MyExceptions : Exception
    {
        private static readonly string LogPath = Path.Combine(
            AppContext.BaseDirectory, "app_data", "log.txt");

        public ExceptionCategory Category { get; }
        public ExceptionSeverity Severity { get; }

        public static event Action<string>? CriticalErrorOccurred;

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
                CriticalErrorOccurred?.Invoke(message);
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