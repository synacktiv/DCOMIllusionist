using DCOMIllusionist.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Generators
{
    public abstract class GenericGenerator : IGenerator
    {
        public virtual object GenerateB64Ysoserial(InputArgs arg)
        {
            throw new NotImplementedException();
        }

        public virtual object GenerateCurl(InputArgs arg)
        {
            throw new NotImplementedException();
        }

        public virtual object GenerateLoadDLL(InputArgs arg)
        {
            throw new NotImplementedException();
        }

        public virtual object GenerateExec(InputArgs arg)
        {
            throw new NotImplementedException();
        }

        public virtual object GenerateFileWrite(InputArgs arg)
        {
            throw new NotImplementedException();
        }

        public bool IsExploitSupported(ExploitType exploit)
        {
            var exploits = SupportedExploits();
            return exploits.Contains(exploit);
        }

        public abstract List<ExploitType> SupportedExploits();
    }
}
