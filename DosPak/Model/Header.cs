using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DosPak.Model
{
    public class Header
    {
        public UInt32 Version;
        public UInt32 DataSectionOffset;
        public UInt32 NoOfArchiveFiles;
        public UInt32 LengthFileTable;
        public bool Endianness;
        public UInt32 NoOfFilesInArchive;

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Version: " + this.Version + "\n");
            builder.Append("DataSectionOffset: " + this.DataSectionOffset + "\n");
            builder.Append("NoOfArchiveFiles: " + this.NoOfArchiveFiles + "\n");
            builder.Append("LengthFileTable: " + this.LengthFileTable + "\n");
            builder.Append("Endianness: " + this.Endianness + "\n");
            builder.Append("NoOfFilesInArchive: " + this.NoOfFilesInArchive + "\n");
            return builder.ToString();

        }
    }
}
