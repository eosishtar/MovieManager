using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MovieManager.Logic
{
    public static class Helper
    {

        public static string[] ReservedWords()
        {
            var ReservedWords = new[]
            {
                "HDRip", "XviD", "BluRay", "DvDRip", "hevc", "bdrip", "Bluray", "x264", "h264", "AC3", "DTS", "480p", "720p", "1080p", "x0r", "mp4"
            };

            return ReservedWords;
        }

        public static int IdentifyFileType(string fullName)
        {
            //Check the extension first
            if (GetFileType(fullName) == "MP3")
            {
                return (int)FileType.MusicFile;
            }
            else if (GetFileType(fullName) == "SRT")
            {
                return (int)FileType.Subtitle;
            }

            //check if could be TV Series
            int seasonStart = fullName.IndexOf("S0", StringComparison.InvariantCultureIgnoreCase);
            var foundTVSeries = Regex.IsMatch(fullName, @"^.*S\d\dE\d\d", RegexOptions.IgnoreCase);
            if (foundTVSeries && seasonStart > -1 && fullName.Length > 4)
            {
                var season = fullName.Substring(seasonStart, 3);
                var seasonNumber = season.LastIndexOf(season, StringComparison.InvariantCultureIgnoreCase);

                var validInt = int.TryParse(seasonNumber.ToString(), out int result);
                if (validInt)
                {
                    return (int)FileType.TVSeries;
                }
            }

            //check if could be movie
            /*
                the index check, sometimes picks up 720p and it causes it to fail

                Solution is to have a set of reserved/known words to first be replaced in the moviename, 
                then we can extract the last 4 digits (hopefully a year)
            */

            var movieNameReplaced = fullName.ToLower();
            var reservedWords = Helper.ReservedWords();
            foreach (var item in reservedWords)
            {
                movieNameReplaced = movieNameReplaced.Replace(item.ToLower(), "");
            }

            var checkmoviename = movieNameReplaced;
            string onlyYear = new string(movieNameReplaced.Where(Char.IsDigit).ToArray());
            if (onlyYear.Length > 4)
            {
                //take the last 4 digits only... 
                onlyYear = onlyYear.Substring(onlyYear.Length - 4, 4);
            }

            if(LooksLikeYear(onlyYear))
            {
                return (int)FileType.Movie;
            }


            // int yearStart = fullName.IndexOf("20");
            // if (yearStart > -1 && fullName.Length > 4)
            // {
            //     //the movie check needs more work
            //     int year;
            //     var movieYear = fullName.Substring(yearStart, 4);
            //     movieYear = Regex.Replace(checkmoviename, "[^0-9]", "");

            //     var thisYear = DateTime.Now;

            //     //check if the year we found is more or less our current year.
            //     if (Convert.ToInt32(movieYear) >= thisYear.AddYears(-20).Year
            //         && Convert.ToInt32(movieYear) <= thisYear.AddYears(20).Year)
            //     {
            //         if (int.TryParse(movieYear, out year) && (year >= 1942 && year <= 2030))
            //         {
            //             return (int)FileType.Movie;
            //         }
            //     }

            //     //remove the year we thought, and check again
            //     var trimmedName = fullName.Replace(movieYear, "");
            //     return IdentifyFileType(trimmedName);
            // }

            return 0;
        }

        public static bool LooksLikeYear(string dataRound)
        {
            return Regex.IsMatch(dataRound, "^(19|20)[0-9][0-9]");
        }

        public static string GetFileType(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            int extensionStart = fileName.LastIndexOf(".", fileName.Length);

            if (extensionStart > 0)
            {
                int extensionLength = (fileName.Length - extensionStart);
                string extension = fileName.Substring(extensionStart + 1, extensionLength - 1);
                return extension.ToUpper();
            }
            else
            {
                return null;
            }
        }

        public static void DeleteFile(string fullpath, bool deleteDirectory = false)
        {
            if (File.Exists(fullpath))
            {
                File.SetAttributes(fullpath, FileAttributes.Normal);
                File.Delete(fullpath);

                if (deleteDirectory)
                {
                    //remove the file and get the path
                    string[] paths = fullpath.Split('\\');
                    var path = paths.Take(paths.Count() - 1);
                    var folderPath = string.Join("\\", path) + "\\";

                    if (Directory.Exists(folderPath))
                    {
                        //only delete directory if empty 
                        if (!Directory.EnumerateFiles(folderPath).Any())
                        {
                            Directory.Delete(folderPath, false);
                        }
                    }
                }
            }
        }

    }
}
