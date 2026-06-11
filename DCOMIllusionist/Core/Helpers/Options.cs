using DCOMIllusionist.Core.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Helpers
{
    public class Options
    {
        public bool Debug { get; set; }  = false;
        public string Session { get; set; }
        public string Gadget { get; set; }
        public ExploitType ExploitType { get; set; }
        public InputArgs Arg { get; set; } = new InputArgs();
        public string Target { get; set; }
        public string ListenPort { get; set; } = Constants.PORT_DEFAULT;
        public string CLSID { get; set; } = Constants.CLSID_DEFAULT;
        public string AppId { get; set; } = Constants.APPID_DEFAULT;
        public string AttackerSID { get; set; }
        public Boolean CheckPort { get; set; } = true;
        public string Listen { get; set; }
        public string RestoreRegistryBackup { get; set; }
        public bool SkipLocalRegistrySetup { get; set; } = false;
        public bool SkipRemoteRegistrySetup { get; set; } = false;
        public Boolean TestNetwork { get; set; } = false;
        public Boolean LocalRegistryOnly { get; set; } = false;
        public Boolean RemoteRegistryOnly { get; set; } = false;
        public bool ListSessions { get; set; } = false;
        public bool Hku { get; set; } = false;
        public bool FakeCLSID { get; set; } = false;

        public Boolean CheckParams()
        {

            if (string.IsNullOrEmpty(Target))
            {
                Log.Error("Target is missing. Add --target <value>");
                return false;
            }

            bool hasValidMode =
                !string.IsNullOrEmpty(Arg.Input) ||             // Covers --ps-exec, --curl, --load-dll, --yso-b64, --file-write-src
                !string.IsNullOrEmpty(Arg.Cmd) ||               // Covers --exec
                !string.IsNullOrEmpty(RestoreRegistryBackup) || // --restore-backup
                LocalRegistryOnly ||                            // --local-registry-only
                TestNetwork ||                                  // --test-network
                ListSessions ||                                 // --list-sessions
                RemoteRegistryOnly ||                           // --remote-registry-only
                FakeCLSID;                                      // --fake-clsid

            if (!hasValidMode)
            {
                Log.Error("You must specify at least one operational mode:");
                Console.WriteLine("    --ps-exec <args>");
                Console.WriteLine("    --exec <cmd>");
                Console.WriteLine("    --curl <url>");
                Console.WriteLine("    --load-dll <path>");
                Console.WriteLine("    --yso-b64 <b64>");
                Console.WriteLine("    --file-write-src <file>");
                Console.WriteLine("    --restore-backup <path>");
                Console.WriteLine("    --local-registry-only");
                Console.WriteLine("    --remote-registry-only");
                Console.WriteLine("    --test-network");
                return false;
            }

            if(this.ExploitType == ExploitType.LoadDLL && string.IsNullOrEmpty(Arg.DLLClass))
            {
                Log.Error($"You must specify a class in the DLL (--dll-class) that implement the static method: {Arg.DLLMethod}");
                return false;
            }

            if (this.ExploitType == ExploitType.Exec && !string.IsNullOrEmpty(Arg.Input) && string.IsNullOrEmpty(Arg.Cmd))
            {
                Log.Error($"You must specify a binary to execute with --exec");
                return false;
            }

            if(this.ExploitType == ExploitType.FileWrite && (string.IsNullOrEmpty(Arg.Input)||string.IsNullOrEmpty(Arg.FileWriteDst)))
            {
                Log.Error($"You must specify a source and a destination --file-write-src / --file-write-dst");
                return false;
            }

            
            if(!string.IsNullOrEmpty(Session) && !Utils.IsDomainJoined())
            {
                Log.Error("When using --session, the attacking machine must be domain-joined. See README.md for more details.");
                return false;            
            }

            if (Hku && !Utils.IsLocalhost(Target) && string.IsNullOrEmpty(AttackerSID))
            {
                Log.Error("When using --hkcu on a remote target the SID of the attacker is required via --attacker-sid.");
                return false;
            }

            if (Utils.IsLocalhost(Target))
                SkipLocalRegistrySetup = true;

            return true;
        }

        public void DebugOptions()
        {
            Log.Debug("Parsed Options:");
            Console.WriteLine($"Debug:                  {Debug}");
            Console.WriteLine($"Target:                 {Target}");
            Console.WriteLine($"ListenPort:             {ListenPort}");
            Console.WriteLine($"Session:                {Session}");
            Console.WriteLine($"CLSID:                  {CLSID}");
            Console.WriteLine($"AppId:                  {AppId}");
            Console.WriteLine($"AttackerSID:            {AttackerSID}");
            Console.WriteLine($"CheckPort:              {CheckPort}");
            Console.WriteLine($"Gadget:                 {Gadget}");
            Console.WriteLine($"ExploitType:            {ExploitType}");
            Console.WriteLine($"Arg:                    ");
            Console.WriteLine($"    - Cmd:              {Arg.Cmd}");
            Console.WriteLine($"    - Input:            {Arg.Input}");
            Console.WriteLine($"    - DLLMethod:        {Arg.DLLMethod}");
            Console.WriteLine($"    - DLLClass:         {Arg.DLLClass}");
            Console.WriteLine($"Listen:                 {Listen}");
            Console.WriteLine($"RestoreRegistryBackup:  {RestoreRegistryBackup}");
            Console.WriteLine($"LocalRegistryOnly:      {LocalRegistryOnly}");
            Console.WriteLine($"RemoteRegistryOnly:     {RemoteRegistryOnly}");
            Console.WriteLine($"SkipLocalRegistrySetup: {SkipLocalRegistrySetup}");
            Console.WriteLine($"SkipRemoteRegistrySetup:{SkipRemoteRegistrySetup}");
            Console.WriteLine($"TestNetwork:            {TestNetwork}");
            Console.WriteLine($"ListSessions:           {ListSessions}");
            Console.WriteLine($"HKCU:                   {Hku}");
        }
    }
}
