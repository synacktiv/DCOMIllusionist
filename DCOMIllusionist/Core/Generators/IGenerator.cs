using DCOMIllusionist.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Generators
{
    public interface IGenerator
    {
        object GenerateExec(InputArgs arg);

        object GenerateLoadDLL(InputArgs arg);

        object GenerateCurl(InputArgs arg);

        object GenerateB64Ysoserial(InputArgs arg);

        object GenerateFileWrite(InputArgs arg);

        List<ExploitType> SupportedExploits();

        Boolean IsExploitSupported(ExploitType exploit);
    }

    public enum ExploitType
    {
        Exec,
        LoadDLL,
        Curl,
        B64Ysoserial,
        FileWrite,
    }
}
