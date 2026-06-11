using NetFwTypeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Helpers
{
    public static class Firewall
    {
        private const string RuleName = "DCOMIllusionist";

        private static INetFwProfile GetFirewallProfile()
        {
            var mgrType = Type.GetTypeFromProgID("HNetCfg.FwMgr", throwOnError: false);
            if (mgrType == null)
                throw new InvalidOperationException("Firewall manager is not available.");

            var mgr = (INetFwMgr)Activator.CreateInstance(mgrType);
            return mgr.LocalPolicy.CurrentProfile;
        }

        public static void OpenPort(int portNumber)
        {
            if (IsPortOpen(portNumber))
            {
                Log.Debug($"Port {portNumber} is already open.");
                return;
            }

            try
            {
                var profile = GetFirewallProfile();

                var port = (INetFwOpenPort)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwOpenPort"));
                port.Port = portNumber;
                port.Name = RuleName;
                port.Protocol = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
                port.Enabled = true;

                profile.GloballyOpenPorts.Add(port);

                Log.Debug($"Opened port {portNumber} on firewall.");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to open port {portNumber}: {ex.Message}");
            }
        }

        public static void ClosePort(int portNumber)
        {
            try
            {
                var profile = GetFirewallProfile();
                profile.GloballyOpenPorts.Remove(portNumber, NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP);

                Log.Debug($"Removed port {portNumber} from firewall.");
            }
            catch (Exception ex)
            {
                Log.Error($"Error removing port {portNumber}: {ex.Message}");
            }
        }

        public static bool IsPortOpen(int portNumber)
        {
            try
            {
                var profile = GetFirewallProfile();
                foreach (INetFwOpenPort port in profile.GloballyOpenPorts)
                {
                    if (port.Port == portNumber && port.Protocol == NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP)
                        return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error checking firewall for port {portNumber}: {ex.Message}");
            }

            return false;
        }
    }
}
