using System;
using System.Collections.Generic;
using NiceIO;
using SimpleJSON;
using UnityEngine;

namespace UnityZed
{
    public class ZedSettings
    {
        private static readonly ILogger sLogger = ZedLogger.Create();
        private static readonly HashSet<string> sLoggedWarnings = new();

        private readonly NPath m_ProjectPath;

        public ZedSettings()
        {
            m_ProjectPath = new NPath(Application.dataPath).Parent;
        }

        public void Sync()
        {
            // A read-only checkout or one malformed configuration file must never stop
            // Unity from opening the requested script. Keep every artifact independent.
            SyncSafely("Zed workspace settings", SyncSettings);
            SyncSafely("OmniSharp settings", SyncOmniSharpSettings);
        }

        private static void SyncSafely(string description, Action sync)
        {
            try
            {
                sync();
            }
            catch (Exception exception)
            {
                LogWarningOnce($"{description}:{exception.Message}", $"Could not synchronize {description}: {exception.Message}");
            }
        }

        private void SyncSettings()
        {
            var path = m_ProjectPath.Combine(".zed/settings.json");
            var settings = ReadObject(path);
            if (settings == null)
                return;

            var exclusions = GetOrCreateArray(settings, "file_scan_exclusions", path);
            if (exclusions == null)
                return;

            foreach (var exclusion in kFileScanExclusions)
                AddIfMissing(exclusions, exclusion);

            var fileTypes = GetOrCreateObject(settings, "file_types", path);
            if (fileTypes == null)
                return;

            foreach (var association in kUnityFileTypes)
            {
                var extensions = GetOrCreateArray(fileTypes, association.language, path);
                if (extensions == null)
                    continue;

                foreach (var extension in association.extensions)
                    AddIfMissing(extensions, extension);
            }

            var lsp = GetOrCreateObject(settings, "lsp", path);
            if (lsp == null)
                return;

            var roslyn = GetOrCreateObject(lsp, "roslyn", path);
            var roslynSettings = roslyn == null ? null : GetOrCreateObject(roslyn, "settings", path);
            if (roslynSettings != null)
            {
                var backgroundAnalysis = GetOrCreateObject(roslynSettings, "csharp|background_analysis", path);
                if (backgroundAnalysis != null)
                {
                    SetIfMissing(backgroundAnalysis, "dotnet_analyzer_diagnostics_scope", "fullSolution");
                    SetIfMissing(backgroundAnalysis, "dotnet_compiler_diagnostics_scope", "fullSolution");
                }

                var completion = GetOrCreateObject(roslynSettings, "csharp|completion", path);
                if (completion != null)
                {
                    SetIfMissing(completion, "dotnet_show_name_completion_suggestions", true);
                    SetIfMissing(completion, "dotnet_show_completion_items_from_unimported_namespaces", true);
                    SetIfMissing(completion, "dotnet_trigger_completion_in_argument_lists", true);
                }

                var navigation = GetOrCreateObject(roslynSettings, "csharp|navigation", path);
                if (navigation != null)
                {
                    SetIfMissing(navigation, "dotnet_navigate_to_decompiled_sources", true);
                    SetIfMissing(navigation, "dotnet_navigate_to_source_link_and_embedded_sources", true);
                }
            }

            var csharpLs = GetOrCreateObject(lsp, "csharp-ls", path);
            var csharpLsSettings = csharpLs == null ? null : GetOrCreateObject(csharpLs, "settings", path);
            if (csharpLsSettings != null)
                SetIfMissing(csharpLsSettings, "analyzersEnabled", true);

            Write(path, settings);
        }

        private void SyncOmniSharpSettings()
        {
            var path = m_ProjectPath.Combine("omnisharp.json");
            var settings = ReadObject(path);
            if (settings == null)
                return;

            // Unity's generated projects contain references to Unity.Analyzers.dll. OmniSharp
            // does not load those analyzers unless Roslyn extension support is enabled.
            var roslyn = GetOrCreateObject(settings, "RoslynExtensionsOptions", path);
            if (roslyn == null)
                return;

            SetIfMissing(roslyn, "enableAnalyzersSupport", true);
            SetIfMissing(roslyn, "enableImportCompletion", true);
            SetIfMissing(roslyn, "analyzeOpenDocumentsOnly", false);

            var formatting = GetOrCreateObject(settings, "FormattingOptions", path);
            if (formatting == null)
                return;

            SetIfMissing(formatting, "enableEditorConfigSupport", true);

            Write(path, settings);
        }

        private static JSONObject ReadObject(NPath path)
        {
            if (path.FileExists() == false)
                return new JSONObject();

            try
            {
                var contents = path.ReadAllText();
                if (HasJsonComments(contents))
                {
                    LogWarningOnce($"{path}:comments", $"'{path}' contains comments; leaving it unchanged to preserve them.");
                    return null;
                }

                var result = JSON.Parse(contents);
                if (result.IsObject)
                    return result as JSONObject;

                LogWarningOnce($"{path}:object", $"'{path}' must contain a JSON object; leaving it unchanged.");
            }
            catch (Exception exception)
            {
                LogWarningOnce($"{path}:{exception.Message}", $"Could not update '{path}': {exception.Message}");
            }

            return null;
        }

        private static bool HasJsonComments(string contents)
        {
            var insideString = false;
            var escaped = false;

            for (var index = 0; index < contents.Length - 1; index++)
            {
                var character = contents[index];
                if (insideString)
                {
                    if (escaped)
                        escaped = false;
                    else if (character == '\\')
                        escaped = true;
                    else if (character == '"')
                        insideString = false;

                    continue;
                }

                if (character == '"')
                {
                    insideString = true;
                    continue;
                }

                if (character == '/' && (contents[index + 1] == '/' || contents[index + 1] == '*'))
                    return true;
            }

            return false;
        }

        private static JSONObject GetOrCreateObject(JSONObject parent, string key, NPath path)
        {
            if (parent.HasKey(key))
            {
                if (parent[key].IsObject)
                    return parent[key] as JSONObject;

                LogWarningOnce($"{path}:{key}:object", $"'{path}' setting '{key}' must be an object; leaving it unchanged.");
                return null;
            }

            var result = new JSONObject();
            parent[key] = result;
            return result;
        }

        private static JSONArray GetOrCreateArray(JSONObject parent, string key, NPath path)
        {
            if (parent.HasKey(key))
            {
                if (parent[key].IsArray)
                    return parent[key] as JSONArray;

                LogWarningOnce($"{path}:{key}:array", $"'{path}' setting '{key}' must be an array; leaving it unchanged.");
                return null;
            }

            var result = new JSONArray();
            parent[key] = result;
            return result;
        }

        private static void SetIfMissing(JSONObject parent, string key, bool value)
        {
            if (parent.HasKey(key) == false)
                parent[key] = value;
        }

        private static void SetIfMissing(JSONObject parent, string key, string value)
        {
            if (parent.HasKey(key) == false)
                parent[key] = value;
        }

        private static void AddIfMissing(JSONArray array, string value)
        {
            foreach (var child in array.Children)
                if (child.Value == value)
                    return;

            array.Add(value);
        }

        private static void LogWarningOnce(string key, string message)
        {
            if (sLoggedWarnings.Add(key))
                sLogger.LogWarning(message);
        }

        private static void Write(NPath path, JSONNode contents)
        {
            path.Parent.CreateDirectory();
            path.ReplaceAllText(contents.ToString(4) + Environment.NewLine);
        }

        private static readonly string[] kFileScanExclusions =
        {
            "**/.*",
            "**/*~",
            "*.csproj",
            "*.sln",
            "**/*.meta",
            "**/*.booproj",
            "**/*.pibd",
            "**/*.suo",
            "**/*.user",
            "**/*.userprefs",
            "**/*.unityproj",
            "**/*.dll",
            "**/*.exe",
            "**/*.pdf",
            "**/*.mid",
            "**/*.midi",
            "**/*.wav",
            "**/*.gif",
            "**/*.ico",
            "**/*.jpg",
            "**/*.jpeg",
            "**/*.png",
            "**/*.psd",
            "**/*.tga",
            "**/*.tif",
            "**/*.tiff",
            "**/*.3ds",
            "**/*.3DS",
            "**/*.fbx",
            "**/*.FBX",
            "**/*.lxo",
            "**/*.LXO",
            "**/*.ma",
            "**/*.MA",
            "**/*.obj",
            "**/*.OBJ",
            "**/*.asset",
            "**/*.cubemap",
            "**/*.flare",
            "**/*.mat",
            "**/*.prefab",
            "**/*.unity",
            "build/",
            "Build/",
            "library/",
            "Library/",
            "obj/",
            "Obj/",
            "ProjectSettings/",
            "UserSettings/",
            "temp/",
            "Temp/",
            "logs/",
            "Logs/",
        };

        private static readonly (string language, string[] extensions)[] kUnityFileTypes =
        {
            ("JSON", new[] { "*.asmdef", "*.asmref" }),
            ("HLSL", new[] { "*.shader", "*.compute", "*.cginc", "*.hlsl", "*.raytrace" }),
            ("GLSL", new[] { "*.glslinc" }),
            ("XML", new[] { "*.uxml" }),
            ("CSS", new[] { "*.uss" }),
        };
    }
}
