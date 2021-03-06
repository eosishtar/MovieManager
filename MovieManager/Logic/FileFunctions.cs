﻿using MovieManager.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//It handles the conversion and formatting. https://github.com/omar/ByteSize
using ByteSizeLib;

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
            if (!Directory.Exists(_settings.DownloadPath))
            {
                Directory.CreateDirectory(_settings.DownloadPath);
            }

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

            if (directories.Count() == 0)
            {
                Console.WriteLine($"There are no items in '{_settings.DownloadPath}'");
                return new List<string>();
            }

            var filestoCopy = new List<string>();

            Console.WriteLine($"Checking files to copy in '{_settings.DownloadPath}'");

            foreach (var directory in directories)
            {
                //get all the files in each directory
                var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    bool copyFile = true;

                    //check if can copy this file
                    var fi = new FileInfo(file);
                    if(!CheckCopyThisFile(fi))
                        continue;

                    copyFile = !filesAlreadyCopied.Any(x => x.Item1
                        .Contains(fi.Name, StringComparison.InvariantCultureIgnoreCase));

                    //add the file to be copied
                    if (copyFile & !string.IsNullOrEmpty(fi.Name))
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

        private bool CheckCopyThisFile(FileInfo fileInfo)
        {
            //check that u dont copy sample avis
            if (_settings.SampleVideoDelete)
            {
                if (fileInfo.Name.Contains("SAMPLE", StringComparison.InvariantCultureIgnoreCase))
                    return false;

                var filesize = ByteSize.FromBytes(fileInfo.Length);
                if (filesize.MegaBytes < Convert.ToInt64(_settings.SampleSizeLimit) && fileInfo.Extension.ToUpper() == ".AVI")
                {
                    return false;
                }
            }

            string formattedExt = fileInfo.Extension.Replace(".", "").Trim();

            return _settings.Extensions.Any(x => x.Contains(formattedExt.ToUpper()));
        }

        public List<DuplicateItemModel> GetDuplicatesCopied()
        {
            List<string> directoriesToSearch = new List<string>
            {
                _settings.CompletedMoviePath,
                _settings.CompletedTVPath
            };

            var dupItems = new List<DuplicateItemModel>();
            var uniqueFileName = new SortedList();

            foreach (var dir in directoriesToSearch)
            {
                //get all the files in each directory
                var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var fi = new FileInfo(file);

                    if (!CheckCopyThisFile(fi))
                    {
                        Helper.DeleteFile(fi.FullName, true);
                    }
                    
                    try
                    {
                        //using a sorted list should guarentee unique file names
                        uniqueFileName.Add(fi.Name, fi.FullName);
                    }
                    catch (Exception ex)
                    {
                        dupItems.Add(new DuplicateItemModel
                        {
                            DuplicateName = fi.Name,
                            FullPath1  = fi.FullName,
                            // need to find this in the sortedList
                            FullPath2 = uniqueFileName.GetByIndex(uniqueFileName.IndexOfKey(fi.Name)).ToString(),
                        });
                    }
                }
            }

            return dupItems;
        }

    }
}
