using System;
using System.Collections.Generic;
using System.Text;

namespace MovieManager.Logic
{
    public static class globals
    {
        public const string FileCopyName = "_copiedFiles.txt";
    }


    public enum FileType
    {
        Movie = 1,
        TVSeries = 2,
        MusicFile = 3,
        Subtitle = 4
    }
}
