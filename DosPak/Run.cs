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
            DosPakManager manager = new DosPakManager(testpath + "Textures.pak");
            
            PakInfo info = manager.PakArchiveInformation;
            System.Console.Write(info);

            List<String> files = new List<String>();
            files.Add(testpath + "test1.png");
            files.Add(testpath + "test2.png");
            files.Add(testpath + "\\test\\test2.png");
            manager.UpdateFiles(testpath, files, false);

            //manager.WritePakArchive(info);

            manager = new DosPakManager(testpath + "Textures.pak");
            info = manager.PakArchiveInformation;

            files.Clear();
            foreach (String file in manager.PakArchiveInformation.FileList.Keys)
            {
                if (manager.PakArchiveInformation.FileList[file].IndexArchiveFile == 2)
                {
                    files.Add(file);
                }
            }
            //manager.DeleteFiles(files);
            manager.ExtractFiles(files, testpath + "\\output");

            Console.Write(info);
            watch.Stop();
            Console.Write(watch.ElapsedMilliseconds);
            Console.ReadKey();
        }
    }
}
