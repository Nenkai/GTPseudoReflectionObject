using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using Syroot.BinaryData;
namespace GTStandardDefinitionEditor.Utils
{
    public static class MiscExtensions
    {
        public static void AlignWithValue(this BinaryStream bs, int alignment, byte val)
        {
            long pos = bs.Position;
            bs.Align(alignment, true);
            long endPos = bs.Position;
            bs.Position = pos;
            for (int i = 0; i < endPos - pos; i++)
                bs.WriteByte(val);
        }
    }
}
