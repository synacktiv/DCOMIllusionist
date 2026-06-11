using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Helpers
{
    public class Service
    {
        private readonly string _target;
        private readonly string _serviceName;
        public bool WasStarted { get; set; } = true;
        public string StartType { get; set; } = "";

        public Service(string target, string serviceName)
        {
            _target = target;
            _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        }

        /// <summary>
        /// Starts the service if it is stopped, enables it if disabled.
        /// </summary>
        public void Start()
        {
            try
            {
                using (var sc = new ServiceController(_serviceName, _target))
                {
                    if (sc.Status == ServiceControllerStatus.Stopped || sc.Status == ServiceControllerStatus.StopPending)
                    {
                        Log.Debug($"Starting {_serviceName} service on {_target}");
                        WasStarted = false;

                        // Enable service if disabled
                        try
                        {
                            StartType = GetServiceStartupType();
                            if (StartType?.Equals("Disabled", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                Log.Debug($"{_serviceName} is disabled; enabling it...");
                                SetServiceStartupType("Manual");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Failed to change start mode: {ex.Message}");
                        }

                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                        Log.Debug($"{_serviceName} service is now running.");
                    }
                    else
                    {
                        Log.Debug($"{_serviceName} service is already running.");
                    }
                }
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("Cannot open RemoteRegistry service on computer"))
            {
                Log.Error($"Access denied or {_serviceName} cannot be controlled. Restart with elevated shell.");
                throw;
            }
        }

        /// <summary>
        /// Restores the service to a previous startup type and running/stopped state.
        /// </summary>
        public void Restore()
        {
            try
            {
                if (!WasStarted)
                {
                    ServiceController sc = new ServiceController("RemoteRegistry", _target);
                    Log.Debug($"Stopping RemoteRegistry on {_target}");
                    sc.Stop();
                }

                if (!String.IsNullOrEmpty(StartType))
                {
                    SetServiceStartupType(StartType);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to restore {_serviceName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the startup type of the service.
        /// </summary>
        public string GetServiceStartupType()
        {
            try
            {
                var scope = new ManagementScope($@"\\{_target}\root\cimv2");
                scope.Connect();

                var query = new ObjectQuery($"SELECT StartMode FROM Win32_Service WHERE Name='{_serviceName}'");
                using (var searcher = new ManagementObjectSearcher(scope, query))
                {
                    foreach (ManagementObject service in searcher.Get())
                    {
                        return service["StartMode"]?.ToString() ?? "Unknown";
                    }
                }

                throw new InvalidOperationException($"Service '{_serviceName}' not found on {_target}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting service start mode: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sets the startup type of the service. Valid values: "Automatic", "Manual", "Disabled".
        /// </summary>
        public void SetServiceStartupType(string mode)
        {
            try
            {
                mode = mode.Trim();

                if (!string.Equals(mode, "Auto", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(mode, "Manual", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(mode, "Disabled", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Invalid mode '{mode}'. Use Auto, Manual, or Disabled.");
                }

                var scope = new ManagementScope($@"\\{_target}\root\cimv2");
                scope.Connect();

                var query = new ObjectQuery($"SELECT * FROM Win32_Service WHERE Name='{_serviceName}'");
                using (var searcher = new ManagementObjectSearcher(scope, query))
                {
                    foreach (ManagementObject service in searcher.Get())
                    {
                        service.InvokeMethod("ChangeStartMode", new object[] { mode });
                        Log.Debug($"{_serviceName} startup type set to '{mode}' on {_target}");
                        return;
                    }
                }

                throw new InvalidOperationException($"Service '{_serviceName}' not found on {_target}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error setting service start mode: {ex.Message}");
                throw;
            }
        }
    }
}
