using MiscUtil.Conversion;
using MiscUtil.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DosPak.Utils
{
    public class Util
    {
        public static EndianBinaryReader CreateEndianBinaryReaderForStream(Stream stream)
        {
            EndianBitConverter endian = GetEndian();
            EndianBinaryReader reader = new EndianBinaryReader(endian, stream);
            return reader;
        }

        public static EndianBinaryWriter CreateEndianBinaryWriterForStream(Stream stream)
        {
            EndianBitConverter endian = GetEndian();
            EndianBinaryWriter writer = new EndianBinaryWriter(endian, stream);
            return writer;
        }

        private static EndianBitConverter GetEndian()
        {
            EndianBitConverter endian = EndianBitConverter.Big;
            if (BitConverter.IsLittleEndian)
            {
                endian = EndianBitConverter.Little;
            }
            return endian;
        }
    }
}
