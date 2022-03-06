using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHRiseModManager.Models
{
    public class ModFileTree
    {
        public string Name { get; set; }

        public string Path { get; set; }
        public List<ModFileTree> Child { get; set; }
        public bool HasChild => Child != null && Child.Any();
    }
}
