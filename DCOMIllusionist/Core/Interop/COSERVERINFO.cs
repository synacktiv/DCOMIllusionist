using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public sealed class COSERVERINFO : IDisposable
    {
        int dwReserved1;
        [MarshalAs(UnmanagedType.LPWStr)]
        string pwszName;
        IntPtr pAuthInfo;
        int dwReserved2;

        void IDisposable.Dispose()
        {
            if (pAuthInfo != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pAuthInfo);
            }
        }

        public COSERVERINFO(string name)
        {
            pwszName = name;
        }
    }
}
