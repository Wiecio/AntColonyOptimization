using ACO.DataLoading;

namespace ACO.Shared.Logging
{
    internal class Logger
    {
        private readonly string _logPath;
        private readonly object _locker = new();

        public Logger(string workingDirectory, string fileName)
        {
            _logPath = Path.Combine(workingDirectory,
                $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now:MM-dd-yy-H-mm-ss}.{Path.GetExtension(fileName)}");
            Directory.CreateDirectory(workingDirectory ?? throw new DirectoryNotFoundException());
            File.Create(_logPath);
        }

        public Logger(ProblemData problemData, string workingDirectory)
        {
            _logPath = Path.Combine(workingDirectory, $"{problemData.Name}_{DateTime.Now:MM-dd-yy-H-mm-ss}.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath) ?? throw new DirectoryNotFoundException());
            File.Create(_logPath);
        }

        public void Log(LogDataBase logData)
        {
            lock (_locker)
            {
                LogWithRetry(10, logData);
            }
        }

        private void LogWithRetry(int retries, LogDataBase logData)
        {
            Console.WriteLine(logData);
            for (var i = 0; i < retries; i++)
            {
                try
                {
                    using var streamWriter = new StreamWriter(_logPath, append: true);
                    streamWriter.WriteLine(logData);
                    break;
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Exception caught while loggin data This was {i} attempt. Exception:");
                    Console.WriteLine(exception);
                    var time = (int)(Random.Shared.NextDouble() * 1000);
                    Console.WriteLine($"Waiting {time} ms");
                    Thread.Sleep(time);
                    continue;
                }
            }
        }
    }
}