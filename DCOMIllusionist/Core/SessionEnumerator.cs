using DCOMIllusionist.Core.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core
{
    public class RemoteSessionEnumerator
    {
        private readonly string _serverName;
        private IntPtr _serverHandle = IntPtr.Zero;

        public RemoteSessionEnumerator(string serverName)
        {
            _serverName = serverName;
            _serverHandle = NativeMethods.WTSOpenServer(_serverName);
            if (_serverHandle == IntPtr.Zero)
                throw new Exception($"Unable to open connection to server: {_serverName}");
        }

        public void Close() => NativeMethods.WTSCloseServer(_serverHandle);

        public IEnumerable<SessionInfo> EnumerateSessions()
        {
            if (!NativeMethods.WTSEnumerateSessions(_serverHandle, 0, 1, out IntPtr ppSessionInfo, out int count))
                yield break;

            try
            {
                int dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
                IntPtr current = ppSessionInfo;

                for (int i = 0; i < count; i++)
                {
                    WTS_SESSION_INFO si = Marshal.PtrToStructure<WTS_SESSION_INFO>(current);
                    current = IntPtr.Add(current, dataSize);

                    // Filter only active or connected sessions
                    if (si.State != WTS_CONNECTSTATE_CLASS.WTSActive &&
                        si.State != WTS_CONNECTSTATE_CLASS.WTSConnected)
                        continue;

                    string username = GetString(si.SessionId, WTS_INFO_CLASS.WTSUserName);
                    string domain = GetString(si.SessionId, WTS_INFO_CLASS.WTSDomainName);
                    string client = GetString(si.SessionId, WTS_INFO_CLASS.WTSClientName);
                    string ip = GetClientIp(si.SessionId);
                    string protocol = GetProtocol(si.SessionId);

                    yield return new SessionInfo
                    {
                        SessionId = si.SessionId,
                        State = si.State.ToString(),
                        UserName = username,
                        Domain = domain,
                        ClientName = client,
                        ClientIP = ip,
                        Protocol = protocol
                    };
                }
            }
            finally
            {
                NativeMethods.WTSFreeMemory(ppSessionInfo);
            }
        }

        private string GetString(int sessionId, WTS_INFO_CLASS infoClass)
        {
            if (NativeMethods.WTSQuerySessionInformation(_serverHandle, sessionId, infoClass, out IntPtr buffer, out int bytesReturned) && bytesReturned > 1)
            {
                string result = Marshal.PtrToStringAnsi(buffer);
                NativeMethods.WTSFreeMemory(buffer);
                return result;
            }
            return "";
        }

        private string GetClientIp(int sessionId)
        {
            if (NativeMethods.WTSQuerySessionInformation(_serverHandle, sessionId, WTS_INFO_CLASS.WTSClientAddress, out IntPtr buffer, out int _))
            {
                var addr = Marshal.PtrToStructure<WTS_CLIENT_ADDRESS>(buffer);
                NativeMethods.WTSFreeMemory(buffer);

                if (addr.AddressFamily == 2)
                    return $"{addr.Address[2]}.{addr.Address[3]}.{addr.Address[4]}.{addr.Address[5]}";
            }
            return "N/A";
        }

        private string GetProtocol(int sessionId)
        {
            if (NativeMethods.WTSQuerySessionInformation(_serverHandle, sessionId, WTS_INFO_CLASS.WTSClientProtocolType, out IntPtr buffer, out int _))
            {
                int proto = Marshal.ReadInt16(buffer);
                NativeMethods.WTSFreeMemory(buffer);
                switch (proto)
                {
                    case 0:
                        return "Console";
                    case 2:
                        return "RDP";
                    default:
                        return $"Unknown ({proto})";
                }
            }
            return "Unknown";
        }
    }

    // Result structure
    public class SessionInfo
    {
        public int SessionId { get; set; }
        public string State { get; set; }
        public string UserName { get; set; }
        public string Domain { get; set; }
        public string ClientName { get; set; }
        public string ClientIP { get; set; }
        public string Protocol { get; set; }

        public override string ToString()
        {
            var parts = new List<string>();

            // Format de l'utilisateur (avec domaine si présent)
            string user = !string.IsNullOrWhiteSpace(UserName)
                ? (!string.IsNullOrWhiteSpace(Domain) ? $"{Domain}\\{UserName}" : UserName)
                : "Inconnu";

            parts.Add($"Session {SessionId} → {user}");

            var extras = new List<string>();

            if (!string.IsNullOrWhiteSpace(ClientName))
                extras.Add($"Client: {ClientName}");

            if (!string.IsNullOrWhiteSpace(ClientIP) && ClientIP != "N/A")
                extras.Add($"IP: {ClientIP}");

            if (!string.IsNullOrWhiteSpace(Protocol))
                extras.Add($"Protocole: {Protocol}");

            /*
            if (!string.IsNullOrWhiteSpace(State))
                extras.Add($"État: {State}");
            */

            if (extras.Count > 0)
                parts.Add($"({string.Join(", ", extras)})");

            return string.Join(" ", parts);
        }
    }
}
