using DosPak.Model;
using DosPak.Utils;
using MiscUtil.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DosPak
{
    public class DosPakManager
    {
        private PakInfo PakArchiveInformation;

        public DosPakManager(string pakArchiveFileName)
        {
            byte[] pakBinaryFileContent = File.ReadAllBytes(pakArchiveFileName);
            using (MemoryStream pakArchiveMemoryStream = new MemoryStream(pakBinaryFileContent))
            {
                using (EndianBinaryReader pakReader = Util.CreateEndianBinaryReaderForStream(pakArchiveMemoryStream))
                {
                    PakArchiveInformation = LoadPakArchiveInformation(pakReader);
                }
            }
        }

        private PakInfo LoadPakArchiveInformation(EndianBinaryReader pakReader)
        {

            PakInfo pakInfo = new PakInfo();
            pakInfo.Header = ReadHeaderInfo(pakReader);
            pakInfo.FileList = ReadFileListInfo(pakReader, pakInfo.Header.NoOfFilesInArchive);
            return pakInfo;
        }

        private Header ReadHeaderInfo(EndianBinaryReader pakReader)
        {
            Header header = new Header();
            header.Version = pakReader.ReadUInt32();
            header.DataSectionOffset = pakReader.ReadUInt32();
            header.NoOfArchiveFiles = pakReader.ReadUInt32();
            header.LengthFileTable = pakReader.ReadUInt32();
            header.Endianness = pakReader.ReadBoolean();
            header.NoOfFilesInArchive = pakReader.ReadUInt32();
            return header;
        }

        private Dictionary<char[], Model.FileInfo> ReadFileListInfo(EndianBinaryReader pakReader, UInt32 noOfFiles)
        {
            Dictionary<char[], Model.FileInfo> fileListInfoDictionary = new Dictionary<char[], DosPak.Model.FileInfo>();

            for (int i = 0; i < noOfFiles; i++)
            {
                DosPak.Model.FileInfo info = ReadFileInfo(pakReader);
                fileListInfoDictionary.Add(info.RelativeFilePath, info);
            }

            return fileListInfoDictionary;
        }

        private DosPak.Model.FileInfo ReadFileInfo(EndianBinaryReader pakReader)
        {
            DosPak.Model.FileInfo info = new DosPak.Model.FileInfo();
            info.RelativeFilePath = System.Text.Encoding.UTF8.GetString(pakReader.ReadBytes(256)).ToCharArray();
            info.OffsetFileInArchive = pakReader.ReadUInt32();
            info.FileSize = pakReader.ReadUInt32();
            info.CompressedFileSize = pakReader.ReadUInt32();
            info.IndexArchiveFile = pakReader.ReadUInt32();
            return info;
        }
    }
}
