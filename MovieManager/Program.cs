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

                case "remove-duplicates":
                    RemoveDuplicates();
                    break;

                case "check-duplicates":
                    CheckDuplicates();
                    break;

                //Still Work in Progress Items 
                case "remove-torrents":
                    RemoveTorrentFiles();
                    break;

                case "test-moviedb-api":
                    TestMovieDbApi();
                    break;

                default:
                    ShowUsageExit();
                    break;
            }

            _logger.LogInformation($"MovieManager completed cmd '{args[0]}'.");
        }

        private static void RemoveTorrentFiles()
        {
            var t = new FileFunctions(_settings);
            var toProcess = t.FilesAlreadyCopied();

            var today = DateTime.Now;

            foreach (var item in toProcess)
            {
                var torrentComplete = Convert.ToDateTime(item.Item2).AddDays(_settings.TorrentSeedDays);

                if (torrentComplete < today)
                {
                    //TODO: Delete file out of download so it cant seed anymore
                    //var deleteFile = new CopyFunctions(_settings);

                    //var path = Path.Combine(_settings.DownloadPath, item.Item1);
                    //deleteFile.DeleteDirectory(path);

                    //_logger.LogInformation($"File '{item.Item1}' removed from '{path}'.");
                }
            }
        }

        private static void TestMovieDbApi()
        {
            //TODO: Work in Progress
            //var movieapi = new MovieDbApi(_settings);
            //var test = movieapi.TestMovieDbApiAsync();
        }

        private static void CheckDuplicates()
        {
            var fileFuncs = new FileFunctions(_settings);

            var dupItems = fileFuncs.GetDuplicatesCopied();

            if (dupItems.Count == 0)
            {
                Console.WriteLine($"No duplicates were found.");
            }

            //write the findings to temp location
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var path = Path.Combine(desktop, "DuplicateFiles.txt");
            Helper.DeleteFile(path);

            File.AppendAllText(path,   "Duplicate files found..." + Environment.NewLine);
            foreach (var item in dupItems)
            {
                File.AppendAllText(path, 
                    Environment.NewLine +
                        $"{item.DuplicateName}" +
                        Environment.NewLine +
                        string.Format("         ", 25) +  $"{item.FullPath1}         {item.FullPath2}");
            }

            _logger.LogInformation($"Duplicates found. Please check file '{path}'.");
            Console.ReadKey();
        }

        private static void RemoveDuplicates()
        {
            var fileFuncs = new FileFunctions(_settings);

            var dupItems = fileFuncs.GetDuplicatesCopied();

            if (dupItems.Count == 0)
            {
                Console.WriteLine($"No duplicates were found.");
            }

            foreach (var item in dupItems)
            {
                var path1 = Path.GetDirectoryName(item.FullPath1);
                var path2 = Path.GetDirectoryName(item.FullPath2);

                //delete the duplicate in another directory
                if (path1.Contains(item.DuplicateName))
                {
                    File.SetAttributes(Path.Combine(path2, item.DuplicateName), FileAttributes.Normal);
                    File.Delete(Path.Combine(path2, item.DuplicateName));

                    _logger.LogInformation($"Duplicate file '{item.DuplicateName}' deleted from '{path2}'.");
                }
                else
                {
                    File.SetAttributes(Path.Combine(path1, item.DuplicateName), FileAttributes.Normal);
                    File.Delete(Path.Combine(path1, item.DuplicateName));

                    _logger.LogInformation($"Duplicate file '{item.DuplicateName}' deleted from '{path1}'.");
                }
            }
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
                        AddFileToCopyList(Path.GetFileName(item), fileCopied.Item1, fileCopied.Item2);
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

                            AddFileToCopyList(Path.GetFileName(item), filePath, false);
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

        private static void AddFileToCopyList(string fileName, string destPath, bool copied)
        {
            if (copied)
            {
                var filePath = Path.Combine(_settings.DownloadPath, globals.FileCopyName);

                using (StreamWriter stream = new FileInfo(filePath).AppendText())
                {
                    var writeString = $"{fileName};{destPath};{DateTime.Now}";
                    stream.WriteLine(writeString);
                }

                Console.WriteLine($"Copied file '{fileName}' to '{destPath}'.");
            }
            else
            {
                //just display to the user the file was processed but not copied.
                Console.WriteLine($"File '{fileName}' not copied. File already exists in '{destPath}'.");
            }
        }

        private static void ShowUsageExit()
        {
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);

            Console.WriteLine("Usage:");
            Console.WriteLine($" {exeName} copy-file");
            Console.WriteLine($" {exeName} check-duplicates");
            Console.WriteLine($" {exeName} remove-duplicates");

            Console.ReadKey();
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
                MovieDbServerUrl = config.GetSection("MovieDbApi:ServerUrl").Value ?? throw new ArgumentNullException("ServerUrl"),
                TorrentSeedDays = Convert.ToInt32(config.GetSection("Torrents:SeedDays").Value),
                SampleVideoDelete = bool.TryParse(config.GetSection("SampleVideos:EnableDelete").Value, out bool result),
                SampleSizeLimit = Convert.ToInt32(config.GetSection("SampleVideos:SizeLimitCheck").Value)
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
