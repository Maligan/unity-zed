using System;
using System.Linq;
using Unity.CodeEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using Microsoft.Unity.VisualStudio.Editor;

namespace UnityZedIntegration
{
    public class ZedExternalCodeEditor : IExternalCodeEditor
    {
        readonly Zed zed = new();

        [InitializeOnLoadMethod]
        static void Initialize() => CodeEditor.Register(new ZedExternalCodeEditor());

        public void Initialize(string editorInstallationPath) => zed.setEditorInstallationPath(editorInstallationPath);
        public CodeEditor.Installation[] Installations => zed.getInstallations();
        public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation) => zed.tryGetInstallationForPath(editorPath, out installation);
        public bool OpenProject(string filePath = "", int line = -1, int column = -1) => zed.openProject(filePath, line, column);
        public void SyncAll() => zed.syncAll();
        public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles) => zed.syncIfNeeded(addedFiles, deletedFiles, movedFiles, movedFromFiles, importedFiles);
        public void OnGUI() => zed.drawExternalTool();
    }
}
