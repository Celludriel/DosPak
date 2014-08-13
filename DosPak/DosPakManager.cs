using DosPak.Model;
using DosPak.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace DosPak
{
    public class DosPakManager
    {
        private static uint PADDING_BLOCK = 32768;
        private static uint FILERECORDSIZE = 272;
        private static uint HEADERSIZE = 21;
        private static uint MAXPAKFILESIZE = 1080655872;

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
            ReadPakArchiveInformation(pakArchiveFileName);
        }

        public byte[] GetFileAsStream(String fileName, bool decompress)
        {
            Model.FileInfo info = GetFileInfoFromArchive(fileName);
            string archiveFileName = BuildArchiveFileName(info.IndexArchiveFile, null);
            using (FileStream pakArchiveMemoryStream = new FileStream(archiveFileName, FileMode.Open))
            {
                using (BinaryReader pakReader = new BinaryReader(pakArchiveMemoryStream))
                {
                    //main archive needs to have a higher offset to take into account header and filetable
                    uint offset = FindOffsetForFileInfo(info);
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

        public void ExtractFiles(List<String> fileNames, String outputFolder)
        {
            foreach (String fileName in fileNames)
            {
                byte[] data = GetFileAsStream(fileName, true);
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                Util.ByteArrayToFile(System.IO.Path.Combine(outputFolder, fileName.Replace("\0", string.Empty)), data);
            }
        }
        
        public void DeleteFiles(List<String> fileNames)
        {
            PakInfo newPakInfo = Util.ClonePakInfo(this.PakArchiveInformation);
            foreach (String fileName in fileNames)
            {
                GetFileInfoFromArchive(fileName);
                newPakInfo.FileList.Remove(fileName);
            }

            RecalculateFileHeader(newPakInfo);
            WritePakArchive(newPakInfo);
            this.PakArchiveInformation = newPakInfo;
        }

        public void UpdateFiles(String basePath, List<String> fileNames, bool compress)
        {
            PakInfo newPakInfo = Util.ClonePakInfo(this.PakArchiveInformation);
            Dictionary<String, NewFileData> fileData = new Dictionary<string, NewFileData>();
            foreach (String fileName in fileNames)
            {
                if (File.Exists(fileName))
                {
                    NewFileData data = CreateNewFileData(basePath, compress, fileName);
                    DosPak.Model.FileInfo info = new DosPak.Model.FileInfo();

                    if (newPakInfo.FileList.ContainsKey(data.fileName.PadRight(DosPak.Model.FileInfo.MAX_PATH_SIZE, '\0')))
                    {
                        info = newPakInfo.FileList[data.fileName];
                        info.FileSize = data.fileSize;
                        info.CompressedFileSize = data.compressedSize;
                        newPakInfo.FileList[data.fileName] = info;
                    }
                    else
                    {
                        info.RelativeFilePath = data.fileName.PadRight(DosPak.Model.FileInfo.MAX_PATH_SIZE, '\0').ToCharArray();                    
                        info.FileSize = data.fileSize;
                        info.CompressedFileSize = data.compressedSize;
                        newPakInfo.FileList.Add(data.fileName, info);
                    }

                    fileData.Add(data.fileName, data);
                }
            }

            newPakInfo.NewFileList = fileData;
            RecalculateFileHeader(newPakInfo);
            WritePakArchive(newPakInfo);
            this.PakArchiveInformation = newPakInfo;
        }

        private void RecalculateFileHeader(PakInfo newPakInfo)
        {
            UInt32 lastArchiveIndex = RecalculateFileOffsets(newPakInfo);
            newPakInfo.Header.NoOfArchiveFiles = lastArchiveIndex + 1;
            newPakInfo.Header.NoOfFilesInArchive = (uint)newPakInfo.FileList.Count();
            newPakInfo.Header.DataSectionOffset = CalculateFullByteBlockSize(CalculateHeaderBlockSize(newPakInfo.Header.NoOfFilesInArchive));
            newPakInfo.Header.LengthFileTable = newPakInfo.Header.NoOfFilesInArchive * FILERECORDSIZE;
        }

        private static NewFileData CreateNewFileData(String basePath, bool compress, String fileName)
        {
            NewFileData data = new NewFileData();
            if (fileName.Contains(basePath))
            {
                data.fileName = fileName.Replace(basePath, "");
                if (data.fileName.StartsWith("\\"))
                {
                    data.fileName = data.fileName.Substring(1);
                }
                data.fileName = data.fileName.PadRight(DosPak.Model.FileInfo.MAX_PATH_SIZE, '\0');

                byte[] byteData = File.ReadAllBytes(fileName);
                data.fileSize = (UInt32)byteData.Length;
                if (compress)
                {
                    byteData = Util.Compress(byteData);
                    data.compressedSize = (UInt32)byteData.Length;
                }
                data.Data = byteData;
                return data;
            }
            else
            {
                throw new Exception("File did not belong to basepath " + basePath);
            }
        }        

        private UInt32 RecalculateFileOffsets(PakInfo pakInfo)
        {
            UInt32 currentPakFileSize = pakInfo.Header.DataSectionOffset;
            UInt32 offset = 0;
            UInt32 archiveIndex = 0;
            foreach (DosPak.Model.FileInfo info in pakInfo.FileList.Values)
            {
                info.IndexArchiveFile = archiveIndex;
                info.OffsetFileInArchive = offset;
                UInt32 fullBlockSize;
                if(info.CompressedFileSize > 0){
                    fullBlockSize = CalculateFullByteBlockSize((uint)info.CompressedFileSize);
                    offset = offset + fullBlockSize;
                }else{
                    fullBlockSize = CalculateFullByteBlockSize((uint)info.FileSize);
                    offset = offset + fullBlockSize;
                }

                if (offset > MAXPAKFILESIZE)
                {                    
                    info.OffsetFileInArchive = 0;
                    archiveIndex = archiveIndex + 1;
                    info.IndexArchiveFile = archiveIndex;                    
                    offset = 0 + fullBlockSize;
                }
            }
            return archiveIndex;
        }

        private void WritePakArchive(PakInfo pakInfo)
        {
            for (uint i = 0; i < pakInfo.Header.NoOfArchiveFiles; i++)
            {
                string archiveFileName = BuildArchiveFileName(i, null);
                string tmpArchiveFileName = BuildArchiveFileName(i, "tmp");
                try
                {
                    using (FileStream tmpArchiveMemoryStream = new FileStream(tmpArchiveFileName, FileMode.Create))
                    {
                        using (BinaryWriter pakWriter = new BinaryWriter(tmpArchiveMemoryStream))
                        {
                            if (i == 0)
                            {
                                WriteHeader(pakWriter, pakInfo.Header);
                                WriteFileInfo(pakWriter, pakInfo.FileList);
                                uint remainingBytesToDataOffset = pakInfo.Header.DataSectionOffset - CalculateHeaderBlockSize(pakInfo.Header.NoOfFilesInArchive);
                                pakWriter.Write(Util.CreatePaddingByteArray((int)remainingBytesToDataOffset));
                            }

                            foreach (String file in pakInfo.FileList.Keys)
                            {
                                DosPak.Model.FileInfo info = pakInfo.FileList[file];
                                if (info.IndexArchiveFile == i)
                                {
                                    byte[] fileData;
                                    if (pakInfo.NewFileList != null && pakInfo.NewFileList.ContainsKey(file))
                                    {
                                        fileData = pakInfo.NewFileList[file].Data;
                                        pakInfo.NewFileList.Remove(file);
                                    }
                                    else
                                    {
                                        fileData = GetFileAsStream(file, false);
                                    }
                                    int paddingSize = (int)CalculateFullByteBlockSize((uint)fileData.Length) - fileData.Length;
                                    pakWriter.Write(fileData);
                                    pakWriter.Write(Util.CreatePaddingByteArray(paddingSize));
                                }
                            }
                        }
                    }
                }
                finally
                {
                    bool success = false;
                    do
                    {
                        try
                        {
                            File.Replace(tmpArchiveFileName, archiveFileName, null);
                            success = true;
                        }
                        catch (Exception)
                        {
                            Thread.Sleep(3000);
                        }
                    } while (!success);
                }
            }
            DeleteLeftOverArchiveFiles(pakInfo);
        }

        private void DeleteLeftOverArchiveFiles(PakInfo pakInfo)
        {            
            if (this.PakArchiveInformation.Header.NoOfArchiveFiles > pakInfo.Header.NoOfArchiveFiles)
            {
                uint noOfFilesToDelete = this.PakArchiveInformation.Header.NoOfArchiveFiles - pakInfo.Header.NoOfArchiveFiles;
                uint fileIndex = pakInfo.Header.NoOfArchiveFiles;
                for (uint i = 0; i < noOfFilesToDelete; i++)
                {
                    bool success = false;
                    do
                    {
                        try
                        {
                            string archiveFileName = BuildArchiveFileName(fileIndex, null);
                            File.Delete(archiveFileName);
                            fileIndex++;
                            success = true;
                        }
                        catch (Exception)
                        {
                            Thread.Sleep(3000);
                        }
                    } while (!success);
                }
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

        private void ReadPakArchiveInformation(string pakArchiveFileName)
        {
            using (FileStream pakArchiveMemoryStream = new FileStream(pakArchiveFileName, FileMode.Open))
            {
                using (BinaryReader pakReader = new BinaryReader(pakArchiveMemoryStream))
                {
                    this.PakArchiveInformation = ReadPakArchiveInformation(pakReader);
                }
            }
        }

        private PakInfo ReadPakArchiveInformation(BinaryReader pakReader)
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

        private uint CalculateHeaderBlockSize(uint noOfFilesInArchive)
        {
            return HEADERSIZE + FILERECORDSIZE * noOfFilesInArchive;
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

        private uint FindOffsetForFileInfo(Model.FileInfo info)
        {
            return info.IndexArchiveFile > 0 ? info.OffsetFileInArchive : info.OffsetFileInArchive + PakArchiveInformation.Header.DataSectionOffset;
        }

        private Model.FileInfo GetFileInfoFromArchive(String fileName)
        {
            Model.FileInfo info = PakArchiveInformation.FileList[fileName];
            if (info == null)
            {
                throw new Exception("File not found in archive");
            }
            return info;
        }

    }
}
