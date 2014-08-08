using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DosPak.Model
{
    public class PakInfo
    {
        public Header Header;
        public Dictionary<String, FileInfo> FileList;
    }
}
