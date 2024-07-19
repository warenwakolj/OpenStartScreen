using System.Collections.Generic;

namespace OpenStartScreen
{
    public class ProgramCategory
    {
        public string Name { get; set; }
        public List<string> Items { get; set; }

        public ProgramCategory(string name)
        {
            Name = name;
            Items = new List<string>();
        }
    }
}
