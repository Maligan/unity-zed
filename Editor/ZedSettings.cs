using UnityEngine;
using NiceIO;
using SimpleJSON;
using System;

namespace UnityZed
{
    public class ZedSettings
    {
        private static readonly ILogger sLogger = ZedLogger.Create();

        private readonly NPath m_SettingsPath;

        public ZedSettings()
        {
            m_SettingsPath = new NPath(Application.dataPath).Parent.Combine(".zed/settings.json");
        }

        public void Sync()
        {
            if (m_SettingsPath.FileExists() == false)
            {
                sLogger.Log("Zed settings file not found, creating default settings file.");
                m_SettingsPath.CreateFile();
                m_SettingsPath.WriteAllText(JSON.Parse(kDefaultSettings).ToString());
            }
        }

        private const string kDefaultSettings = @"{
            ""file_scan_exclusions"": [
                ""**/.*"",
                ""**/*~"",

                ""*.csproj"",
                ""*.sln"",

                ""**/*.meta"",
                ""**/*.booproj"",
                ""**/*.pibd"",
                ""**/*.suo"",
                ""**/*.user"",
                ""**/*.userprefs"",
                ""**/*.unityproj"",
                ""**/*.dll"",
                ""**/*.exe"",
                ""**/*.pdf"",
                ""**/*.mid"",
                ""**/*.midi"",
                ""**/*.wav"",
                ""**/*.gif"",
                ""**/*.ico"",
                ""**/*.jpg"",
                ""**/*.jpeg"",
                ""**/*.png"",
                ""**/*.psd"",
                ""**/*.tga"",
                ""**/*.tif"",
                ""**/*.tiff"",
                ""**/*.3ds"",
                ""**/*.3DS"",
                ""**/*.fbx"",
                ""**/*.FBX"",
                ""**/*.lxo"",
                ""**/*.LXO"",
                ""**/*.ma"",
                ""**/*.MA"",
                ""**/*.obj"",
                ""**/*.OBJ"",
                ""**/*.asset"",
                ""**/*.cubemap"",
                ""**/*.flare"",
                ""**/*.mat"",
                ""**/*.meta"",
                ""**/*.prefab"",
                ""**/*.unity"",

                ""build/"",
                ""Build/"",
                ""library/"",
                ""Library/"",
                ""obj/"",
                ""Obj/"",
                ""ProjectSettings/"",
                ""UserSettings/"",
                ""temp/"",
                ""Temp/"",
                ""logs"",
                ""Logs"",
            ]
        }";
    }
}
