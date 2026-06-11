using DCOMIllusionist.Core.Helpers;
using Microsoft.Win32.SafeHandles;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.IO;
using DCOMIllusionist.Core.Interop;
using Newtonsoft.Json;
using System.Management;
using System.Security;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DCOMIllusionist.Core
{
    public class Registry : IDisposable
    {
        private string _remoteServer;
        private string _keyPath;
        private RegistryKey _regKey;
        private IntPtr _remoteHive;
        private IntPtr _subKeyHandle;
        private readonly HKEY _rootKey;

        private Service _remoteRegistryService;

        private string _target;
        private byte[] _originalSecurityDescriptor;
        private SecurityIdentifier _originalOwnerSid;
        private List<Dictionary<string, object>> _originalValues = new List<Dictionary<string, object>>();

        public byte[] OriginalSecurityDescriptor
        {
            get => _originalSecurityDescriptor;
            set => _originalSecurityDescriptor = value;
        }

        public SecurityIdentifier OriginalOwnerSid
        {
            get => _originalOwnerSid;
            set => _originalOwnerSid = value;
        }

        public List<Dictionary<string, object>> OriginalValues
        {
            get => _originalValues;
            set => _originalValues = value;
        }

        public Service RemoteRegistryService
        {
            get => _remoteRegistryService;
            set => _remoteRegistryService = value;
        }

        public Registry(string remoteServer, string subKeyPath, HKEY rootKey = HKEY.HKEY_LOCAL_MACHINE, bool startRemoteService = false)
        {
            _target = remoteServer;
            _remoteServer = remoteServer.StartsWith(@"\\") ? remoteServer : @"\\" + remoteServer;
            _keyPath = subKeyPath;
            _rootKey = rootKey;

            // Start RemoteRegistry service on the remote machine
            if (startRemoteService)
            {
                _remoteRegistryService = new Service(remoteServer, "RemoteRegistry");
                _remoteRegistryService.Start();
            }
        }

        public Registry(string subKeyPath) : this("127.0.0.1", subKeyPath) { }

        public RegistryKey RegKey => _regKey;

        public void Initialize()
        {
            Initialize(RegistryRights.TakeOwnership | RegistryRights.ReadPermissions);
        }

        public void Initialize(RegistryRights registryRights)
        {
            uint result = NativeMethods.RegConnectRegistry(_remoteServer, _rootKey, out _remoteHive);
            if (result != 0 || _remoteHive == IntPtr.Zero)
            {
                var ex = new Win32Exception((int)result);
                throw new InvalidOperationException($"RegConnectRegistry failed: {ex.Message}");
            }

            result =  NativeMethods.RegOpenKeyEx(_remoteHive, _keyPath, 0, (REGSAM)(uint)registryRights, out _subKeyHandle);
            if (result != 0 || _remoteHive == IntPtr.Zero)
            {
                var ex = new Win32Exception((int)result);
                throw new InvalidOperationException($"RegOpenKeyEx failed: {ex.Message}");
            }

            try
            {
                _regKey = RegistryKey.FromHandle(new SafeRegistryHandle(_subKeyHandle, ownsHandle: true));
                if (_regKey == null)
                {
                    throw new InvalidOperationException($"Failed to open the remote registry subkey: {_keyPath} ({_rootKey.ToString()})");
                }
            }
            catch (Exception ex) when (
                ex.Message.Contains("Requested registry access is not allowed"))
            {
                Log.Error($"Access denied, restart with elevated shell.");
                throw;
            }
            
        }

        private void ReOpenKeyWithPerms(RegistryRights rights)
        {
            //var baseKey = RegistryKey.FromHandle(new SafeRegistryHandle(_remoteHive, false));
            //var baseKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.Users, _remoteServer);
            //_regKey = baseKey.OpenSubKey(_keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree, rights);
            var result = NativeMethods.RegOpenKeyEx(_remoteHive, _keyPath, 0, (REGSAM)(uint)rights, out _subKeyHandle);
            if (result != 0 || _remoteHive == IntPtr.Zero)
            {
                var ex = new Win32Exception((int)result);
                throw new InvalidOperationException($"RegOpenKeyEx failed: {ex.Message}");
            }

            _regKey = RegistryKey.FromHandle(new SafeRegistryHandle(_subKeyHandle, ownsHandle: true));
        }

        private void EnsureInitialized()
        {
            if (_regKey == null)
                throw new InvalidOperationException("Call Initialize() before performing operations.");
        }

        public void SetRegKeyValue(string key, object value)
        {
            if (!IsBackupAlreadyPresent(key))
                BackupValue(key);

            object originalValue = _regKey.GetValue(key, null);

            if (originalValue != null)
            {
                _regKey.SetValue(key, value, _regKey.GetValueKind(key));
            }
            else
            {
                _regKey.SetValue(key, value);
            }
        }

        public RegistryKey CreateSubKey(string subPath)
        {
            return _regKey.CreateSubKey(subPath);
        }

        private void BackupValue(string key)
        {
            Dictionary<string, object> backup = new Dictionary<string, object>();

            backup.Add("key", key);

            object originalValue = _regKey.GetValue(key, null);

            if (originalValue != null)
            {
                RegistryValueKind kind = _regKey.GetValueKind(key);
                switch (kind)
                {
                    case RegistryValueKind.String:
                    case RegistryValueKind.ExpandString:
                    case RegistryValueKind.DWord:
                        backup.Add("value", originalValue);
                        break;
                    case RegistryValueKind.Binary:
                        byte[] bytes = (byte[])originalValue;
                        backup.Add("value", Convert.ToBase64String(bytes));
                        backup.Add("kind", RegistryValueKind.Binary);
                        break;
                    default:
                        Log.Error($"Unhandled type: {kind}");
                        break;
                }
            }

            _originalValues.Add(backup);
        }

        private bool IsBackupAlreadyPresent(string value)
        {
            return _originalValues.Any(dict =>
                dict.Values.Any(v => string.Equals(v?.ToString(), value, StringComparison.OrdinalIgnoreCase)));
        }

        //Debug code
        private string FormatOriginalValues()
        {
            return string.Join(Environment.NewLine, _originalValues.Select((dict, index) =>
                $"Entry {index + 1}:\n" +
                string.Join("\n", dict.Select(kvp => $"  {kvp.Key}: {kvp.Value}"))
            ));
        }

        public void SaveRegistryInfoToJson()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            SaveRegistryInfoToJson($"./backup-{_target}-{timestamp}.json");
        }

        public void SaveRegistryInfoToJson(string filePath)
        {
            Log.Info($"Saving modifications to: {filePath}");
            Backup export = new Backup()
            {
                target = _target,
                keyPath = _keyPath,
                values = _originalValues,
                wasStarted = _remoteRegistryService.WasStarted,
                startType = _remoteRegistryService.StartType,
            };

            if (_originalSecurityDescriptor != null)
            {
                export.securityDescriptor = Convert.ToBase64String(_originalSecurityDescriptor);
            }

            if (_originalOwnerSid != null)
            {
                export.ownerSid = _originalOwnerSid.Value;
            }

            string json = JsonConvert.SerializeObject(export, Newtonsoft.Json.Formatting.Indented);

            File.WriteAllText(filePath, json);
        }

        public static Registry LoadRegistryInfoFromJson(string filePath)
        {
            Registry restored;
            string json = File.ReadAllText(filePath);

            var import = JsonConvert.DeserializeObject<Backup>(json);

            if (import.target != null)
            {
                restored  = new Registry(import.target, import.keyPath);
            }
            else
            {
                restored  = new Registry(import.keyPath);
            }

            restored.OriginalOwnerSid = string.IsNullOrWhiteSpace(import.ownerSid) ? null : new SecurityIdentifier(import.ownerSid);
            restored.OriginalSecurityDescriptor = string.IsNullOrWhiteSpace(import.securityDescriptor) ? null : Convert.FromBase64String(import.securityDescriptor);
            restored.OriginalValues = import.values;

            restored.RemoteRegistryService = new Service(import.target, "RemoteRegistry");
            restored.RemoteRegistryService.WasStarted = import.wasStarted;
            restored.RemoteRegistryService.StartType = import.startType;

            return restored;
        }

        public void SetOwnership(SecurityIdentifier sid)
        {
            Log.Debug($"Adding {sid.Value} as owner");
            EnsureInitialized();

            ReOpenKeyWithPerms(RegistryRights.TakeOwnership|RegistryRights.ReadPermissions);
            
            if (_regKey == null)
                throw new UnauthorizedAccessException("Failed to open registry key with TakeOwnership rights.");

            // save original value
            var security = _regKey.GetAccessControl(AccessControlSections.All);
            _originalOwnerSid = security.GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;

            security = _regKey.GetAccessControl(AccessControlSections.Owner);
            security.SetOwner(sid);
            _regKey.SetAccessControl(security);
            
        }

        public void GrantFullControl(SecurityIdentifier sid)
        {
            Log.Debug($"Granting full control to: {sid.Value}");
            EnsureInitialized();

            try
            {
                ReOpenKeyWithPerms(RegistryRights.ReadPermissions|RegistryRights.ChangePermissions);
                
                if (_regKey == null)
                    throw new UnauthorizedAccessException("Failed to open registry key with ChangePermissions rights.");

                // Save original security descriptor
                var security = _regKey.GetAccessControl(AccessControlSections.All);
                _originalSecurityDescriptor = security.GetSecurityDescriptorBinaryForm();

                var accessRule = new RegistryAccessRule(
                    sid,
                    RegistryRights.FullControl,
                    InheritanceFlags.None,
                    PropagationFlags.None,
                    AccessControlType.Allow);

                var newSecurity = _regKey.GetAccessControl(AccessControlSections.All);
                newSecurity.AddAccessRule(accessRule);
                _regKey.SetAccessControl(newSecurity);
               
            }
            catch (SecurityException)
            {
                Log.Error($"Fail to grant full control to {sid}, try --attacker-sid if you are using runas.");
                throw;
            }
        }

        public void ReopenKeyWithFullPerms()
        {
            EnsureInitialized();
            ReOpenKeyWithPerms(RegistryRights.FullControl);
        }

        public void RestoreOriginalSecurityDescriptor()
        {
            if (_originalSecurityDescriptor != null)
            {
                RegistrySecurity currentSecurity = _regKey.GetAccessControl(AccessControlSections.All);
                currentSecurity.SetSecurityDescriptorBinaryForm(_originalSecurityDescriptor);
                _regKey.SetAccessControl(currentSecurity);
            }
        }

        public void RestoreOriginalOwner()
        {
            if (_originalOwnerSid != null)
            {
                RegistrySecurity currentSecurity = _regKey.GetAccessControl(AccessControlSections.All);
                currentSecurity.SetOwner(_originalOwnerSid);
                _regKey.SetAccessControl(currentSecurity);
            }
        }
        public void RestoreRegistryState()
        {
            _remoteRegistryService?.Restore();
        }

        public void RestoreOriginalValues()
        {
            foreach (Dictionary<string, object> dic in _originalValues)
            {
                dic.TryGetValue("key", out object key);
                dic.TryGetValue("value", out object value);
                dic.TryGetValue("kind", out object kind);
                
                if (kind != null && RegistryValueKind.Binary.Equals((RegistryValueKind)kind))
                {
                    value = Convert.FromBase64String((string)value);
                }

                if (value == null)
                {
                    _regKey.DeleteValue((string)key);
                }
                else
                {
                    _regKey.SetValue((string)key, value);
                }
            }
        }

        public void Restore(bool restoreState = true)
        {
            Log.Debug($"Restoring registry modifications on {_target}");
            try
            {
                if (_regKey != null)
                {
                    ReopenKeyWithFullPerms();
                    RestoreOriginalValues();
                    RestoreOriginalOwner();
                    RestoreOriginalSecurityDescriptor();
                }

                if (restoreState)
                    RestoreRegistryState();
            }
            catch (Exception ex)
            {
                Log.Error($"Fail to restore modifications: {ex.Message}");

                if (Log.MinimumLevel == LogLevel.Debug)
                {
                    Log.Debug($"StackTrace:");
                    Console.WriteLine(ex.StackTrace);
                }

                SaveRegistryInfoToJson();
            }
            finally
            {
                if (_regKey != null)
                    _regKey.Close();
            }

        }

        public void DisplayCurrentSecurityInfo()
        {
            if (_regKey == null)
                throw new InvalidOperationException("Call Initialize() before performing operations.");

            var security = _regKey.GetAccessControl(AccessControlSections.Owner | AccessControlSections.Access);

            // Get current owner
            IdentityReference owner = security.GetOwner(typeof(NTAccount));
            SecurityIdentifier ownerSid = security.GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;
            Console.WriteLine($"Owner: {owner.Value} ({ownerSid?.Value})");
            Console.WriteLine("Access Rules:");

            // Iterate over access rules
            foreach (AuthorizationRule rule in security.GetAccessRules(true, true, typeof(NTAccount)))
            {
                if (rule is RegistryAccessRule regRule)
                {
                    string identity = regRule.IdentityReference.Value;
                    string rights = regRule.RegistryRights.ToString();
                    string accessType = regRule.AccessControlType.ToString();

                    Console.WriteLine($"- {identity} => {rights} ({accessType})");
                }
            }
        }

        public void Dispose()
        {
            _regKey?.Dispose();
            if (_remoteHive != IntPtr.Zero)
            {
                NativeMethods.RegCloseKey(_remoteHive);
            }

            if (_subKeyHandle != IntPtr.Zero)
            {
                NativeMethods.RegCloseKey(_subKeyHandle);
            }
        }

        public CommonSecurityDescriptor getSecurityDescriptorFromKey(string key)
        {
            object value = RegKey?.GetValue(key);

            if (value != null)
            {
                byte[] binarySd = value as byte[];
                return new CommonSecurityDescriptor(false, false, binarySd, 0);
            }
            return null;
        }
    }

    class Backup
    {
        public string target { get; set; }
        public string keyPath { get; set; }
        public string securityDescriptor { get; set; }
        public string ownerSid { get; set; }
        public List<Dictionary<string, object>> values { get; set; }
        public bool wasStarted { get; set; }
        public string startType { get; set; }
    }
}
