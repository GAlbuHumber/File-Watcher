using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Timers;
using Timer = System.Timers.Timer;

namespace FileWatcherWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private FileSystemWatcher _watcher;
        private Timer _timer;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            InitializeWatcher();
        }

        private void InitializeWatcher()
        {
            _watcher = new FileSystemWatcher
            {
                Path = _configuration["sourcepath"],
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = "*.*"
            };

            _watcher.Created += OnCreate;
            _watcher.Deleted += OnDelete;
            _watcher.Renamed += OnRenamed;

            _timer = new Timer(double.Parse(_configuration["delay1"]));
            _timer.Elapsed += DeleteFolderContents;
        }

        private void DeleteFolderContents(object sender, ElapsedEventArgs e)
        {
            DirectoryInfo dirSource = new DirectoryInfo(_configuration["sourcepath"]);
            DateTime now = DateTime.Now;
            foreach (FileInfo f in dirSource.GetFiles())
            {
                if (f.LastAccessTime < DateTime.Now.AddMinutes(int.Parse(_configuration["delay1"])))
                {
                    f.Delete();
                }
            }
        }

        private bool CheckFileHasCopied(string FilePath)
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    using (File.OpenRead(FilePath))
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                Thread.Sleep(100);
                return CheckFileHasCopied(FilePath);
            }
        }

        private string GetDailyDestinationFolder()
        {
            DateTime now = DateTime.Now;
            string sourcePath = _configuration["sourcepath"];

            if (!Directory.Exists(sourcePath))
            {
                Directory.CreateDirectory(sourcePath);
            }

            string parentSourcePath = Directory.GetParent(sourcePath).FullName;

            // Construct the backup folder name
            string dailyFolderName;

            if (now.TimeOfDay > new TimeSpan(12, 0, 0))
            {
                dailyFolderName = $"Backup_{now:yyyy-MM-dd_HH-mm-ss}";
            }
            else
            {
                dailyFolderName = $"Default_Backup_{now:yyyy-MM-dd}";
            }

            // Create a separate "logs" folder
            string logsFolderPath = Path.Combine(parentSourcePath, "logs");
            if (!Directory.Exists(logsFolderPath))
            {
                Directory.CreateDirectory(logsFolderPath);
            }

          
            return Path.Combine(parentSourcePath, dailyFolderName);
        }



        private void OnCreate(object source, FileSystemEventArgs e)
        {
            int n = 1;
            DateTime now = DateTime.Now;
            string crtFileName = e.Name;

            string sourceFile = Path.Combine(_configuration["sourcepath"], crtFileName);

            string dailyDestinationFolder = GetDailyDestinationFolder();
            if (!Directory.Exists(dailyDestinationFolder))
            {
                Directory.CreateDirectory(dailyDestinationFolder);
            }

            string destinationFile = Path.Combine(dailyDestinationFolder, crtFileName);
            int idx = destinationFile.LastIndexOf('.');
            string partDest = idx != -1 ? destinationFile.Substring(0, idx) : destinationFile;
            string ext = idx != -1 ? destinationFile.Substring(idx + 1) : "";

            if (CheckFileHasCopied(e.FullPath))
            {
                try
                {
                    if (!File.Exists(destinationFile))
                    {
                        File.Copy(sourceFile, destinationFile, true);
                        LogFileAction(dailyDestinationFolder, $"{now.ToString("F")} file detected {crtFileName}");
                        LogFileAction(dailyDestinationFolder, $"{now.ToString("F")} file {crtFileName} will be moved to {dailyDestinationFolder} after {int.Parse(_configuration["delay1"]) / 1000} s");
                        LogFileAction(dailyDestinationFolder, $"{now.ToString("F")} file {crtFileName} has been moved to {dailyDestinationFolder} will be deleted after {int.Parse(_configuration["delay2"])} min");
                    }
                    else
                    {
                        string copyDestination = !File.Exists(partDest + " - Copy." + ext)
                            ? partDest + " - Copy." + ext
                            : GetUniqueCopyDestination(partDest, ext, n);

                        File.Copy(sourceFile, copyDestination, true);
                        LogFileAction(dailyDestinationFolder, $"{now.ToString("F")} file detected {crtFileName}");
                        LogFileAction(dailyDestinationFolder, $"{now.ToString("F")} file {crtFileName} will be moved to {dailyDestinationFolder} after {int.Parse(_configuration["delay1"]) / 1000} s");
                        LogFileAction(dailyDestinationFolder, $"{now.ToString("F")} file {crtFileName} has been moved to {copyDestination} will be deleted after {int.Parse(_configuration["delay2"])} min");
                    }
                }
                catch (IOException iox)
                {
                    LogFileAction(dailyDestinationFolder, "Following exception occurred:", iox.Message);
                }
            }
        }

        private string GetUniqueCopyDestination(string partDest, string ext, int n)
        {
            while (File.Exists(partDest + " - Copy (" + n + ")." + ext))
                n++;
            return partDest + " - Copy (" + n + ")." + ext;
        }

        private void LogFileAction(params string[] messages)
        {
            string logsFolderPath = Path.Combine(_configuration["sourcepath"], "logs");
            string logFilePath = Path.Combine(logsFolderPath, "logs.txt");

            if (!Directory.Exists(logsFolderPath))
            {
                Directory.CreateDirectory(logsFolderPath);
            }

            using (StreamWriter writer = File.AppendText(logFilePath))
            {
                foreach (var message in messages)
                {
                    writer.WriteLine(message);
                }
            }
        }


        private void OnDelete(object source, FileSystemEventArgs e)
        {
            DateTime now = DateTime.Now;

            using (StreamWriter writer = File.AppendText(_configuration["logfile"]))
            {
                writer.WriteLine($"{now.ToString("F")} {e.Name} deleted from {_configuration["sourcepath"]}");
            }
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            using (StreamWriter writer = new StreamWriter(_configuration["sourcepath"], true))
            {
                writer.WriteLine(e.OldFullPath + " renamed to " + e.FullPath);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _watcher.EnableRaisingEvents = true;
            _timer.Start();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            _watcher.EnableRaisingEvents = false;
            _timer.Stop();
        }
    }
}
