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

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Header.ToString());

            foreach (String key in FileList.Keys)
            {
                builder.Append(FileList[key]);
            }

            return builder.ToString();

        }
    }
}
