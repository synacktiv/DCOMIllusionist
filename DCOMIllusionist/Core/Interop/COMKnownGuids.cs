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

using DCOMIllusionist.Core.Interop.Interfaces;
using System;

namespace DCOMIllusionist.Core.Interop
{

    public static class COMKnownGuids
    {
        private static Guid GetGuid<T>()
        {
            return typeof(T).GUID;
        }

        public static Guid IID_IUnknown => GetGuid<IUnknown>();
        public static Guid IID_IDispatch => GetGuid<IDispatch>();
        public static Guid IID_IMarshal => new Guid("{00000003-0000-0000-C000-000000000046}");
        public static Guid CLSID_StdMarshal => new Guid("{00000017-0000-0000-C000-000000000046}");
        public static Guid CLSID_ComActivator = new Guid("0000033C-0000-0000-c000-000000000046");
    }
}