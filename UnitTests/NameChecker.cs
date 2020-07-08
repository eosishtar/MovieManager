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
                var result = Helper.IdentifyFileType(item.Item1);
                Assert.AreEqual((int)item.Item2, result, $"Error: Did not identify correctly '{item.Item1}'");
            }
        }

        private List<Tuple<string, FileType>> GetListToBeSorted()
        {
            //add the list of file names here to check the logic
            var list = new List<Tuple<string, FileType>>();
            list.Add(new Tuple<string, FileType>("Big.Driver.2014.DVDRip.XviD.AC3.RoSubbed - playXD.srt", FileType.Subtitle));

            list.Add(new Tuple<string, FileType>("21 Bridges.2019.HC.HDRip.XviD.AC3 - EVO.avi", FileType.Movie));
            list.Add(new Tuple<string, FileType>("2050.2019.HDRip.XviD.AC3-EVO.avi", FileType.Movie));
            list.Add(new Tuple<string, FileType>("2050.2019.HDRip.XviD.AC3-EVO - Copy.avi", FileType.Movie));
            list.Add(new Tuple<string, FileType>("Dark.Waters.2019.SCR.XviD.AC3-EVO.avi", FileType.Movie));
            list.Add(new Tuple<string, FileType>("The.Murder.Of.Nicole.Brown.Simpson.2019.HDRip.XviD.AC3-EVO.mkv", FileType.Movie));
            list.Add(new Tuple<string, FileType>("The.Windermere.Children.2020.HDRip.AC3.x264-CMRG.avi", FileType.Movie));
            list.Add(new Tuple<string, FileType>("Blindspot.S04E09.XviD-AFG.avi", FileType.TVSeries));
            list.Add(new Tuple<string, FileType>("Blindspot.S04E13.XviD-AFG.avi", FileType.TVSeries));
            list.Add(new Tuple<string, FileType>("The.Witcher.S01E08.INTERNAL.XviD-AFG.avi", FileType.TVSeries));
            list.Add(new Tuple<string, FileType>("The.Vampire.Diaries.S03E13.HDTV.XviD-LOL.avi ", FileType.TVSeries));
            list.Add(new Tuple<string, FileType>("Blade Runner 2049 (2017)a.vi", FileType.Movie));
            list.Add(new Tuple<string, FileType>("In.the.Shadow.of.the.Moon.2019.HDRip.XviD.AC3 - EVOa.mkv", FileType.Movie));
            list.Add(new Tuple<string, FileType>("Lion.King.2019.DVDRip.XviD.AC3 - EVO.mkv", FileType.Movie));
            list.Add(new Tuple<string, FileType>("Paw Patrol Marshall & Chase on the Case 2015 DVDRip - PoundPuppy.avi", FileType.Movie));
            list.Add(new Tuple<string, FileType>("Big.Little.Lies.S01E01.480p.BluRay.nSD.x264 - NhaNc3.avi", FileType.TVSeries));
            list.Add(new Tuple<string, FileType>("The Angry Birds Movie 2.2019.HC.HDRip.XviD.AC3 - EVO.avi", FileType.Movie));
            list.Add(new Tuple<string, FileType>("Big.Little.Lies.S01E01.480p.BluRay.nSD.x264 - NhaNc3.mkv", FileType.TVSeries));
            list.Add(new Tuple<string, FileType>("Billions.S01E04.480p.BluRay.nSD.x264 - NhaNc3.mkv", FileType.TVSeries));
            list.Add(new Tuple<string, FileType>("Billions.S01E08.480p.BluRay.nSD.x264 - NhaNc3.mkv", FileType.TVSeries));
            list.Add(new Tuple<string, FileType>("Billions.S03E12.480p.WEB.nSD.x264 - NhaNc3.mkv", FileType.TVSeries));
            list.Add(new Tuple<string, FileType>("Dora and the Lost City of Gold.2019.HDRip.XviD.AC3 - EVO.avi", FileType.Movie));
            list.Add(new Tuple<string, FileType>("El.Camino.A.Breaking.Bad.Movie.2019.HDRip.XviD.AC3 - EVO.avi", FileType.Movie));
            list.Add(new Tuple<string, FileType>("Family.Guy.s01e03.Chitty.Chitty.Death.Bang.XviD - SChiZO.avi", FileType.TVSeries));
            list.Add(new Tuple<string, FileType>("Family.Guy.s01e05.A.Hero.Sits.Next.Door.XviD - SChiZO.avi", FileType.TVSeries));
            list.Add(new Tuple<string, FileType>("40 Days and 40 Nights(2002).mkv", FileType.Subtitle));
            list.Add(new Tuple<string, FileType>("40D.and.40N8s.2002.DvDRip.x264 - WiNTeaM.srt", FileType.Subtitle));
            list.Add(new Tuple<string, FileType>("Captain America -The First Avenger(2011).mp4", FileType.Subtitle));
            list.Add(new Tuple<string, FileType>("Captain America Civil War(2016).mp4", FileType.Movie));

            return list;
        }
    }
}
