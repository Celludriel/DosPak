using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DosPak.Model
{
    class PakInfo
    {
        public Header header;
        public Dictionary<Char[], FileInfo> fileList;
    }
}
