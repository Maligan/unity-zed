using System;
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

        public void Initialize(string editorInstallationPath)
        {
            m_Process = new(editorInstallationPath);
            m_Generator = CreateSdkStyleGeneration();
            m_Preferences = new(m_Generator);
            m_Settings = new();
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

            if (!string.IsNullOrEmpty(filePath) && !m_Generator.IsSupportedFile(filePath))
            {
                sLogger.Log($"File '{filePath}' is not supported by the generator.");
                return false;
            }

            m_Generator.Sync();
            m_Settings.Sync();

            return m_Process.OpenProject(filePath, line, column);
        }

        public void SyncAll()
        {
            Assert.IsNotNull(m_Generator);

            m_Generator.Sync();
            m_Settings.Sync();
        }

        public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
            Assert.IsNotNull(m_Generator);

            m_Generator.SyncIfNeeded(addedFiles.Union(deletedFiles).Union(movedFiles).Union(movedFromFiles), importedFiles);
        }

        //
        // Preference GUI
        //

        public void OnGUI()
            => m_Preferences.OnGUI();
    }
}
