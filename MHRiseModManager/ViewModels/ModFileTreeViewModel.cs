using MHRiseModManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHRiseModManager.ViewModels
{
    public class ModFileTreeViewModel
    {
        public List<ModFileTree> ModTree { get; set; }
        public ModFileTreeViewModel()
        {
            ModTree = new List<ModFileTree>();

            ModTree = new List<ModFileTree>()
            {
                new ModFileTree()
                {
                    Name = "test1",
                    Child = new List<ModFileTree>()
                    {
                        new ModFileTree() { Name = "test1-1" },
                        new ModFileTree() { Name = "test1-2" },
                        new ModFileTree() { Name = "test1-3" },
                    }
                },
                new ModFileTree()
                {
                    Name = "test2",
                    Child = new List<ModFileTree>()
                    {
                        new ModFileTree() { Name = "test2-1" },
                        new ModFileTree()
                        {
                            Name = "test2-2",
                            Child = new List<ModFileTree>()
                            {
                                new ModFileTree() { Name = "test2-2-1" },
                                new ModFileTree() { Name = "test2-2-2" }
                            }
                        }
                    }
                }
            };
        }
    }
}
