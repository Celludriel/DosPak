using DosPak.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DosPak
{
    class Run
    {
        private static string testpath = "C:\\Git\\Repos\\DosPak\\DosPak\\TestData\\";
        
        static void Main()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            DosPakManager manager = new DosPakManager(testpath + "testdata.pak");
            PakInfo info = manager.PakArchiveInformation;

            System.Console.Write(info);

            List<String> files = new List<String>();
            files.Add(testpath + "test1.png");
            files.Add(testpath + "test2.png");
            manager.UpdateFiles(files, false);

            //manager.WritePakArchive(info);

            manager = new DosPakManager(testpath + "testdata.pak");
            info = manager.PakArchiveInformation;

            files.Clear();
            foreach (String file in manager.PakArchiveInformation.FileList.Keys)
            {
                files.Add(file);
            }
            manager.ExtractFiles(files, testpath + "Output\\");

            Console.Write(info);
            watch.Stop();
            Console.Write(watch.ElapsedMilliseconds);
            Console.ReadKey();
        }
    }
}
