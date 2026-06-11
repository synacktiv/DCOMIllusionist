using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Helpers
{
    public class InputArgs
    {
        public string Cmd { get; set; }
        public string Input { get; set; }
        public string DLLClass { get; set; }
        public string DLLMethod { get; set; } = "Run";
        public string FileWriteDst { get; set; }
    }
}
