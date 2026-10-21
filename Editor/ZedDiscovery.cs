using Unity.CodeEditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.XPath;
using System.Text;
using NiceIO;
using UnityEngine;

namespace UnityZed
{
    public class ZedDiscovery
    {
        public CodeEditor.Installation[] GetInstallations()
        {
            var results = new List<CodeEditor.Installation>();

            var candidates = new (NPath path, TryGetVersion tryGetVersion)[] {

                // [MacOS]
                ("/Applications/Zed.app/Contents/MacOS/cli", TryGetVersionFromPlist),
                ("/usr/local/bin/zed", null),

                // [Linux] (Flatpak)
                ("/var/lib/flatpak/app/dev.zed.Zed/current/active/files/bin/zed", null),

                // [Linux] (Repo) 
                ("/usr/bin/zeditor", null),

                // [Linux] (NixOS)
                ("/run/current-system/sw/bin/zeditor", null),
                // [Linux] (NixOS HomeManager from Zed Flake)
                ("/etc/profiles/per-user/linx/bin/zed", null),
                // [Linux] (NixOS HomeManager from NixPkgs)
                ("/etc/profiles/per-user/linx/bin/zeditor", null),

                // [Linux] (Official Website)
                (NPath.HomeDirectory.Combine(".local/bin/zed"), null),

                // [Windows] (per-user installer, then system-wide installer)
                (WindowsPath(Environment.SpecialFolder.LocalApplicationData, "Programs/Zed/Zed.exe"), TryGetVersionFromExe),
                (WindowsPath(Environment.SpecialFolder.ProgramFiles, "Zed/Zed.exe"), TryGetVersionFromExe),
            };

            foreach (var candidate in candidates)
            {
                var candidatePath = candidate.path;
                var candidateTryGetVersion = candidate.tryGetVersion ?? TryGetVersionFallback;

                if (candidatePath.FileExists())
                {
                    var name = new StringBuilder("Zed");

                    if (candidateTryGetVersion(candidatePath, out var version))
                        name.Append($" [{version}]");

                    results.Add(new()
                    {
                        Name = name.ToString(),
                        Path = candidatePath.MakeAbsolute().ToString(),
                    });

                    break;
                }
            }

            return results.ToArray();
        }

        public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
        {
            foreach (var installed in GetInstallations())
            {
                if (SamePath(installed.Path, editorPath))
                {
                    installation = installed;
                    return true;
                }
            }

            installation = default;
            return false;
        }

        // Windows paths ignore case and accept either slash, so the editor path Unity
        // saved may spell the same Zed.exe differently than discovery does.
        private static bool SamePath(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return false;

            if (Application.platform != RuntimePlatform.WindowsEditor)
                return a == b;

            try
            {
                return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Off Windows these folders are missing or hold no Zed.exe, so the candidate is skipped.
        private static NPath WindowsPath(Environment.SpecialFolder folder, string relative)
        {
            var root = Environment.GetFolderPath(folder);
            if (string.IsNullOrEmpty(root))
                return new NPath("/nonexistent-zed-windows-path");

            return new NPath(Path.Combine(root, relative));
        }

        //
        // TryGetVersion implementations
        //
        private delegate bool TryGetVersion(NPath path, out string vertion);

        private static bool TryGetVersionFallback(NPath path, out string version)
        {
            version = null;
            return false;
        }

        private static bool TryGetVersionFromPlist(NPath path, out string version)
        {
            version = null;

            var plistPath = path.Combine("../../").Combine("Info.plist");
            if (plistPath.FileExists() == false)
                return false;

            var xPath = new XPathDocument(plistPath.ToString());
            var xNavigator = xPath.CreateNavigator().SelectSingleNode("/plist/dict/key[text()='CFBundleShortVersionString']/following-sibling::string[1]/text()");
            if (xNavigator == null)
                return false;

            version = xNavigator.Value;
            return true;
        }

        private static bool TryGetVersionFromExe(NPath path, out string version)
        {
            version = null;

            try
            {
                version = FileVersionInfo.GetVersionInfo(path.ToString()).ProductVersion;
            }
            catch (Exception)
            {
                return false;
            }

            return !string.IsNullOrEmpty(version);
        }
    }
}
