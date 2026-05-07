using System.Text;

namespace eCertify.Utils
{
    public class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _basePath;
        private static readonly object _lock = new();

        public FileLogger(string categoryName, string basePath)
        {
            _categoryName = categoryName;
            _basePath = basePath;
        }

        public IDisposable BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            string message = formatter(state, exception);
            if (string.IsNullOrEmpty(message))
                return;

            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {_categoryName} - {message}";
            if (exception != null)
            {
                logMessage += Environment.NewLine + exception;
            }

            lock (_lock)
            {
                string directory = Path.GetDirectoryName(_basePath)!;
                Directory.CreateDirectory(directory);

                string logFileName = Path.GetFileNameWithoutExtension(_basePath) + "-" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                string fullPath = Path.Combine(directory, logFileName);

                File.AppendAllText(fullPath, logMessage + Environment.NewLine, Encoding.UTF8);
            }
        }
    }
}
