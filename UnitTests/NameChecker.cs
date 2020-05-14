using MovieManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;
using MovieManager.Logic;

namespace UnitTests
{
    [TestClass]
    public class NameChecker
    {
        [TestMethod]
        public void MovieOrTVSeriesNameChecker()
        {
            var fileList = GetListToBeSorted();

            foreach (var item in fileList)
            {
                var result = Helper.CheckIfMovieOrTvSeries(item.Item1);
                Assert.AreEqual((int)item.Item2, result, $"Error: Did not identify correctly '{item.Item1}'");
            }
        }

        private List<Tuple<string, FileType>> GetListToBeSorted()
        {
            //add the list of file names here to check the logic
            var list = new List<Tuple<string, FileType>>();

            list.Add(new Tuple<string, FileType>("21 Bridges.2019.HC.HDRip.XviD.AC3 - EVO", FileType.Movie));
            list.Add(new Tuple<string, FileType>("2050.2019.HDRip.XviD.AC3-EVO", FileType.Movie));
            list.Add(new Tuple<string, FileType>("2050.2019.HDRip.XviD.AC3-EVO - Copy", FileType.Movie));
            list.Add(new Tuple<string, FileType>("Dark.Waters.2019.SCR.XviD.AC3-EVO", FileType.Movie));
            list.Add(new Tuple<string, FileType>("The.Murder.Of.Nicole.Brown.Simpson.2019.HDRip.XviD.AC3-EVO", FileType.Movie));
            list.Add(new Tuple<string, FileType>("The.Windermere.Children.2020.HDRip.AC3.x264-CMRG", FileType.Movie));

            list.Add(new Tuple<string, FileType>("Blindspot.S04E09.XviD-AFG", FileType.TVSeries));
            list.Add(new Tuple<string, FileType>("Blindspot.S04E13.XviD-AFG", FileType.TVSeries));
            list.Add(new Tuple<string, FileType>("The.Witcher.S01E08.INTERNAL.XviD-AFG", FileType.TVSeries));

            list.Add(new Tuple<string, FileType>("The.Vampire.Diaries.S03E13.HDTV.XviD-LOL.avi ", FileType.TVSeries));
            list.Add(new Tuple<string, FileType>("Blade Runner 2049 (2017)", FileType.Movie));

            return list;
        }
    }
}
