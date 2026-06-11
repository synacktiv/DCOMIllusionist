using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace DCOMIllusionist.Core.Interop
{
    internal static class NativeMethods
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern uint RegConnectRegistry(
            string lpMachineName,
            HKEY hKey,
            out IntPtr phkResult);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint RegOpenKeyEx(
            IntPtr hKey,
            string lpSubKey,
            uint ulOptions,
            REGSAM samDesired,
            out IntPtr phkResult);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern uint RegCloseKey(IntPtr hKey);

        [DllImport("ole32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void CoMarshalInterface(System.Runtime.InteropServices.ComTypes.IStream pStm, in Guid riid,
        [MarshalAs(UnmanagedType.Interface)] object pUnk, MSHCTX dwDestContext, IntPtr pvDestContext, MSHLFLAGS mshlflags);

        [DllImport("ole32.Dll")]
        public static extern uint CoCreateInstance(ref Guid clsid,
           [MarshalAs(UnmanagedType.IUnknown)] object inner,
           uint context,
           ref Guid uuid,
           [MarshalAs(UnmanagedType.IUnknown)] out object rReturnedComObject);

        [DllImport("rpcrt4.dll")]
        public static extern int RpcServerUseProtseqEp(
            string Protseq,
            uint MaxCalls,
            string Endpoint,
            IntPtr SecurityDescriptor);

        [return: MarshalAs(UnmanagedType.Interface)]
        [DllImport("ole32.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern IBindCtx CreateBindCtx([In] uint reserved);

        [return: MarshalAs(UnmanagedType.Interface)]
        [DllImport("ole32.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern IMoniker CreateObjrefMoniker(
            [MarshalAs(UnmanagedType.Interface)] object punk);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern IntPtr WTSOpenServer(string pServerName);

        [DllImport("wtsapi32.dll")]
        public static extern void WTSCloseServer(IntPtr hServer);

        [DllImport("wtsapi32.dll")]
        public static extern bool WTSEnumerateSessions(
            IntPtr hServer,
            int Reserved,
            int Version,
            out IntPtr ppSessionInfo,
            out int pCount
        );

        [DllImport("wtsapi32.dll")]
        public static extern void WTSFreeMemory(IntPtr pMemory);

        [DllImport("wtsapi32.dll")]
        public static extern bool WTSQuerySessionInformation(
            IntPtr hServer,
            int sessionId,
            WTS_INFO_CLASS infoClass,
            out IntPtr ppBuffer,
            out int pBytesReturned
        );

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();
    }
}
