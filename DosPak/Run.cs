﻿using DosPak.Model;
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
            DosPakManager manager = new DosPakManager(testpath + "Textures.pak");
            PakInfo info = manager.PakArchiveInformation;

            System.Console.Write(info);

            //manager.DeleteFile(info.FileList.Keys.ElementAt(0));
            manager.WritePakArchive(info);

            manager = new DosPakManager(testpath + "Textures.pak");
            info = manager.PakArchiveInformation;

            foreach (String file in manager.PakArchiveInformation.FileList.Keys)
            {
                manager.ExtractFile(file, testpath + "Output\\");
            }

            Console.Write(info);
            Console.ReadKey();
        }
    }
}
