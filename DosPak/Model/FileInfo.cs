using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DosPak.Model
{
    public class FileInfo
    {
        public static int MAX_PATH_SIZE = 256;

        public Char[] RelativeFilePath;
        public UInt32 OffsetFileInArchive;
        public UInt32 FileSize;
        public UInt32 CompressedFileSize;
        public UInt32 IndexArchiveFile;
    }
}
