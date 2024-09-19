using Unity.CodeEditor;
using System.Collections.Generic;
using System.Xml.XPath;
using System.Text;
using NiceIO;

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
                if (installed.Path == editorPath)
                {
                    installation = installed;
                    return true;
                }
            }

            installation = default;
            return false;
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
    }
}
