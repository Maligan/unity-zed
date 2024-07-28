using Unity.CodeEditor;
using System.Collections.Generic;
using System.Xml.XPath;
using System.Text;

namespace UnityZed
{
    public class ZedDiscovery
    {
        public CodeEditor.Installation[] GetInstallations()
        {
            var results = new List<CodeEditor.Installation>();

            var candidates = new NPath[] {
                new ("/Applications/Zed.app/Contents/MacOS/Zed"),
                new ("/usr/local/bin/zed"),
            };

            foreach (var candidate in candidates)
            {
                if (candidate.FileExists())
                {
                    var name = new StringBuilder("Zed");

                    var version = GetVersionFromPlist(candidate);
                    if (version != null)
                        name.Append($" [{version}]");

                    results.Add(new() {
                        Name = name.ToString(),
                        Path = candidate.MakeAbsolute().ToString(),
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

        private static string GetVersionFromPlist(NPath path)
        {
            var plistPath = path.Combine("../../").Combine("Info.plist");
            if (plistPath.FileExists() == false)
                return null;

            var xPath = new XPathDocument(plistPath.ToString());
            var xNavigator = xPath.CreateNavigator().SelectSingleNode("/plist/dict/key[text()='CFBundleShortVersionString']/following-sibling::string[1]/text()");
            if (xNavigator == null)
                return null;

            return xNavigator.Value;
        }
    }
}
