using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Interop
{
    public enum WTS_INFO_CLASS
    {
        WTSInitialProgram = 0,
        WTSApplicationName = 1,
        WTSWorkingDirectory = 2,
        WTSOEMId = 3,
        WTSSessionId = 4,
        WTSUserName = 5,
        WTSWinStationName = 6,
        WTSDomainName = 7,
        WTSConnectState = 8,
        WTSClientBuildNumber = 9,
        WTSClientName = 10,
        WTSClientDirectory = 11,
        WTSClientProductId = 12,
        WTSClientHardwareId = 13,
        WTSClientAddress = 14,
        WTSClientDisplay = 15,
        WTSClientProtocolType = 16,
        WTSIdleTime = 17,
        WTSLogonTime = 18,
        WTSIncomingBytes = 19,
        WTSOutgoingBytes = 20,
        WTSIncomingFrames = 21,
        WTSOutgoingFrames = 22,
        WTSClientInfo = 23,             // WTSCLIENT structure
        WTSSessionInfo = 24,            // WTSINFO structure
        WTSSessionInfoEx = 25,          // WTSINFOEX structure (Windows Vista+)
        WTSConfigInfo = 26,             // WTS_CONFIG_INFO structure
        WTSValidationInfo = 27,         // WTS_VALIDATION_INFORMATION
        WTSSessionAddressV4 = 28,       // IPv4 address only
        WTSIsRemoteSession = 29         // BOOL (non-zero if session is remote)
    }
}
