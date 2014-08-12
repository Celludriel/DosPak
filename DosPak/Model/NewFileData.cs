using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DosPak.Model
{
    public class NewFileData
    {
        public string fileName;
        public byte[] Data;
        public UInt32 fileSize;
        public UInt32 compressedSize;
    }
}
