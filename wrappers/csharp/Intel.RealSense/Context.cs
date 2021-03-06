﻿using System;
using System.Runtime.InteropServices;

namespace Intel.RealSense
{
    public class Context : IDisposable
    {
        internal HandleRef m_instance;

        public readonly int api_version;
        public string Version
        {
            get
            {
                if (api_version / 10000 == 0) return api_version.ToString();
                return (api_version / 10000) + "." + (api_version % 10000) / 100 + "." + (api_version % 100);
            }
        }

        /// <summary>
        /// default librealsense context class
        /// </summary>
        public Context()
        {
            object error;
            api_version = NativeMethods.rs2_get_api_version(out error);
            m_instance = new HandleRef(this, NativeMethods.rs2_create_context(api_version, out error));

            onDevicesChangedCallback = new rs2_devices_changed_callback(onDevicesChanged);
            NativeMethods.rs2_set_devices_changed_callback(m_instance.Handle, onDevicesChangedCallback, IntPtr.Zero, out error);
        }

        // Keeps the delegate alive, if we were to assign onDevicesChanged directly, there'll be 
        // no managed reference it, it will be collected and cause a native exception.
        readonly rs2_devices_changed_callback onDevicesChangedCallback;

        public delegate void OnDevicesChangedDelegate(DeviceList removed, DeviceList added);
        public event OnDevicesChangedDelegate OnDevicesChanged;

        private void onDevicesChanged(IntPtr removedList, IntPtr addedList, IntPtr userData)
        {
            var e = OnDevicesChanged;
            if (e != null)
            {
                using (var removed = new DeviceList(removedList))
                using (var added = new DeviceList(addedList))
                    e(removed, added);
            }
        }


        /// <summary>
        /// create a static snapshot of all connected devices at the time of the call
        /// </summary>
        /// <returns></returns>
        public DeviceList QueryDevices(bool include_platform_camera = false)
        {
            object error;
            var ptr = NativeMethods.rs2_query_devices_ex(m_instance.Handle,
                include_platform_camera ? 0xff : 0xfe, out error);
            return new DeviceList(ptr);
        }

        /// <summary>
        /// create a static snapshot of all connected devices at the time of the call
        /// </summary>
        public DeviceList Devices
        {
            get
            {
                return QueryDevices();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    OnDevicesChanged = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                if (m_instance.Handle != IntPtr.Zero)
                {
                    NativeMethods.rs2_delete_context(m_instance.Handle);
                    m_instance = new HandleRef(this, IntPtr.Zero);
                }

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Context()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }


}
