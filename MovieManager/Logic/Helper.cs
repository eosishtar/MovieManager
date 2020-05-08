using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MovieManager.Logic
{
    public static class Helper
    {
        public static int CheckIfMovieOrTvSeries(string fullName)
        {
            int yearStart = fullName.IndexOf("20");
            int seasonStart = fullName.IndexOf("S0");

            //check if could be TV Series
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
            if (yearStart > -1 && fullName.Length > 4)
            {
                //the move check needs more work

                int year;
                var movieYear = fullName.Substring(yearStart, 4);

                var thisYear = DateTime.Now;

                //check if the year we found is more or less our current year.
                if (Convert.ToInt32(movieYear) >= thisYear.AddYears(-10).Year
                    && Convert.ToInt32(movieYear) <= thisYear.AddYears(10).Year)
                {
                    if (int.TryParse(movieYear, out year) && (year >= 1942 && year <= 2030))
                    {
                        return (int)FileType.Movie;
                    }
                }

                //remove the year we thought, and check again
                var trimmedName = fullName.Replace(movieYear, "");
                return CheckIfMovieOrTvSeries(trimmedName);
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

        public static string GetFileType(string fileName)
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
    }
}
