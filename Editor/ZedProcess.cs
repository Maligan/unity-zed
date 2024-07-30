using System.Text;
using Unity.CodeEditor;
using UnityEngine;

namespace UnityZed
{
    public class ZedProcess
    {
        private static readonly ILogger sLogger = ZedLogger.Create();

        private readonly NPath m_ExecPath;
        private readonly NPath m_ProjectPath;

        public ZedProcess(string execPath)
        {
            m_ExecPath = execPath;
            m_ProjectPath = new NPath(Application.dataPath).Parent;
        }

        public bool OpenProject(string filePath = "", int line = -1, int column = -1)
        {
            sLogger.Log("OpenProject");

            // always add project path
            var args = new StringBuilder($"{m_ProjectPath}");

            // if file path is provided, add it too
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
    }
}
