using DosPak.Model;
using DosPak.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace DosPak
{
    public class DosPakManager
    {
        private String PakArchiveName = "";
        private String PakExtention = "";
        private String PakPath = "";
        public PakInfo PakArchiveInformation;

        public DosPakManager(string pakArchiveFileName)
        {
            this.PakPath = Path.GetDirectoryName(pakArchiveFileName);
            string filename = Path.GetFileName(pakArchiveFileName);
            this.PakArchiveName = Path.GetFileNameWithoutExtension(filename);
            this.PakExtention = Path.GetExtension(filename);
            using (FileStream pakArchiveMemoryStream = new FileStream(pakArchiveFileName, FileMode.Open))
            {
                using (BinaryReader pakReader = new BinaryReader(pakArchiveMemoryStream))
                {
                    this.PakArchiveInformation = LoadPakArchiveInformation(pakReader);
                }
            }
        }

        public void ExtractFile(String fileName, String outputFolder)
        {
            byte[] data = GetFileAsStream(fileName);
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            fileName = System.IO.Path.Combine(outputFolder, fileName.Replace("\0", string.Empty));
            Util.ByteArrayToFile(fileName, data);
        }

        public byte[] GetFileAsStream(String fileName)
        {
            Model.FileInfo info = PakArchiveInformation.FileList[fileName];
            if (info != null)
            {
                string archiveFileName = info.IndexArchiveFile > 0 ? this.PakPath + Path.DirectorySeparatorChar + this.PakArchiveName + "_" + info.IndexArchiveFile + this.PakExtention : this.PakPath + Path.DirectorySeparatorChar + this.PakArchiveName + this.PakExtention;
                using (FileStream pakArchiveMemoryStream = new FileStream(archiveFileName, FileMode.Open))
                {
                    using (BinaryReader pakReader = new BinaryReader(pakArchiveMemoryStream))
                    {
                        //main archive needs to have a higher offset to take into account header and filetable
                        uint offset = info.IndexArchiveFile > 0 ? info.OffsetFileInArchive : info.OffsetFileInArchive + PakArchiveInformation.Header.DataSectionOffset;
                        pakReader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        byte[] fileContent = pakReader.ReadBytes((int)info.FileSize);
                        if (info.CompressedFileSize == 0)
                        {
                            return fileContent;
                        }
                        else
                        {
                            return Util.Decompress(fileContent);
                        }
                    }
                }
            }
            else
            {
                throw new Exception("File not found in archive");
            }
        }

        private PakInfo LoadPakArchiveInformation(BinaryReader pakReader)
        {

            PakInfo pakInfo = new PakInfo();
            pakInfo.Header = ReadHeaderInfo(pakReader);
            pakInfo.FileList = ReadFileListInfo(pakReader, pakInfo.Header.NoOfFilesInArchive);
            return pakInfo;
        }

        private Header ReadHeaderInfo(BinaryReader pakReader)
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

        private Dictionary<String, Model.FileInfo> ReadFileListInfo(BinaryReader pakReader, UInt32 noOfFiles)
        {
            Dictionary<String, Model.FileInfo> fileListInfoDictionary = new Dictionary<String, DosPak.Model.FileInfo>();

            for (int i = 0; i < noOfFiles; i++)
            {
                DosPak.Model.FileInfo info = ReadFileInfo(pakReader);
                fileListInfoDictionary.Add(new String(info.RelativeFilePath), info);
            }

            return fileListInfoDictionary;
        }

        private DosPak.Model.FileInfo ReadFileInfo(BinaryReader pakReader)
        {
            DosPak.Model.FileInfo info = new DosPak.Model.FileInfo();
            info.RelativeFilePath = pakReader.ReadChars(DosPak.Model.FileInfo.MAX_PATH_SIZE);
            info.OffsetFileInArchive = pakReader.ReadUInt32();
            info.FileSize = pakReader.ReadUInt32();
            info.CompressedFileSize = pakReader.ReadUInt32();
            info.IndexArchiveFile = pakReader.ReadUInt32();
            return info;
        }
    }
}
