using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Interop
{
    public enum REGSAM : uint
    {
        QUERY_VALUE = 0x0001,
        SET_VALUE = 0x0002,
        CREATE_SUB_KEY = 0x0004,
        ENUMERATE_SUB_KEYS = 0x0008,
        READ_CONTROL = 0x00020000,
        WRITE_DAC = 0x00040000,
        WRITE_OWNER = 0x00080000,
        KEY_ALL_ACCESS = 0xF003F
    }
}
