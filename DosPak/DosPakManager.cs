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
        public static uint PADDING_BLOCK = 32768;
        public static uint PADDING = 173;
        public static uint READBUFFER = 1024;
        public static uint FILERECORDSIZE = 272;
        public static uint HEADERSIZE = 21;

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
            LoadPakArchiveInformation(pakArchiveFileName);
        }

        public void WritePakArchive(PakInfo pakInfo)
        {
            for (uint i = 0; i < pakInfo.Header.NoOfArchiveFiles; i++)
            {
                string archiveFileName = BuildArchiveFileName(i, null);
                string tmpArchiveFileName = BuildArchiveFileName(i, "tmp");
                using (FileStream tmpArchiveMemoryStream = new FileStream(tmpArchiveFileName, FileMode.Create))
                {
                    using (BinaryWriter pakWriter = new BinaryWriter(tmpArchiveMemoryStream))
                    {
                        if (i == 0)
                        {
                            WriteHeader(pakWriter, pakInfo.Header);
                            WriteFileInfo(pakWriter, pakInfo.FileList);
                            uint remainingBytesToDataOffset = pakInfo.Header.DataSectionOffset - CalculateHeaderBlockSize(pakInfo.Header);
                            pakWriter.Write(Util.CreatePaddingByteArray((int)remainingBytesToDataOffset));
                        }

                        foreach (String file in pakInfo.FileList.Keys)
                        {
                            DosPak.Model.FileInfo info = pakInfo.FileList[file];
                            if (info.IndexArchiveFile == i)
                            {
                                Console.WriteLine("Writing " + file);
                                byte[] fileData = GetFileAsStream(file, false);
                                int paddingSize = (int)CalculateFullByteBlockSize((uint)fileData.Length) - fileData.Length;
                                pakWriter.Write(fileData);
                                pakWriter.Write(Util.CreatePaddingByteArray(paddingSize));
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                File.Delete(archiveFileName);
                File.Move(tmpArchiveFileName, archiveFileName);
            }
        }

        public void DeleteFile(String fileName)
        {
            FileExistInArchive(fileName);

            PakInfo newPakInfo = Util.ClonePakInfo(this.PakArchiveInformation);
            newPakInfo.FileList.Remove(fileName);
            Dictionary<String, DosPak.Model.FileInfo>.KeyCollection filesInArchive = newPakInfo.FileList.Keys;
            newPakInfo.Header.NoOfFilesInArchive = (uint)filesInArchive.Count;
            newPakInfo.Header.LengthFileTable = newPakInfo.Header.NoOfFilesInArchive * FILERECORDSIZE;
            uint headerBlock = CalculateHeaderBlockSize(newPakInfo.Header);
            newPakInfo.Header.DataSectionOffset = CalculateFullByteBlockSize(headerBlock);

            UpdateFileOffsetsInFileTable(newPakInfo);
            WritePakArchive(newPakInfo);
        }

        public void ExtractFile(String fileName, String outputFolder)
        {
            byte[] data = GetFileAsStream(fileName, true);
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            fileName = System.IO.Path.Combine(outputFolder, fileName.Replace("\0", string.Empty));
            Util.ByteArrayToFile(fileName, data);
        }

        public byte[] GetFileAsStream(String fileName, bool decompress)
        {
            Model.FileInfo info = FileExistInArchive(fileName);
            string archiveFileName = BuildArchiveFileName(info.IndexArchiveFile, null);
            using (FileStream pakArchiveMemoryStream = new FileStream(archiveFileName, FileMode.Open))
            {
                using (BinaryReader pakReader = new BinaryReader(pakArchiveMemoryStream))
                {
                    //main archive needs to have a higher offset to take into account header and filetable
                    uint offset = FindOffsetFile(info);
                    pakReader.BaseStream.Seek(offset, SeekOrigin.Begin);
                    byte[] fileContent = pakReader.ReadBytes((int)info.FileSize);
                    if (info.CompressedFileSize == 0 || !decompress)
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

        private void LoadPakArchiveInformation(string pakArchiveFileName)
        {
            using (FileStream pakArchiveMemoryStream = new FileStream(pakArchiveFileName, FileMode.Open))
            {
                using (BinaryReader pakReader = new BinaryReader(pakArchiveMemoryStream))
                {
                    this.PakArchiveInformation = LoadPakArchiveInformation(pakReader);
                }
            }
        }

        private void UpdateFileOffsetsInFileTable(PakInfo pakInfo)
        {
            uint offset = 0;
            uint archiveIndex = 0;
            foreach (DosPak.Model.FileInfo info  in pakInfo.FileList.Values)
            {
                if (info.IndexArchiveFile > archiveIndex)
                {
                    archiveIndex = info.IndexArchiveFile;
                    offset = 0;
                }

                info.OffsetFileInArchive = offset;
                if (info.CompressedFileSize > 0)
                {
                    offset = offset + CalculateFullByteBlockSize(info.CompressedFileSize);
                }
                else
                {
                    offset = offset + CalculateFullByteBlockSize(info.FileSize);
                }
            }
        }

        private uint CalculateHeaderBlockSize(Header header)
        {
            return HEADERSIZE + FILERECORDSIZE * header.NoOfFilesInArchive;
        }

        private void WriteFileInfo(BinaryWriter pakWriter, Dictionary<String, DosPak.Model.FileInfo> fileList)
        {
            foreach (DosPak.Model.FileInfo info in fileList.Values)
            {
                pakWriter.Write(info.RelativeFilePath);
                pakWriter.Write(info.OffsetFileInArchive);
                pakWriter.Write(info.FileSize);
                pakWriter.Write(info.CompressedFileSize);
                pakWriter.Write(info.IndexArchiveFile);
            }
        }

        private void WriteHeader(BinaryWriter pakWriter, Header header)
        {
            pakWriter.Write(header.Version);
            pakWriter.Write(header.DataSectionOffset);
            pakWriter.Write(header.NoOfArchiveFiles);
            pakWriter.Write(header.LengthFileTable);
            pakWriter.Write(header.Endianness);
            pakWriter.Write(header.NoOfFilesInArchive);
        }

        private static uint CalculateFullByteBlockSize(uint sizeToAddPaddingTo)
        {
            return (UInt32)(Math.Ceiling(sizeToAddPaddingTo / (double)PADDING_BLOCK) * PADDING_BLOCK);
        }

        private string BuildArchiveFileName(uint indexOfArchive, String tmpSuffix)
        {
            if (tmpSuffix != null)
            {
                return indexOfArchive > 0 ? this.PakPath + Path.DirectorySeparatorChar + this.PakArchiveName + "_" + indexOfArchive + this.PakExtention + "." + tmpSuffix : this.PakPath + Path.DirectorySeparatorChar + this.PakArchiveName + this.PakExtention + "." + tmpSuffix;
            }
            return indexOfArchive > 0 ? this.PakPath + Path.DirectorySeparatorChar + this.PakArchiveName + "_" + indexOfArchive + this.PakExtention : this.PakPath + Path.DirectorySeparatorChar + this.PakArchiveName + this.PakExtention;
        }

        private uint FindOffsetFile(Model.FileInfo info)
        {
            return info.IndexArchiveFile > 0 ? info.OffsetFileInArchive : info.OffsetFileInArchive + PakArchiveInformation.Header.DataSectionOffset;
        }

        private Model.FileInfo FileExistInArchive(String fileName)
        {
            Model.FileInfo info = PakArchiveInformation.FileList[fileName];
            if (info == null)
            {
                throw new Exception("File not found in archive");
            }
            return info;
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
