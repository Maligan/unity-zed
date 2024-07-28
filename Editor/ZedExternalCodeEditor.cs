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

        static ZedExternalCodeEditor() => CodeEditor.Register(new ZedExternalCodeEditor());

        private static readonly ILogger sLogger = ZedLogger.Create();
        private static readonly ZedDiscovery sDiscovery = new();

        private ZedProcess m_Process;
        private ZedPreferences m_Preferences;
        private IGenerator m_Generator;

        public void Initialize(string editorInstallationPath)
        {
            m_Process = new(editorInstallationPath);
            m_Generator = CreateSdkStyleGeneration();
            m_Preferences = new(m_Generator);
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

            if (!m_Generator.IsSupportedFile(filePath))
                return false;

            m_Generator.Sync();

            return m_Process.OpenProject(filePath, line, column);
        }

        public void SyncAll()
        {
            Assert.IsNotNull(m_Generator);
            m_Generator.Sync();
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
