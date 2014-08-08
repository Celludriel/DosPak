using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DosPak.Model
{
    class Header
    {
        public UInt32 Version;
        public UInt32 DataSectionOffset;
        public UInt32 NoOfArchiveFiles;
        public UInt32 LengthFileTable;
        public bool Endianness;
        public UInt32 NoOfFilesInArchive;
    }
}
