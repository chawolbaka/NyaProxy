/*
 *  Copyright 2012-2016 The Pkcs11Interop Project
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

/*
 *  Written for the Pkcs11Interop project by:
 *  Jaroslav IMRICH <jimrich@jimrich.sk>
 */

using System;
using System.IO;

namespace NyaProxy.API
{
    public static class Platform
    {
        static Platform() => DetectPlatform();

        public static bool Uses64BitRuntime => IntPtr.Size == 8;
        public static bool User32BitRuntime => IntPtr.Size == 4;

        public static bool IsWindows { get; private set; }
        public static bool IsLinux { get; private set; }
        public static bool IsMacOsX { get; private set; }

        
        private static void DetectPlatform()
        {
            if (IsWindows||IsLinux||IsMacOsX)
                return;

            string windir = Environment.GetEnvironmentVariable("windir");
            if (!string.IsNullOrEmpty(windir) && windir.Contains(@"\") && Directory.Exists(windir))
            {
               IsWindows = true;
            }
            else if (File.Exists(@"/proc/sys/kernel/ostype"))
            {
                string osType = File.ReadAllText(@"/proc/sys/kernel/ostype");
                if (osType.StartsWith("Linux", StringComparison.OrdinalIgnoreCase))
                {
                    // Note: Android gets here too
                    IsLinux = true;
                }
                else
                {
                    throw new PlatformNotSupportedException($"cannot supported on \"{osType}\" platform");
                }

            }
            else if (File.Exists(@"/System/Library/CoreServices/SystemVersion.plist"))
            {
                // Note: iOS gets here too
                IsMacOsX = true;
            }
            else
            {
                throw new PlatformNotSupportedException("cannot supported on this platform");
            }
        }
    }
}