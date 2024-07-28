using System.Text;
using Unity.CodeEditor;
using UnityEngine;

namespace UnityZed
{
    public class ZedProcess
    {
        private static readonly ILogger sLogger = ZedLogger.Create();

        private readonly NPath m_ExecPath;
        private readonly NPath m_AssetsPath;
        private readonly NPath m_PackagesPath;

        public ZedProcess(string path)
        {
            m_ExecPath = path;
            m_AssetsPath = new NPath(Application.dataPath);
            m_PackagesPath = m_AssetsPath.Parent.Combine("Packages");
        }

        public bool OpenProject(string filePath = "", int line = -1, int column = -1)
        {
            sLogger.Log("OpenProject");

            var args = new StringBuilder($"{m_AssetsPath} {m_PackagesPath}");

            if (!string.IsNullOrEmpty(filePath))
            {
                args.Append(" -a ");
                args.Append(filePath);

                if (line >= 0)
                {
                    args.Append(":");
                    args.Append(line);

                    if (column >= 0)
                    {
                        args.Append(":");
                        args.Append(column);
                    }
                }
            }

            return CodeEditor.OSOpenFile(m_ExecPath.ToString(), args.ToString());
        }

        public void SyncAll()
            => sLogger.Log("SyncAll");

        public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
            => sLogger.Log("SyncIfNeeded");
    }
}
