using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Interop.Interfaces
{
    [Guid("000001B8-0000-0000-c000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IStandardActivator
    {
        void StandardGetClassObject(in Guid rclsid, CLSCTX dwContext, [In] COSERVERINFO pServerInfo, in Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppvClassObj);

        void StandardCreateInstance(in Guid Clsid, IntPtr punkOuter, CLSCTX dwClsCtx, [In] COSERVERINFO pServerInfo, int dwCount, [In, Out][MarshalAs(UnmanagedType.LPArray)] MULTI_QI[] pResults);

        void StandardGetInstanceFromFile([In] COSERVERINFO pServerInfo, in Guid pclsidOverride,
            IntPtr punkOuter, CLSCTX dwClsCtx, int grfMode, [MarshalAs(UnmanagedType.LPWStr)] string pwszName, int dwCount, [In, Out][MarshalAs(UnmanagedType.LPArray)] MULTI_QI[] pResults);

        int StandardGetInstanceFromIStorage(
            [In] COSERVERINFO pServerInfo,
            in Guid pclsidOverride,
            IntPtr punkOuter,
            CLSCTX dwClsCtx,
            IStorage pstg,
            int dwCount,
            [In, Out][MarshalAs(UnmanagedType.LPArray)] MULTI_QI[] pResults);

        int StandardGetInstanceFromIStoragee(
            COSERVERINFO pServerInfo,
            ref Guid pclsidOverride,
            [MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter,
            CLSCTX dwClsCtx,
            IStorage pstg,
            int dwCount,
            [In, Out][MarshalAs(UnmanagedType.LPArray)] MULTI_QI[] pResults);

        void Reset();
    }
}
