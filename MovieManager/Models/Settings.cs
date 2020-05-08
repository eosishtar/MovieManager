﻿using System.Collections.Generic;

namespace MovieManager.Models
{
    public class Settings
    {
        public string DownloadPath { get; set; }

        public string CompletedMoviePath { get; set; }
        public string CompletedTVPath { get; set; }

        public List<string> Extensions { get; set; }

        public string MovieDbApiKey { get; set; }

        public string MovieDbServerUrl { get; set; }

    }

}