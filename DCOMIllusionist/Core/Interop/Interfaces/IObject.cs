using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Interop.Interfaces
{
    [Guid("65074f7f-63c0-304e-af0a-d51741cb4a8d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IObject
    {
        string ToString();
        bool Equals(object obj);
        int GetHashCode();
        Type GetType();
    }
}
