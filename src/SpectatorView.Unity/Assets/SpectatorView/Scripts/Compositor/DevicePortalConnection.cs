using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using System.Collections;
using System.Text;
using System.IO;

namespace Microsoft.MixedReality.SpectatorView
{
    public enum DevicePortalState
    {
        NotConnected,
        Connecting,
        NotStarted,
        NotRunning,
        Running,
        Starting,
        Stopping
    }

    public class DevicePortalConnection : MonoBehaviour
    {
        private const string HolographicCameraAppName = "SpectatorView.HolographicCamera";
        private const string InfoRoute = "/api/os/info";
        private const string BatteryRoute = "/api/power/battery";
        private const string InstalledPackagesRoute = "/api/app/packagemanager/packages";
        private const string RunningProcessesRoute = "/api/resourcemanager/processes";
        private const string StartAppRouteBase = "/api/taskmanager/app?appid=";
        private const string StopAppRouteBase = "/api/taskmanager/app?package=";

        [SerializeField]
        [Tooltip("Interval in seconds between two health checks")]
        private float healthCheckInterval = 5.0f;
        [SerializeField]
        [Tooltip("Interval in seconds between two checks during app start")]
        private float appStartCheckInterval = 1.0f;
        [SerializeField]
        [Tooltip("Number of checks during app start")]
        private int appStartMaxTries = 10;

        private string ipAddress;
        private string authorization;
        private InstalledPackage? deviceApp;
        private float lastHealthCheck;
        private CancellationTokenSource connectCTS = null;
        private string StartAppRoute => StartAppRouteBase + Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(deviceApp.Value.PackageRelativeId)));
        private string StopAppRoute => StopAppRouteBase + Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(deviceApp.Value.PackageFullName)));

        public DevicePortalState State { get; private set; } = DevicePortalState.NotConnected;
        public bool IsConnected => State != DevicePortalState.NotConnected && State != DevicePortalState.Connecting;

        public string DeviceName { get; private set; } = "<unknown>";
        public bool IsAppInstalled => deviceApp.HasValue;
        public float BatteryLevel { get; private set; } = 0.0f;
        public bool IsPowered { get; private set; } = false;
        public int WorkingSet { get; private set; } = 0;
        public float CPUUsage { get; private set; } = 0.0f;

        public DeviceInfoObserver HolographicDeviceInfo { get; set; } = null;

        public void StartConnect(string ipAddress, string user, string password)
        {
            async Task Connect()
            {
                var info = await SendJSONRequest<InfoResponse>(InfoRoute, "GET", connectCTS.Token);
                DeviceName = info.ComputerName;

                var packages = await SendJSONRequest<PackagesResponse>(InstalledPackagesRoute);
                var optDeviceApp = packages.InstalledPackages?.FirstOrDefault(p => p.Name.Contains(HolographicCameraAppName));
                if (packages.InstalledPackages == null || optDeviceApp == null || optDeviceApp.Value.PackageFullName == null)
                {
                    Debug.LogError("Could not find holographic camera app installed on device");
                }
                else
                {
                    deviceApp = optDeviceApp.Value;
                }

                await RunHealthCheck();
            }

            State = DevicePortalState.Connecting;
            this.ipAddress = ipAddress;
            // mind the "auto-" prefix to bypass CSRF protection
            authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("auto-" + user + ":" + password));
            connectCTS = new CancellationTokenSource();
            Dispatcher.ScheduleAsync(Connect, connectCTS.Token);
        }

        public void CancelConnect()
        {
            if (connectCTS != null)
            {
                connectCTS.Cancel();
                State = DevicePortalState.NotConnected;
            }
        }

        public void Disconnect()
        {
            State = DevicePortalState.NotConnected;
        }

        public void StartApp()
        {
            async Task StartAppAsync()
            {
                var request = await SendRequest(StartAppRoute, "POST");
                if (request.isHttpError || request.isNetworkError)
                {
                    Debug.LogError("Could not send a request to start the app: " + request.error);
                    State = DevicePortalState.NotStarted;
                    return;
                }

                for (int i = 0; i < appStartMaxTries; i++)
                {
                    await CheckRunningProcess();
                    if (State == DevicePortalState.Running)
                        return;
                    await Task.Delay(TimeSpan.FromSeconds(appStartCheckInterval));
                }

                Debug.LogError("Could not start the app");
            }

            if (!IsAppInstalled)
            {
                Debug.LogError("App cannot be started because it is not installed");
                return;
            }
            if (State != DevicePortalState.NotStarted)
            {
                Debug.LogError($"Unexpected state for starting the app: {State}");
                return;
            }
            State = DevicePortalState.Starting;
            var cts = new CancellationTokenSource();
            Dispatcher.ScheduleAsync(StartAppAsync, cts.Token);
        }

        public void StopApp()
        {
            async Task StopAppAsync()
            {
                var request = await SendRequest(StopAppRoute, "DELETE");
                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError($"Could not send a request to stop the app: {request.error}");
                    return;
                }

                for (int i = 0; i < appStartMaxTries; i++)
                {
                    await CheckRunningProcess();
                    if (State != DevicePortalState.Running && State != DevicePortalState.NotRunning)
                        return;
                    State = DevicePortalState.Stopping; // stopping takes a while and changes from running to not running to not started
                    await Task.Delay(TimeSpan.FromSeconds(appStartCheckInterval));
                }

                Debug.LogError("Could not stop the app");
            }

            if (!IsAppInstalled)
            {
                Debug.LogError("App cannot be stopped because it is not installed");
                return;
            }
            if (State != DevicePortalState.NotRunning && State != DevicePortalState.Running)
            {
                Debug.LogError($"Unexpected state for stopping the app: {State}");
                return;
            }
            State = DevicePortalState.Stopping;
            var cts = new CancellationTokenSource();
            Dispatcher.ScheduleAsync(StopAppAsync, cts.Token);
        }

        private void Update()
        {
            if (!IsConnected || State == DevicePortalState.Starting || State == DevicePortalState.Stopping)
                return;

            if (Time.time - lastHealthCheck >= healthCheckInterval)
            {
                var cts = new CancellationTokenSource();
                Dispatcher.ScheduleAsync(RunHealthCheck, cts.Token);
            }
        }

        private async Task RunHealthCheck()
        {
            lastHealthCheck = Time.time;

            var battery = await SendJSONRequest<BatteryResponse>(BatteryRoute);
            IsPowered = battery.AcOnline;
            BatteryLevel = battery.Level;

            await CheckRunningProcess();
        }

        private async Task CheckRunningProcess()
        {
            if (deviceApp == null)
            {
                State = DevicePortalState.NotStarted;
                return;
            }

            var processes = await SendJSONRequest<ProcessesResponse>(RunningProcessesRoute);
            var optProcess = processes.Processes?.FirstOrDefault(
                p => p.PackageFullName == deviceApp.Value.PackageFullName && p.ImageName != "RuntimeBroker.exe");
            if (processes.Processes == null || optProcess == null || optProcess.Value.PackageFullName == null)
            {
                State = DevicePortalState.NotStarted;
                return;
            }
            State = optProcess.Value.IsRunning ? DevicePortalState.Running : DevicePortalState.NotRunning;
            CPUUsage = optProcess.Value.CPUUsage;
            WorkingSet = optProcess.Value.PrivateWorkingSet;
        }

        private async Task<UnityWebRequest> SendRequest(string route, string method = "GET", CancellationToken? ct = null)
        {
            var request = new UnityWebRequest($"https://{ipAddress}{route}", method);
            request.SetRequestHeader("authorization", authorization);
            request.certificateHandler = new DevicePortalCertificateHandler();
            request.downloadHandler = new DownloadHandlerBuffer();
            var requestOperation = request.SendWebRequest();
            await Dispatcher.WhenAsync(() => requestOperation.isDone, ct ?? CancellationToken.None);

            return request;
        }

        private async Task<T> SendJSONRequest<T>(string route, string method = "GET", CancellationToken? ct = null)
        {
            var request = await SendRequest(route, method, ct);

            if (request.isNetworkError || request.isHttpError)
            {
                State = DevicePortalState.NotConnected;
                var exception = new IOException("Could not send request to device portal: " + request.error);
                Debug.LogException(exception); // thrown exceptions from tasks are not visible in Unity console 
                throw exception;
            }
            return JsonUtility.FromJson<T>(request.downloadHandler.text);
        }

        private class DevicePortalCertificateHandler : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                // TODO: Give opportunity for user to provide certificate hash to compare against?
                return true;
            }
        }

#pragma warning disable CS0649 // variable is never assigned (wrong, it is by JsonUtility)
        [Serializable]
        private struct InfoResponse
        {
            public string ComputerName;
        }

        [Serializable]
        private struct InstalledPackage
        {
            public string Name;
            public string PackageFullName;
            public string PackageRelativeId;
        }

        [Serializable]
        private struct PackagesResponse
        {
            public InstalledPackage[] InstalledPackages;
        }

        [Serializable]
        private struct BatteryResponse
        {
            public bool AcOnline;
            public int MaximumCapacity;
            public int RemainingCapacity;

            public float Level => MaximumCapacity >= 0
                ? RemainingCapacity / (float)MaximumCapacity
                : float.NaN;
        }

        [Serializable]
        private struct ProcessesResponse
        {
            [Serializable]
            public struct RunningProcess
            {
                public float CPUUsage;
                public bool IsRunning;
                public string PackageFullName;
                public int PrivateWorkingSet;
                public string ImageName;
            }
            public RunningProcess[] Processes;
        }
#pragma warning restore CS0649
    }
}
