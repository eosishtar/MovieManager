using MovieManager.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MovieManager.Logic
{
    public class CopyFunctions
    {
        public Settings _settings;

        public CopyFunctions(Settings settings)
        {
            this._settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public (string, bool) Copy(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

            DirectoryInfo source = new DirectoryInfo(Path.Combine(_settings.DownloadPath, fileName));

            //determine if the file is a movie or tv series
            var fileType = Helper.IdentifyFileType(source.Name);
            DirectoryInfo target = null;

            switch (fileType)
            {
                case (int)FileType.Subtitle:
                    var moviePath = source.Parent.ToString();
                    var movieName = moviePath.Replace("subtitles", "", StringComparison.InvariantCultureIgnoreCase);

                    var subtitlePath = "subtitle";
                    if(moviePath.Contains("subtitles", StringComparison.CurrentCultureIgnoreCase))
                    {
                        subtitlePath = $"{subtitlePath}s"; 
                    }
                    movieName = moviePath.Replace(subtitlePath, "", StringComparison.InvariantCultureIgnoreCase);
                    movieName = movieName.Replace(_settings.DownloadPath, "", StringComparison.InvariantCultureIgnoreCase);

                    var formattedMovieName = GetMovieName(movieName);
                    var actualMovieName = Path.GetFileName(formattedMovieName);

                    target = new DirectoryInfo(Path.Combine(_settings.CompletedMoviePath, actualMovieName));
                    return CopySubtitle(source, target, actualMovieName);

                case (int)FileType.Movie:
                    target = new DirectoryInfo(Path.Combine(_settings.CompletedMoviePath, source.Name));
                    return CopyMovie(source, target, fileName);

                case (int)FileType.TVSeries:
                    //need to check and create the tv series directory
                    var tvPath = CreateTVPath(source.Name);
                    target = new DirectoryInfo(Path.Combine(tvPath, source.Name));
                    return CopyTVSeries(source, target, fileName);

                case (int)FileType.MusicFile:
                    //not catering for this now
                    return (string.Empty, false);

                default:
                    //dont know the type of file
                    return (string.Empty, false);
            }
        }

        private (string, bool) CopyMovie(DirectoryInfo source, DirectoryInfo target, string fileName)
        {
            var fi = CheckFileLocation(source.FullName, target.FullName);

            var moviePath = Path.Combine(target.FullName, fi.Name);
            string newFile = string.Empty;
            bool fileCopied;

            if (!File.Exists(moviePath))
            {
                fi.CopyTo(moviePath, true);
                fileCopied = true;

                //rename the newly copied folder
                var movieName = GetMovieName(source.Name);
                var extension = Helper.GetFileType(fi.Name);
                var formattedMovieName = $"{movieName}.{extension}";

                newFile = Path.Combine(_settings.CompletedMoviePath, movieName, formattedMovieName);

                try
                {
                    //rename the actual folder
                    target.MoveTo(Path.Combine(target.Parent.FullName, movieName));

                    //rename the file 
                    target = new DirectoryInfo(Path.Combine(target.Parent.FullName, movieName));
                    var oldFile = Path.Combine(target.FullName, source.Name);

                    //check if the new name isnt already been renamed. Sometimes the movies name are correct.
                    if (oldFile.Trim().ToUpper() != newFile.Trim().ToUpper())
                    {
                        RenameFile(oldFile, newFile);
                    }

                    return (newFile, fileCopied);
                }
                catch (Exception ex)
                {
                    //delete newly created file/folder
                    fileCopied = false;
                    DeleteDirectory(target.FullName);
                }
            }
            else
            {
                fileCopied = false;
                LogToFile($"File '{moviePath}' already exists. File not copied.");
            }

            return (string.IsNullOrEmpty(newFile) ? moviePath : newFile
                , fileCopied);
        }

        private (string, bool) CopyTVSeries(DirectoryInfo source, DirectoryInfo target, string fileName)
        {
            var fi = CheckFileLocation(source.FullName, target.FullName);
            var tvPath = Path.Combine(target.FullName, fi.Name);
            string newFileName = string.Empty;
            bool fileCopied = false;

            if (!File.Exists(tvPath))
            {
                //copy file 
                if (!File.Exists(tvPath))
                {
                    fi.CopyTo(tvPath, true);

                    fileCopied = true;
                    //move file outside folder, into season folder
                    var oldLocation = tvPath;
                    var newLocation = Path.Combine(target.Parent.FullName, fi.Name + "_temp");

                    try
                    {
                        File.Move(oldLocation, newLocation);

                        //delete folder
                        if (Directory.Exists(target.FullName))
                        {
                            Directory.Delete(target.FullName);
                        }
                    }
                    catch
                    {
                        //tv series already exisits
                        DeleteDirectory(target.FullName);
                    }

                    //remove '_temp' name
                    newFileName = newLocation.Replace("_temp", "");
                    File.Move(newLocation, newFileName);
                }
                else
                {
                    //do error, file not copied.... 
                    fileCopied = false;
                    LogToFile($"File '{tvPath}' already exists. File not copied.");
                }
            }

            return (newFileName, fileCopied);
        }

        private (string, bool) CopySubtitle(DirectoryInfo source, DirectoryInfo target, string fileName)
        {
            var fi = CheckFileLocation(source.FullName, target.FullName);
            bool fileCopied = false;
            var moviePath = Path.Combine(target.FullName, fi.Name);
            string newFilePath = moviePath;

            if (!File.Exists(Path.Combine(target.FullName, fi.Name)))
            {
                //TODO... check the subtitle language
                string newFileName = string.Format($"{fileName}{fi.Name}");
                newFilePath = Path.Combine(_settings.CompletedMoviePath, fileName, newFileName);

                if (!File.Exists(newFilePath))
                {
                    fi.CopyTo(moviePath, true);

                    //rename subtitle
                    //RenameFile(moviePath, newFilePath);
                    fileCopied = true;
                }
            }

            return (newFilePath, fileCopied);
        }

        private FileInfo CheckFileLocation(string sourceName, string targetName)
        {
            FileInfo fi = new FileInfo(sourceName);

            if (!Directory.Exists(targetName))
            {
                Directory.CreateDirectory(targetName);
            }

            return fi;
        }

        public string GetMovieName(string name)
        {
            // get the list of reserved words
            var reservedWords = Helper.ReservedWords();

            var movieNameReplaced = name.ToLower();
            foreach (var item in reservedWords)
            {
                movieNameReplaced = movieNameReplaced.Replace(item.ToLower(), "");
            }

            //strip everything except numbers to try determine the year
            string onlyNumbers = new string(movieNameReplaced.Where(Char.IsDigit).ToArray());

            do
            {
                if (onlyNumbers.Length >= 4)
                {
                    int year;
                    //get the year, working from right to left
                    string yearTest = onlyNumbers.Substring(onlyNumbers.Length - 4, 4);

                    if (Helper.LooksLikeYear(yearTest))
                    {
                        if (int.TryParse(yearTest, out year)) // && (year >= 1942 && year <= 2030))
                        {
                            //we have found the year, strip everything from the year on
                            int yearStart = name.IndexOf(yearTest);

                            var movieName = name.Remove(yearStart + 4, name.Length - yearStart - 4);    //Name of the movie
                            movieName = movieName.Replace(".", " ", true, System.Globalization.CultureInfo.InvariantCulture);

                            movieName = Regex.Replace(movieName, @"(\[|\]|\{|\}|\(|\)|\/|\\|\'|\.)", "");
                            movieName = movieName.Replace(yearTest, "", true, System.Globalization.CultureInfo.InvariantCulture);

                            var formattedMovieName = $"{movieName.Trim()} ({year})";

                            return formattedMovieName;
                        }
                    }

                    //remove the last digit and try again
                    onlyNumbers = onlyNumbers.Remove(onlyNumbers.Length - 1, 1);
                }

            } while (onlyNumbers.Length > 1);

            throw new Exception($"Could not get movie name from '{name}'");
        }


        private string CreateTVPath(string name)
        {
            //Create the directory for the whole tv series.
            int seasonStart = name.IndexOf("S0");
            var seasonName = name.Substring(0, seasonStart - 1);    //Name of the season
            seasonName = seasonName.Replace(".", " ", true, System.Globalization.CultureInfo.InvariantCulture);

            var seasonNumber = name.Substring(seasonStart + 2, 1);   //Just the season number

            var seasonPath = Path.Combine(_settings.CompletedTVPath, seasonName);

            //Check if the folder exist for the season, create if not...
            if (!Directory.Exists(seasonPath))
            {
                Directory.CreateDirectory(seasonPath);
            }

            seasonPath = Path.Combine(seasonPath, "Season " + seasonNumber);
            if (!Directory.Exists(seasonPath))
            {
                Directory.CreateDirectory(seasonPath);
            }

            return seasonPath;
        }

        private bool RenameFile(string oldFile, string newFile)
        {
            //rename if the file exists and new file doesnt
            if (File.Exists(oldFile) &&
                !File.Exists(newFile))
            {
                File.Move(oldFile, newFile);
                return true;
            }

            return false;
        }

        private static void LogToFile(string message)
        {
            lock (typeof(CopyFunctions))
            {
                var path = Path.GetTempPath();
                path = Path.Combine(path, "MovieManager.log");

                File.AppendAllText(path, DateTime.Now.ToString("yyyyMMdd HHmmss") + ": " + message + Environment.NewLine);
            }
        }

        //this method will delete the files and then the directory
        public void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }
    }
}
