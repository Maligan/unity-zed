using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using Microsoft.Unity.VisualStudio.Editor;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityZedIntegration
{
    public class Zed
    {
        string editorInstallationPath;
        string projectPath;

        readonly IGenerator generator;

        public Zed()
        {
            var assembly = typeof(IGenerator).Assembly;
            var type = assembly.GetType("Microsoft.Unity.VisualStudio.Editor.SdkStyleProjectGeneration");
            generator = (IGenerator)Activator.CreateInstance(type);
        }

        public void setEditorInstallationPath(string path)
        {
            this.editorInstallationPath = path;
            projectPath = Directory.GetParent(Application.dataPath).FullName;
        }

        public void addSettingsFileIfNeeded()
        {
            const string defaultSettings = @"{
            ""file_scan_exclusions"": [
                ""**/.*"",
                ""**/*~"",

                ""*.csproj"",
                ""*.sln"",

                ""**/*.meta"",
                ""**/*.booproj"",
                ""**/*.pibd"",
                ""**/*.suo"",
                ""**/*.user"",
                ""**/*.userprefs"",
                ""**/*.unityproj"",
                ""**/*.dll"",
                ""**/*.exe"",
                ""**/*.pdf"",
                ""**/*.mid"",
                ""**/*.midi"",
                ""**/*.wav"",
                ""**/*.gif"",
                ""**/*.ico"",
                ""**/*.jpg"",
                ""**/*.jpeg"",
                ""**/*.png"",
                ""**/*.psd"",
                ""**/*.tga"",
                ""**/*.tif"",
                ""**/*.tiff"",
                ""**/*.3ds"",
                ""**/*.3DS"",
                ""**/*.fbx"",
                ""**/*.FBX"",
                ""**/*.lxo"",
                ""**/*.LXO"",
                ""**/*.ma"",
                ""**/*.MA"",
                ""**/*.obj"",
                ""**/*.OBJ"",
                ""**/*.asset"",
                ""**/*.cubemap"",
                ""**/*.flare"",
                ""**/*.mat"",
                ""**/*.meta"",
                ""**/*.prefab"",
                ""**/*.unity"",

                ""build/"",
                ""Build/"",
                ""library/"",
                ""Library/"",
                ""obj/"",
                ""Obj/"",
                ""ProjectSettings/"",
                ""UserSettings/"",
                ""temp/"",
                ""Temp/"",
                ""logs"",
                ""Logs"",
            ]
        }";

            var settingsPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ".zed", "settings.json");
            if (!File.Exists(settingsPath))
            {
                Debug.Log("[ZED] settings file not found, creating default settings file.");
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                File.WriteAllText(settingsPath, defaultSettings);
            }
        }

        public void syncAll()
        {
            generator.Sync();
            addSettingsFileIfNeeded();
        }

        public void syncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
            generator.SyncIfNeeded(addedFiles.Union(deletedFiles).Union(movedFiles).Union(movedFromFiles), importedFiles);
        }

        public bool openProject(string filePath = "", int line = -1, int column = -1)
        {
            if (!string.IsNullOrEmpty(filePath) && !generator.IsSupportedFile(filePath))
            {
                Debug.Log($"[ZED] File '{filePath}' is not supported by the generator.");
                return false;
            }

            generator.Sync();
            addSettingsFileIfNeeded();

            var args = new StringBuilder($"\"{projectPath}\" ");

            if (!string.IsNullOrEmpty(filePath))
            {
                string fileArg = filePath;
                if (line >= 0)
                {
                    fileArg += $":{line}";
                    if (column >= 0) fileArg += $":{column}";
                }
                args.Append($"\"{fileArg}\"");
            }

            //Debug.Log(editorInstallationPath + " " + args);
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = editorInstallationPath,
                    Arguments = args.ToString(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (Process.Start(startInfo))
                {
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ZED] Failed to start Zed: {ex.Message}");
                return false;
            }
        }

        public void drawExternalTool()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var version = "0.9";
            var displayName = "Unity Zed";
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(GetType().Assembly);
            if (package != null)
            {
                version = package.version;
                displayName = package.displayName;
            }

            var style = new GUIStyle
            {
                richText = true,
                margin = new RectOffset(0, 4, 0, 0)
            };

            GUILayout.Label($"<color=grey>{displayName} v{version} enabled</color>", style);
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Generate .csproj files for:");
            EditorGUI.indentLevel++;
            settingsButton(ProjectGenerationFlag.Embedded, "Embedded packages", "");
            settingsButton(ProjectGenerationFlag.Local, "Local packages", "");
            settingsButton(ProjectGenerationFlag.Registry, "Registry packages", "");
            settingsButton(ProjectGenerationFlag.Git, "Git packages", "");
            settingsButton(ProjectGenerationFlag.BuiltIn, "Built-in packages", "");
            settingsButton(ProjectGenerationFlag.LocalTarBall, "Local tarball", "");
            settingsButton(ProjectGenerationFlag.Unknown, "Packages from unknown sources", "");
            settingsButton(ProjectGenerationFlag.PlayerAssemblies, "Player projects", "For each player project generate an additional csproj with the name 'project-player.csproj'");
            var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            rect.width = 252;
            if (GUI.Button(rect, "Regenerate project files"))
            {
                generator.Sync();
            }

            EditorGUI.indentLevel--;

            void settingsButton(ProjectGenerationFlag preference, string guiMessage, string toolTip)
            {
                var prevValue = generator.AssemblyNameProvider.ProjectGenerationFlag.HasFlag(preference);

                var newValue = EditorGUILayout.Toggle(new GUIContent(guiMessage, toolTip), prevValue);
                if (newValue != prevValue)
                {
                    generator.AssemblyNameProvider.ToggleProjectGeneration(preference);
                }
            }
        }

        public CodeEditor.Installation[] getInstallations()
        {
            var results = new List<CodeEditor.Installation>();

            var candidates = new List<(string path, TryGetVersion tryGetVersion)>
            {
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
                (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "zed"), null),
            };

#if UNITY_EDITOR_WIN
            // [Windows] Default install locations
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var windowsCandidates = new[]
            {
                Path.Combine(programFiles, "Zed", "bin", "zed.exe"),
                Path.Combine(programFilesX86, "Zed", "bin", "zed.exe"),
                Path.Combine(localAppData, "Programs", "Zed", "bin", "zed.exe"), // common for user-level installs
                Path.Combine(localAppData, "Zed", "bin", "zed.exe")
            };

            foreach (var winPath in windowsCandidates)
            {
                candidates.Add((winPath, null));
            }

            // [Windows] Check if 'zed.exe' is in PATH
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                var pathDirs = pathEnv.Split(Path.PathSeparator);
                foreach (var dir in pathDirs)
                {
                    var exePath = Path.Combine(dir.Trim(), "zed.exe");
                    if (File.Exists(exePath))
                    {
                        candidates.Add((exePath, null));
                    }
                }
            }
#endif

            foreach (var candidate in candidates)
            {
                var candidatePath = candidate.path;
                var candidateTryGetVersion = candidate.tryGetVersion ?? TryGetVersionFallback;

                if (File.Exists(candidatePath))
                {
                    var name = new StringBuilder("Zed");

                    if (candidateTryGetVersion(candidatePath, out var version))
                        name.Append($" [{version}]");

                    results.Add(new()
                    {
                        Name = name.ToString(),
                        Path = Path.GetFullPath(candidatePath),
                    });

                    break;
                }
            }

            return results.ToArray();

            static bool TryGetVersionFallback(string path, out string version)
            {
                version = null;
                return false;
            }

            static bool TryGetVersionFromPlist(string path, out string version)
            {
                version = null;

                var plistPath = Path.GetFullPath(Path.Combine(path, "..", "..", "Info.plist"));
                if (File.Exists(plistPath) == false)
                    return false;

                var xPath = new XPathDocument(plistPath);
                var xNavigator = xPath.CreateNavigator().SelectSingleNode("/plist/dict/key[text()='CFBundleShortVersionString']/following-sibling::string[1]/text()");
                if (xNavigator == null)
                    return false;

                version = xNavigator.Value;
                return true;
            }
        }

        public bool tryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
        {
            foreach (var installed in getInstallations())
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


        delegate bool TryGetVersion(string path, out string version);
    }
}
