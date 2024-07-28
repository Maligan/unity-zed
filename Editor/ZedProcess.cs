using UnityEngine;

namespace UnityZed
{
    public class ZedProcess
    {
        private static readonly ILogger sLogger = ZedLogger.Create();

        private readonly NPath m_Path;

        public ZedProcess(string path)
        {
            m_Path = path;
        }

        public bool OpenProject(string filePath = "", int line = -1, int column = -1)
        {
            // TODO: At the moment, Zed does not support opening a line/column from the command line
            //       and while IPC isn't implemented between this class & zed plugin, we can open new files
            //       on existing zed instance. So we just return 'false' here and unity will call zed by itself.

            sLogger.Log("OpenProject");
            return false;
        }

        public void SyncAll()
            => sLogger.Log("SyncAll");

        public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
            => sLogger.Log("SyncIfNeeded");
    }
}
