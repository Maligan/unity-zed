using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.XPath;
using NiceIO;
using Unity.CodeEditor;

namespace UnityZed
{
    public class ZedDiscovery
    {
        public CodeEditor.Installation[] GetInstallations()
        {
            var results = new List<CodeEditor.Installation>();

            var candidates = new List<(NPath path, TryGetVersion tryGetVersion)> {

                // [MacOS]
                ("/Applications/Zed.app/Contents/MacOS/cli", TryGetVersionFromPlist),
                ("/Applications/Zed Preview.app/Contents/MacOS/cli", TryGetVersionFromPlist),
                ("/usr/local/bin/zed", null),

                // [Linux] (Flatpak)
                ("/var/lib/flatpak/app/dev.zed.Zed/current/active/files/bin/zed", null),

                // [Linux] (Repo) 
                ("/usr/bin/zeditor", null),

                // [Linux] (NixOS)
                ("/run/current-system/sw/bin/zeditor", null),
                // [Linux] (NixOS HomeManager from Zed Flake)
                ($"/etc/profiles/per-user/{Environment.UserName}/bin/zed", null),
                // [Linux] (NixOS HomeManager from NixPkgs)
                ($"/etc/profiles/per-user/{Environment.UserName}/bin/zeditor", null),

                // [Linux] (Official Website)
                (NPath.HomeDirectory.Combine(".local/bin/zed"), null),
                (NPath.HomeDirectory.Combine(".local/zed.app/bin/zed"), null),
            };

            AddWindowsCandidates(candidates);
            AddPathCandidates(candidates);

            foreach (var candidate in candidates)
            {
                var candidatePath = candidate.path;
                var candidateTryGetVersion = candidate.tryGetVersion ?? TryGetVersionFallback;

                try
                {
                    if (candidatePath.FileExists() == false)
                        continue;

                    var name = new StringBuilder("Zed");

                    if (candidateTryGetVersion(candidatePath, out var version))
                        name.Append($" [{version}]");

                    var installation = new CodeEditor.Installation
                    {
                        Name = name.ToString(),
                        Path = candidatePath.MakeAbsolute().ToString(),
                    };

                    if (results.Exists(result => string.Equals(result.Path, installation.Path, StringComparison.OrdinalIgnoreCase)) == false)
                        results.Add(installation);
                }
                catch (Exception)
                {
                    // One inaccessible or malformed candidate must not prevent discovery
                    // in all other standard locations and PATH entries.
                }
            }

            return results.ToArray();
        }

        public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
        {
            foreach (var installed in GetInstallations())
            {
                if (installed.Path == editorPath)
                {
                    installation = installed;
                    return true;
                }
            }

            // Unity allows selecting an executable manually. Do not reject a valid custom,
            // preview, portable, or future Zed install merely because it is not in our list.
            if (string.IsNullOrWhiteSpace(editorPath))
            {
                installation = default;
                return false;
            }

            try
            {
                var customPath = new NPath(editorPath);
                if (customPath.FileExists() && customPath.FileNameWithoutExtension.StartsWith("zed", StringComparison.OrdinalIgnoreCase))
                {
                    installation = new()
                    {
                        Name = "Zed [Custom]",
                        Path = customPath.MakeAbsolute().ToString(),
                    };
                    return true;
                }
            }
            catch (ArgumentException)
            {
                // Invalid paths can be stored by older Unity preferences. Treat them as
                // unsupported rather than breaking the External Tools preferences UI.
            }

            installation = default;
            return false;
        }

        //
        // TryGetVersion implementations
        //
        private static void AddWindowsCandidates(List<(NPath path, TryGetVersion tryGetVersion)> candidates)
        {
            AddCandidate(candidates, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs/Zed/Zed.exe");
            AddCandidate(candidates, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Zed/Zed.exe");
            AddCandidate(candidates, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zed/Zed.exe");
        }

        private static void AddPathCandidates(List<(NPath path, TryGetVersion tryGetVersion)> candidates)
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(path))
                return;

            foreach (var directory in path.Split(System.IO.Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(directory))
                    continue;

                try
                {
                    var candidateDirectory = new NPath(directory.Trim().Trim('"'));
                    candidates.Add((candidateDirectory.Combine("zed"), null));
                    candidates.Add((candidateDirectory.Combine("zeditor"), null));
                    candidates.Add((candidateDirectory.Combine("zed.exe"), null));
                }
                catch (ArgumentException)
                {
                    // Ignore malformed PATH entries; explicitly configured candidates and
                    // all remaining entries should still be considered.
                }
            }
        }

        private static void AddCandidate(List<(NPath path, TryGetVersion tryGetVersion)> candidates, string directory, string relativePath)
        {
            if (string.IsNullOrEmpty(directory) == false)
                candidates.Add((new NPath(directory).Combine(relativePath), null));
        }

        private delegate bool TryGetVersion(NPath path, out string version);

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
    }
}
