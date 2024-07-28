using UnityEngine;
using Unity.CodeEditor;
using UnityEngine.Assertions;
using UnityEditor;

namespace UnityZed
{
    [InitializeOnLoad]
    public partial class ZedExternalCodeEditor : IExternalCodeEditor
    {
        static ZedExternalCodeEditor() => CodeEditor.Register(new ZedExternalCodeEditor());

        private static readonly ILogger sLogger = ZedLogger.Create();
        private static readonly ZedDiscovery sDiscovery = new ();

        private ZedProcess m_Process;

        public void Initialize(string editorInstallationPath)
            => m_Process = new (editorInstallationPath);

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
            return m_Process.OpenProject(filePath, line, column);
        }

        public void SyncAll()
        {
            Assert.IsNotNull(m_Process);
            m_Process.SyncAll();
        }

        public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
            Assert.IsNotNull(m_Process, $"{nameof(ZedExternalCodeEditor)} is not initialized");
            m_Process.SyncIfNeeded(addedFiles, deletedFiles, movedFiles, movedFromFiles, importedFiles);
        }

        //
        // Preference GUI
        //

        public void OnGUI()
        {
        }
    }
}
