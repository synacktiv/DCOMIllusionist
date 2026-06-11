using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Interop
{
    public enum HKEY : uint
    {
        HKEY_CLASSES_ROOT   = 0x80000000, // HKCR
        HKEY_CURRENT_USER   = 0x80000001, // HKCU
        HKEY_LOCAL_MACHINE  = 0x80000002, // HKLM
        HKEY_USERS          = 0x80000003, // HKU
        HKEY_CURRENT_CONFIG = 0x80000005, // HKCC
        HKEY_PERFORMANCE_DATA = 0x80000004 // Perf Data
    }
}
