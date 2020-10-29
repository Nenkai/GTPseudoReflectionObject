using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTStandardDefinitionEditor.Entities
{
    public class SDEFDataCategory
    {
        public string Name { get; set; }
        public List<SDEFDataEntry> Entries = new List<SDEFDataEntry>();

        public override string ToString()
            => $"{Name} ({Entries.Count} entries)";
    }
}
