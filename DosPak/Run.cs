using DosPak.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DosPak
{
    class Run
    {
        private static string testpath = "C:\\Git\\Repos\\DosPak\\DosPak\\TestData\\";
        
        static void Main()
        {
            DosPakManager manager = new DosPakManager(testpath + "testdata.pak");
            PakInfo info = manager.PakArchiveInformation;
            manager.ExtractFile(info.FileList.Keys.ElementAt(0), testpath);
        }
    }
}
