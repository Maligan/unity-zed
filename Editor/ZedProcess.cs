using System.Diagnostics;
using System.IO;
using System.Text;
using NiceIO;
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
            var args = new StringBuilder($"\"{m_ProjectPath}\"");

            // if file path is provided, add it too
            if (!string.IsNullOrEmpty(filePath))
            {
                args.Append(" -a ");
                args.Append($"\"{filePath}\"");

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

            if (Application.platform == RuntimePlatform.WindowsEditor)
                return OpenOnWindows(args.ToString());

            return CodeEditor.OSOpenFile(m_ExecPath.ToString(), args.ToString());
        }

        // On Windows the chosen editor is the GUI app (Zed.exe). The `-a` and
        // `file:line:column` arguments belong to the CLI next to it (bin\zed.exe),
        // a console program, so start it without a console window.
        private bool OpenOnWindows(string args)
        {
            var exe = m_ExecPath.ToString();
            var cli = Path.Combine(Path.GetDirectoryName(exe) ?? "", "bin", "zed.exe");
            if (File.Exists(cli))
                exe = cli;

            try
            {
                var startInfo = new ProcessStartInfo(exe, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                Process.Start(startInfo)?.Dispose();
                return true;
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"[UnityZed] Failed to start '{exe}': {e.Message}");
                return false;
            }
        }
    }
}
