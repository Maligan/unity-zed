using Unity.CodeEditor;
using UnityEngine;

namespace UnityZed
{
    public class ZedProcess
    {
        private static readonly ILogger sLogger = ZedLogger.Create();

        private readonly NPath m_ExecPath;
        private readonly NPath m_ProjectPath;

        public ZedProcess(string path)
        {
            m_ExecPath = path;
            m_ProjectPath = new NPath(Application.dataPath).Parent;
        }

        public bool OpenProject(string filePath = "", int line = -1, int column = -1)
        {
            sLogger.Log("OpenProject");
            
            line = Mathf.Max(0, line);
            column = Mathf.Max(0, column);

            return CodeEditor.OSOpenFile(m_ExecPath.ToString(), $"{m_ProjectPath} {filePath}:{line}:{column}");
        }

        public void SyncAll()
            => sLogger.Log("SyncAll");

        public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
            => sLogger.Log("SyncIfNeeded");
    }
}
