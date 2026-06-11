using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Helpers
{
    public class Utils
    {
        public static bool IsDomainJoined()
        {
            try
            {
                Domain.GetComputerDomain();
                return true;
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                return false;
            }
            catch (Exception)
            {
                // En cas d'autres exceptions, on retourne false
                return false;
            }
        }

        public static bool IsLocalhost(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
                return false;

            return target == "127.0.0.1" ||
                target == "::1" ||
                target.Equals("localhost", StringComparison.OrdinalIgnoreCase);
        }
    }
}
