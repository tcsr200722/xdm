﻿//using Microsoft.Win32;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Runtime.InteropServices;
//using System.Text;
//using XDM.Core;

//namespace NativeMessaging
//{
//    public static class NativeMessagingHostConfigurer
//    {
//        private static string GetExecutablePath()
//        {
//            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "XDM.App.Host" + Path.DirectorySeparatorChar + "xdm-app-host" + (
//                Environment.OSVersion.Platform == PlatformID.Win32NT ? ".exe" : string.Empty));
//            //#if NET472
//            //            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
//            //                    @"microsoft\windowsapps\xdm-app-host.exe");
//            //#else
//            //            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "XDM.App.Host" + Path.DirectorySeparatorChar + "xdm-app-host" + (
//            //                Environment.OSVersion.Platform == PlatformID.Win32NT ? ".exe" : string.Empty));
//            //#endif
//        }

//        //private static string GetFirefoxBatchPath()
//        //{
//        //    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "xdm-app-host.bat");
//        //}

//        private static void CreateMessagingHostManifest(Browser browser, string appName, string manifestPath)
//        {
//            var extensions = new List<string> { browser == Browser.Firefox ? "xdm-integration-module@subhra74.github.io" : "chrome-extension://akdmdglbephckgfmdffcdebnpjgamofc/" };
//            try
//            {
//                var file = Path.Combine(Config.AppDir, "extension.txt");
//                if (File.Exists(file))
//                {
//                    extensions.AddRange(File.ReadAllLines(file));
//                }
//            }
//            catch { }
//            var folder = Path.GetDirectoryName(manifestPath)!;
//            if (!Directory.Exists(folder))
//            {
//                Directory.CreateDirectory(folder);
//            }
//            string pathToExe = GetExecutablePath();

//            //#if WINDOWS
//            //            string? batchFilePath = null;
//            //            if (browser == Browser.Firefox)
//            //            {
//            //                batchFilePath = GetFirefoxBatchPath();
//            //                File.WriteAllText(batchFilePath, $"@echo off\r\n\"{aliasPath}\"");
//            //                aliasPath = batchFilePath;
//            //            }
//            //#endif

//            using var stream = new FileStream(manifestPath, FileMode.Create);
//            using var textWriter = new StreamWriter(stream);
//            using var writer = new JsonTextWriter(textWriter);
//            writer.Formatting = Formatting.Indented;
//            writer.WriteStartObject();
//            writer.WritePropertyName("name");
//            writer.WriteValue(appName);
//            writer.WritePropertyName("description");
//            writer.WriteValue("Native messaging host for Xtreme Download Manager");
//            writer.WritePropertyName("path");
//            writer.WriteValue(pathToExe);
//            writer.WritePropertyName("type");
//            writer.WriteValue("stdio");
//            writer.WritePropertyName(browser == Browser.Firefox ? "allowed_extensions" : "allowed_origins");
//            writer.WriteStartArray();
//            foreach (var extension in extensions)
//            {
//                if (!string.IsNullOrEmpty(extension))
//                {
//                    writer.WriteValue(extension);
//                }
//            }
//            writer.WriteEndArray();
//            writer.WriteEndObject();
//            writer.Close();
//        }

//#if WINDOWS
//        private static bool IsMessagingHostAlreadyInstalledForChrome()
//        {
//            try
//            {
//                using var regKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\xdm_chrome.native_host");
//                var path = (string)regKey.GetValue(null);
//                if (path == Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine("XDM.App.Host", "xdm_chrome.native_host.json")))
//                {
//                    return true;
//                }
//            }
//            catch { }
//            return false;
//        }

//        private static bool IsMessagingHostAlreadyInstalledForFirefox()
//        {
//            try
//            {
//                using var regKey = Registry.CurrentUser.OpenSubKey(@"Software\Mozilla\NativeMessagingHosts\xdmff.native_host");
//                var path = (string)regKey.GetValue(null);
//                if (path == Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine("XDM.App.Host", "xdmff.native_host.json")))
//                {
//                    return true;
//                }
//            }
//            catch { }
//            return false;
//        }

//        public static void InstallNativeMessagingHostForWindows(Browser browser)
//        {
//            //if (browser == Browser.Firefox && IsMessagingHostAlreadyInstalledForFirefox()) { return; }
//            //if (browser == Browser.Chrome && IsMessagingHostAlreadyInstalledForChrome()) { return; }
//            var appName = browser == Browser.Firefox ? "xdmff.native_host" :
//                    "xdm_chrome.native_host";
//            var manifestPath = Path.Combine(Config.AppDir, $"{appName}.json");
//            CreateMessagingHostManifest(browser, appName, manifestPath);
//            var regPath = (browser == Browser.Firefox ?
//                @"Software\Mozilla\NativeMessagingHosts\" :
//                @"Software\Google\Chrome\NativeMessagingHosts");
//            using var regKey = Registry.CurrentUser.CreateSubKey(regPath);
//            using var key = regKey.CreateSubKey(appName, RegistryKeyPermissionCheck.ReadWriteSubTree);
//            key.SetValue(null, manifestPath);
//        }
//#endif

//#if LINUX
//        public static void InstallNativeMessagingHostForLinux(Browser browser)
//        {
//            var appName = browser == Browser.Firefox ? "xdmff.native_host" :
//                    "xdm_chrome.native_host";
//            string manifestPath;
//            var home = Environment.GetEnvironmentVariable("HOME")!;
//            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
//            {
//                manifestPath = Path.Combine(home, browser == Browser.Firefox ?
//                        $"Library/Application Support/Mozilla/NativeMessagingHosts/{appName}.json" :
//                        $"Library/Application Support/Google/Chrome/NativeMessagingHosts/{appName}.json");
//            }
//            else
//            {
//                manifestPath = Path.Combine(home, browser == Browser.Firefox ?
//                        $".mozilla/native-messaging-hosts/{appName}.json" :
//                        $".config/google-chrome/NativeMessagingHosts/{appName}.json");
//            }
//            CreateMessagingHostManifest(browser, appName, manifestPath);
//        }
//#endif

//    }

//    public enum Browser
//    {
//        Chrome, Firefox, MSEdge
//    }
//}
