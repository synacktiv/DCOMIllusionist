using DCOMIllusionist.Core;
using DCOMIllusionist.Core.Generators;
using DCOMIllusionist.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Options options = ArgsParser.Parse(args);

            if (!options.CheckParams())
                return;

            if(options.Debug)
                options.DebugOptions();

            Illusionist illusionist = new Illusionist(options);

            if (!string.IsNullOrEmpty(options.Arg.Cmd) || !string.IsNullOrEmpty(options.Arg.Input))
                illusionist.Exploit();

            if (!string.IsNullOrEmpty(options.RestoreRegistryBackup))
                illusionist.RestoreRegistryBackup();

            if (options.TestNetwork)
                illusionist.TestNetwork();

            if (options.ListSessions)
                illusionist.DisplayRemoteSessions();

            if (options.LocalRegistryOnly)
                illusionist.ModifyLocalRegistry();

            if(options.RemoteRegistryOnly)
                illusionist.ModifyRemoteRegistry();

            if (options.FakeCLSID)
                illusionist.CreateFakeCLSID();
        }
    }
}
