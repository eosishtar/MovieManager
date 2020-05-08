using MovieManager.Models;
using System;
using System.IO;
using System.Linq;

namespace MovieManager.Logic
{
    public class CopyFunctions
    {
        public Settings _settings;

        public CopyFunctions(Settings settings)
        {
            this._settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public string Copy(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

            DirectoryInfo source = new DirectoryInfo(Path.Combine(_settings.DownloadPath, fileName));

            //determine if the file is a movie or tv series
            var fileType = CheckIfMovieOrTvSeries(source.Name);
            DirectoryInfo target = null;

            switch (fileType)
            {
                case (int)FileType.Subtitle:
                    var moviePath = source.Parent.ToString();
                    var movieName = moviePath.Replace("subtitles", "", StringComparison.InvariantCultureIgnoreCase);
                    var formattedMovieName = GetMovieName(movieName);
                    var actualMovieName = Path.GetFileName(formattedMovieName);

                    target = new DirectoryInfo(Path.Combine(_settings.CompletedMoviePath, actualMovieName));
                    break;

                case (int)FileType.Movie:
                    target = new DirectoryInfo(Path.Combine(_settings.CompletedMoviePath, source.Name));
                    break;

                case (int)FileType.TVSeries:
                    //need to check and create the tv series directory
                    var tvPath = CreateTVPath(source.Name);
                    target = new DirectoryInfo(Path.Combine(tvPath, source.Name));
                    break;

                case (int)FileType.MusicFile:
                    //not catering for this now
                    return null;

                default:
                    //dont know the type of file
                    return null;
            }


            //check if can copy this type if file
            FileInfo fi = new FileInfo(source.FullName);

            if (!Directory.Exists(target.FullName))
            {
                Directory.CreateDirectory(target.FullName);
            }

            if (fileType == (int)FileType.Movie)
            {
                //copy the file 
                var moviePath = Path.Combine(target.FullName, fi.Name);
                if (!File.Exists(Path.Combine(target.FullName, fi.Name)))
                {
                    fi.CopyTo(moviePath, true);

                    //rename the newly copied folder
                    var movieName = GetMovieName(source.Name);
                    var extension = GetFileType(fi.Name);
                    var formattedMovieName = $"{movieName}.{extension}";

                    var newFile = Path.Combine(_settings.CompletedMoviePath, movieName);
                    newFile = Path.Combine(newFile, formattedMovieName);

                    try
                    {
                        target.MoveTo(Path.Combine(target.Parent.FullName, movieName));

                        //rename the file 
                        target = new DirectoryInfo(Path.Combine(target.Parent.FullName, movieName));
                        var oldFile = Path.Combine(target.FullName, source.Name);

                        File.Move(oldFile, newFile);

                        return newFile;
                    }
                    catch (Exception ex)
                    {
                        //delete newly created file/folder
                        DeleteDirectory(target.FullName);

                        return newFile;
                    }
                }
                else
                {
                    LogToFile($"File '{moviePath}' already exists. File not copied.");
                }
            }
            else if (fileType == (int)FileType.Subtitle)
            {
                //copy the file 
                var moviePath = Path.Combine(target.FullName, fi.Name);
                if (!File.Exists(Path.Combine(target.FullName, fi.Name)))
                {
                    fi.CopyTo(moviePath, true);
                }
                return moviePath;
            }
            else if (fileType == (int)FileType.TVSeries)
            {
                var tvPath = Path.Combine(target.FullName, fi.Name);
                if (!File.Exists(tvPath))
                {
                    //copy file 
                    if (!File.Exists(tvPath))
                    {
                        fi.CopyTo(tvPath, true);

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
                        string newName = newLocation.Replace("_temp", "");
                        File.Move(newLocation, newName);

                        return newName;
                    }
                    else
                    {
                        //do error, file not copied.... 
                        LogToFile($"File '{tvPath}' already exists. File not copied.");
                    }
                }
            }

            //nothing copied
            return string.Empty;
        }

        private string GetMovieName(string name)
        {
            int yearStart = name.IndexOf("20");
            var movieName = name.Substring(0, yearStart - 1);    //Name of the movie
            movieName = movieName.Replace(".", " ", true, System.Globalization.CultureInfo.InvariantCulture);

            var year = name.Substring(yearStart, 4);
            var formattedMovieName = $"{movieName} ({year})";

            return formattedMovieName;
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

        private int CheckIfMovieOrTvSeries(string fullName)
        {
            int yearStart = fullName.IndexOf("20");
            int seasonStart = fullName.IndexOf("S0");

            if (yearStart > -1 && fullName.Length > 4)
            {
                int year;
                var movieYear = fullName.Substring(yearStart, 4);
                if (int.TryParse(movieYear, out year) && (year >= 1942 && year <= 2030))
                {
                    return (int)FileType.Movie;
                }
            }


            if (seasonStart > -1 && fullName.Length > 4)
            {
                var season = fullName.Substring(seasonStart, 3);
                var seasonNumber = season.LastIndexOf(season, StringComparison.InvariantCultureIgnoreCase);

                var validInt = int.TryParse(seasonNumber.ToString(), out int result);
                if (validInt)
                {
                    return (int)FileType.TVSeries;
                }
            }

            if (GetFileType(fullName) == "MP3")
            {
                return (int)FileType.MusicFile;
            }
            else if (GetFileType(fullName) == "SRT")
            {
                return (int)FileType.Subtitle;
            }

            return 0;
        }

        private bool CheckCopyThisFile(string fileExtension)
        {
            return _settings.Extensions.Any(x => x.Contains(fileExtension.ToUpper()));
        }

        private string GetFileType(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            var extensionStart = fileName.LastIndexOf(".", fileName.Length);

            if (extensionStart > 0)
            {
                int extensionLength = (fileName.Length - extensionStart);
                var extension = fileName.Substring(extensionStart + 1, extensionLength - 1);
                return extension.ToUpper();
            }
            else
            {
                return null;
            }
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
