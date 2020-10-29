using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTStandardDefinitionEditor.Entities
{
    public class SDEFMetaDataCategory
    {
        public string Name { get; set; }
        public List<SDEFMetaDataEntry> Entries = new List<SDEFMetaDataEntry>();

        public override string ToString()
            => $"{Name} ({Entries.Count} entries)";
    }
}
