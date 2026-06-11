using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Interop.Interfaces
{
    [Guid("000001B9-0000-0000-c000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpecialSystemPropertiesActivator
    {
        void SetSessionId(int dwSessionId, int bUseConsole, int fRemoteThisSessionId);

        void GetSessionId(out int pdwSessionId, out int pbUseConsole);

        void GetSessionId2(out int pdwSessionId, out int pbUseConsole, out int pfRemoteThisSessionId);

        void SetClientImpersonating(int fClientImpersonating);

        void GetClientImpersonating(out int pfClientImpersonating);

        void SetPartitionId(ref Guid guidPartition);

        void GetPartitionId(out Guid pguidPartition);

        void SetProcessRequestType(ProcessRequestType dwPRT);

        void GetProcessRequestType(out ProcessRequestType pdwPRT);

        void SetOrigClsctx(int dwOrigClsctx);

        void GetOrigClsctx(out int pdwOrigClsctx);

        void GetDefaultAuthenticationLevel(out int pdwDefaultAuthnLvl);

        void SetDefaultAuthenticationLevel(int dwDefaultAuthnLvl);

        void GetLUARunLevel(out RunLevel pdwLUARunLevel, out IntPtr phwnd);

        void SetLUARunLevel(RunLevel dwLUARunLevel, IntPtr hwnd);
    }
}
