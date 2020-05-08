using MovieManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MovieManager.Logic
{
    public class FileFunctions
    {
        private readonly Settings _settings;

        public FileFunctions(Settings settings)
        {
            this._settings = settings;
        }

        public List<Tuple<string, string>> FilesAlreadyCopied()
        {
            var alreadyCopiedMovies = Path.Combine(_settings.DownloadPath, globals.FileCopyName);
            if (!File.Exists(alreadyCopiedMovies))
            {
                File.Create(alreadyCopiedMovies);
                //file doesnt exist, assume no movies have been copied.
                return new List<Tuple<string, string>>();
            }

            //Read all the movies into a list 
            var list = new List<Tuple<string, string>>();
            var fileStream = new FileStream(alreadyCopiedMovies, FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    //only take the file name
                    var lines = line.Split(';');
                    if (!string.IsNullOrEmpty(lines[0]))
                    {
                        var validDate = DateTime.TryParse(lines[2], out DateTime result);

                        list.Add(new Tuple<string, string>(lines[0], 
                            (validDate == true) ? result.ToString() : DateTime.MinValue.ToString()));
                    }
                }
            }

            return list;
        }

        public List<string> GetFilesToCopy()
        {
            var filesAlreadyCopied = FilesAlreadyCopied();
            var directories = Directory.GetDirectories(_settings.DownloadPath, "*");

            var filestoCopy = new List<string>();
            string checkItem = "";

            Console.WriteLine($"Checking files to copy in '{_settings.DownloadPath}'");

            foreach (var directory in directories)
            {
                //get all the files in each directory
                var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    bool copyFile = true;

                    //check if can copy this file
                    var extension = Path.GetExtension(file);
                    var canCopyThisFile = CheckCopyThisFile(extension.Substring(1, extension.Length - 1));
                    
                    //dont bother checking if cant copy this type of file....
                    if (!canCopyThisFile)
                        continue;

                    //check that u dont copy sample avis
                    if (file.Contains("SAMPLE", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    //check if havent copied the file previously
                    checkItem = Path.GetFileName(file);
                    copyFile = !filesAlreadyCopied.Any(x => x.Item1.Contains(checkItem));

                    //add the movie to be copied
                    if (copyFile & !string.IsNullOrEmpty(checkItem))
                    {
                        filestoCopy.Add(file);
                    }
                }
            }

            if (filestoCopy.Count == 0)
            {
                Console.WriteLine($"No files to be processed.");
            }
            else
            {
                Console.WriteLine($"Found '{filestoCopy.Count}' files to be processed.");
            }

            return filestoCopy;
        }

        private bool CheckCopyThisFile(string fileExtension)
        {
            return _settings.Extensions.Any(x => x.Contains(fileExtension.ToUpper()));
        }

        //old code
        //public List<string> GetFilesToCopy()
        //{
        //    var filesAlreadyCopied = FilesAlreadyCopied();
        //    var filesInDownloadDir = Directory.GetDirectories(_settings.DownloadPath, "*");

        //    var filestoCopy = new List<string>();
        //    string checkItem = "";

        //    foreach (var item in filesInDownloadDir)
        //    {

        //        bool copyFile = true;

        //        checkItem = item.Replace(_settings.DownloadPath, "").Trim();

        //        //check if havent copied the file previously
        //        for (int i = 0; i < filesAlreadyCopied.Count; i++)
        //        {
        //            if (checkItem == filesAlreadyCopied[i].Trim())
        //            {
        //                copyFile = false;   //dont copy file
        //                break;
        //            }
        //        }

        //        //add the movie to be copied
        //        if (copyFile & !string.IsNullOrEmpty(checkItem))
        //        {
        //            filestoCopy.Add(checkItem);
        //        }
        //    }

        //    return filestoCopy;
        //}


    }
}
