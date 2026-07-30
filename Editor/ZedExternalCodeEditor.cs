using System;
using System.IO;
using System.Linq;
using Unity.CodeEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using Microsoft.Unity.VisualStudio.Editor;

namespace UnityZed
{
    public partial class ZedExternalCodeEditor : IExternalCodeEditor
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
            => CodeEditor.Register(new ZedExternalCodeEditor());

        private static IGenerator CreateSdkStyleGeneration()
        {
            var assembly = typeof(IGenerator).Assembly;
            var type = assembly.GetType("Microsoft.Unity.VisualStudio.Editor.SdkStyleProjectGeneration");
            return (IGenerator)Activator.CreateInstance(type);
        }

        private static readonly ILogger sLogger = ZedLogger.Create();
        private static readonly ZedDiscovery sDiscovery = new();

        private ZedProcess m_Process;
        private ZedPreferences m_Preferences;
        private ZedSettings m_Settings;
        private IGenerator m_Generator;
        private bool m_CacheResetWarningLogged;

        public void Initialize(string editorInstallationPath)
        {
            m_Process = new(editorInstallationPath);
            m_Generator = CreateSdkStyleGeneration();
            m_Preferences = new(m_Generator);
            m_Settings = new();
            m_Settings.Sync();

            if (m_Generator.HasSolutionBeenGenerated() == false)
                m_Generator.Sync();
        }

        //
        // Discovery
        //

        public CodeEditor.Installation[] Installations
            => sDiscovery.GetInstallations();

        public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
            => sDiscovery.TryGetInstallationForPath(editorPath, out installation);

        //
        // Interopt
        //

        public bool OpenProject(string filePath = "", int line = -1, int column = -1)
        {
            Assert.IsNotNull(m_Process);
            Assert.IsNotNull(m_Generator);

            if (!string.IsNullOrEmpty(filePath) && m_Generator.IsSupportedFile(filePath) == false)
            {
                sLogger.LogWarning($"File '{filePath}' is not supported by the project generator.");
                return false;
            }

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath) == false)
            {
                sLogger.LogWarning($"File '{filePath}' does not exist.");
                return false;
            }

            m_Generator.Sync();
            m_Settings.Sync();

            return m_Process.OpenProject(filePath, line, column);
        }

        public void SyncAll()
        {
            Assert.IsNotNull(m_Generator);

            ResetProjectGenerationCache();
            AssetDatabase.Refresh();
            m_Generator.Sync();
            m_Settings.Sync();
        }

        public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
            Assert.IsNotNull(m_Generator);

            ResetProjectGenerationCache();
            m_Generator.SyncIfNeeded(
                (addedFiles ?? Array.Empty<string>())
                    .Union(deletedFiles ?? Array.Empty<string>())
                    .Union(movedFiles ?? Array.Empty<string>())
                    .Union(movedFromFiles ?? Array.Empty<string>()),
                importedFiles ?? Array.Empty<string>());
            m_Settings.Sync();
        }

        private void ResetProjectGenerationCache()
        {
            try
            {
                var provider = m_Generator.AssemblyNameProvider;
                for (var type = provider.GetType(); type != null; type = type.BaseType)
                {
                    var method = type.GetMethod(
                        "ResetPackageInfoCache",
                        System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.DeclaredOnly);
                    if (method == null)
                        continue;

                    method.Invoke(provider, null);
                    return;
                }
            }
            catch (Exception exception)
            {
                // This is an optimization for package moves and updates. Project sync must
                // continue if a future Visual Studio Editor package changes its internals.
                if (m_CacheResetWarningLogged == false)
                {
                    m_CacheResetWarningLogged = true;
                    sLogger.LogWarning($"Could not reset the project-generation cache: {exception.Message}");
                }
            }
        }

        //
        // Preference GUI
        //

        public void OnGUI()
            => m_Preferences.OnGUI();
    }
}
