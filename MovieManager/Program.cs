using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MovieManager.Logic;
using MovieManager.Models;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace MovieManager
{
    class Program
    {
        private static Settings _settings;
        private static ServiceProvider _serviceProvider = null;
        private static ILogger _logger = null;

        static void Main(string[] args)
        {
            try
            {
                Run(args);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Crashed with exception:");
                Console.WriteLine(exception.ToString());
                Environment.Exit(1);
            }
        }

        public static void Run(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                ShowUsageExit();
            }

            BuildServiceProvider();
            InitializeLocals();

            _logger.LogInformation("MovieManager has started.");
            _logger.LogInformation($"MovieManager running cmd '{args[0]}'.");

            switch (args[0])
            {
                case "copy-file":
                    CopyFilesToDrive();
                    break;

                case "remove-torrents":
                    FromTorrentFiles();
                    break;

                case "test-moviedb-api":
                    TestMovieDbApi();
                    break;

                default:
                    break;
            }

            _logger.LogInformation($"MovieManager completed cmd '{args[0]}'.");
        }

        private static void FromTorrentFiles()
        {
            var t = new FileFunctions(_settings);
            var toProcess = t.FilesAlreadyCopied();

            foreach (var item in toProcess)
            {
                if (item.Item1.ToString() == "Check Date")
                {
                    //TODO: check if can delete the torrent,
                }
            }
        }

        private static void TestMovieDbApi()
        {
            var movieapi = new MovieDbApi(_settings);
            var test = movieapi.TestMovieDbApiAsync();
        }

        private static void CopyFilesToDrive()
        {
            var filefuncs = new FileFunctions(_settings);
            var filesToCopy = filefuncs.GetFilesToCopy();

            var logic = new CopyFunctions(_settings);
            int itemCnt = 0;

            if (filesToCopy.Count > 0)
            {
                string fileNames = null;

                foreach (var item in filesToCopy)
                {
                    try
                    {
                        var fileCopied = logic.Copy(item);
                        AddFileToCopyList(Path.GetFileName(item), fileCopied);
                        itemCnt++;

                        var fileString = string.Format(item, Environment.NewLine);
                        fileNames = string.Concat(fileNames, fileString);
                    }
                    catch (Exception ex)
                    {
                        if (ex.HResult == -2147024713)
                        {
                            //file already exists
                            string rawFilePath = ex.Message.ToString();
                            int fileNameStart = rawFilePath.IndexOf("'");
                            int fileNameEnd = rawFilePath.LastIndexOf("'");
                            var filePath = rawFilePath.Substring(fileNameStart, fileNameEnd - fileNameStart);    //Name of the file

                            AddFileToCopyList(Path.GetFileName(item), filePath);
                            continue;
                        }

                        //log error 
                        _logger.LogError(ex, $"Error occurred while copying '{filesToCopy[itemCnt]}'. Error: {ex.Message}");
                        Console.WriteLine($"Error occurred while copying '{filesToCopy[itemCnt]}'. Error: {ex.Message}");
                    }
                }

                _logger.LogInformation($"{filesToCopy.Count} files copied.");
            }
        }

        private static void AddFileToCopyList(string fileName, string destPath)
        {
            var filePath = Path.Combine(_settings.DownloadPath, globals.FileCopyName);

            using (StreamWriter stream = new FileInfo(filePath).AppendText())
            {
                var writeString = $"{fileName};{destPath};{DateTime.Now}";
                stream.WriteLine(writeString);
            }

            Console.WriteLine($"Copied file '{fileName}' to '{destPath}'.");
        }

        private static void ShowUsageExit()
        {
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);

            Console.WriteLine("Usage:");
            Console.WriteLine($" {exeName} copy-file");

            Environment.Exit(1);
        }

        private static void BuildServiceProvider()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging();

            serviceCollection.AddLogging(builder =>
            {
                //  Serilog Configuration
                var configuration = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext();

                //  Log to text File 
                string logPath = config.GetSection("Serilog:LogPath").Value ?? throw new ArgumentNullException("LogPath");

                configuration.WriteTo.File(logPath, fileSizeLimitBytes: 1_000_000, rollOnFileSizeLimit: true,
                    shared: true, flushToDiskInterval: TimeSpan.FromSeconds(1));

                //  Build Serilog
                Log.Logger = configuration.CreateLogger();

                builder
                    .AddConfiguration(config.GetSection("Logging"))
                    .AddConsole()
                    .AddEventLog()
                    .AddSerilog(dispose: true);
            });

            //Depenedency Injection
            serviceCollection.AddSingleton<IConfiguration>(config);

            _serviceProvider = serviceCollection.BuildServiceProvider();


            //populate the settings class
            var extensionString = config.GetSection("PathConfig:Extensions").Value.Split(',') ?? throw new ArgumentNullException("DownloadPath");
            var extensions = new List<string>();

            foreach (var extension in extensionString)
            {
                extensions.Add(extension.ToUpper());
            }

            var settings = new Settings
            {
                DownloadPath = config.GetSection("PathConfig:DownloadPath").Value ?? throw new ArgumentNullException("DownloadPath"),
                CompletedMoviePath = config.GetSection("PathConfig:CompleteMoviePath").Value ?? throw new ArgumentNullException("CompleteMoviePath"),
                CompletedTVPath = config.GetSection("PathConfig:CompleteTVPath").Value ?? throw new ArgumentNullException("CompleteTVPath"),
                Extensions = extensions,
                MovieDbApiKey = config.GetSection("MovieDbApi:ApiKey").Value ?? throw new ArgumentNullException("ApiKey"),
                MovieDbServerUrl = config.GetSection("MovieDbApi:ServerUrl").Value ?? throw new ArgumentNullException("ServerUrl")
            };

            //check the paths 
            if (!Directory.Exists(settings.CompletedMoviePath))
            {
                Directory.CreateDirectory(settings.CompletedMoviePath);
            }
            if (!Directory.Exists(settings.CompletedTVPath))
            {
                Directory.CreateDirectory(settings.CompletedTVPath);
            }

            _settings = settings;

        }

        private static void InitializeLocals()
        {
            if (_logger == null)
            {
                _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
            }
        }
    }
}
