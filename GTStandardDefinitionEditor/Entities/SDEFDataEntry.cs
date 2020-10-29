using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTStandardDefinitionEditor.Utils;
namespace GTStandardDefinitionEditor.Entities
{
    public class SDEFMetaDataEntry
    {
        public string Name { get; set; }
        public ushort TypeOrIndex { get; set; }
        public bool HasCustomType { get; set; }

        public ushort ArrayCategoryIndex { get; set; }
        public bool ArrayHasCustomType { get; set; }
        public uint ArrayLength { get; set; }

        public override string ToString()
            => Name;
    }

}
