//    This file is part of OleViewDotNet.
//    Copyright (C) James Forshaw 2014
//
//    OleViewDotNet is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    OleViewDotNet is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with OleViewDotNet.  If not, see <http://www.gnu.org/licenses/>.

using DCOMIllusionist.Core.COM.Marshalling;
using DCOMIllusionist.Core.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;

namespace DCOMIllusionist.Core.COM
{
    public static class COMUtilities
    {

        // stolen from KrbRelay
        public static byte[] MarshalObject(object obj, Guid iid, MSHCTX mshctx, MSHLFLAGS mshflags)
        {
            MemoryStream stm = new MemoryStream();
            NativeMethods.CoMarshalInterface(new IStreamImpl(stm), iid, obj, mshctx, IntPtr.Zero, mshflags);
            return stm.ToArray();
        }

        public static byte[] MarshalObject(object obj)
        {
            return MarshalObject(obj, COMKnownGuids.IID_IUnknown, MSHCTX.DIFFERENTMACHINE, MSHLFLAGS.NORMAL);
        }

        // Stolen from original POC of James Forshaw
        public static byte[] MarshalledObjectWithMoniker(object o)
        {
            IMoniker moniker = NativeMethods.CreateObjrefMoniker(o);
            IBindCtx bindCtx = NativeMethods.CreateBindCtx(0);

            moniker.GetDisplayName(bindCtx, null, out string name);

            return Convert.FromBase64String(name.Substring(7).TrimEnd(':'));
        }

        internal static int GetProcessIdFromIPid(Guid ipid)
        {
            return BitConverter.ToUInt16(ipid.ToByteArray(), 4);
        }

        internal static int GetApartmentIdFromIPid(Guid ipid)
        {
            return BitConverter.ToInt16(ipid.ToByteArray(), 6);
        }

        internal static string GetApartmentIdStringFromIPid(Guid ipid)
        {
            int appid = GetApartmentIdFromIPid(ipid);
            switch (appid)
            {
                case 0:
                    return "NTA";

                case -1:
                    return "MTA";

                default:
                    return $"STA (Thread ID {appid})";
            }
        }

        public static string GetProcessNameById(int pid)
        {
            try
            {
                return Process.GetProcessById(pid).ProcessName;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
