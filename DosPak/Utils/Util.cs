using DosPak.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace DosPak.Utils
{
    public class Util
    {
        public static PakInfo ClonePakInfo(PakInfo source)
        {
            PakInfo target = new PakInfo();

            Header header = new Header();
            header.Version = source.Header.Version;
            header.DataSectionOffset = source.Header.DataSectionOffset;
            header.LengthFileTable = source.Header.LengthFileTable;
            header.Endianness = source.Header.Endianness;
            header.NoOfArchiveFiles = source.Header.NoOfArchiveFiles;
            header.NoOfFilesInArchive = source.Header.NoOfFilesInArchive;

            Dictionary<String, DosPak.Model.FileInfo> fileList = new Dictionary<string, Model.FileInfo>();
            foreach (String key in source.FileList.Keys)
            {
                DosPak.Model.FileInfo targetFileInfo = new DosPak.Model.FileInfo();
                DosPak.Model.FileInfo sourceFileInfo = source.FileList[key];

                targetFileInfo.RelativeFilePath = sourceFileInfo.RelativeFilePath;
                targetFileInfo.OffsetFileInArchive = sourceFileInfo.OffsetFileInArchive;
                targetFileInfo.FileSize = sourceFileInfo.FileSize;
                targetFileInfo.CompressedFileSize = sourceFileInfo.CompressedFileSize;
                targetFileInfo.IndexArchiveFile = sourceFileInfo.IndexArchiveFile;

                fileList.Add(key, targetFileInfo);
            }

            target.Header = header;
            target.FileList = fileList;
            return target;
        }

        public static byte[] CreatePaddingByteArray(int length)
        {
            var arr = new byte[length];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = 0x00;
            }
            return arr;
        }

        public static byte[] Decompress(byte[] gzip)
        {
            byte[] retValue;
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip),
                                  CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    retValue = memory.ToArray();
                }
            }
            return retValue;
        }

        public static byte[] Compress(byte[] gzip)
        {
            byte[] retValue;
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip),
                                  CompressionMode.Compress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    retValue = memory.ToArray();
                }
            }
            return retValue;
        }

        public static bool ByteArrayToFile(string fileName, byte[] byteArray)
        {
            try
            {
                string dirName = Path.GetDirectoryName(fileName);
                if(!Directory.Exists(dirName)){
                    Directory.CreateDirectory(dirName);
                }

                using(FileStream fileStream = new FileStream(fileName, FileMode.Create,FileAccess.Write)){
                    fileStream.Write(byteArray, 0, byteArray.Length);
                }

                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception caught in process: {0}",
                                  exception.ToString());
            }
            return false;
        }
    }
}
