using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using Unity.CodeEditor;
using UnityEditor;
using UnityEditor.Compilation;

namespace UnityZed
{
    internal class ZedProjectPostprocessor : AssetPostprocessor
    {
        private const string kNewLine = "\r\n";

        private static string OnGeneratedCSProject(string path, string contents)
        {
            if (CodeEditor.CurrentEditor is ZedExternalCodeEditor == false)
                return contents;

            try
            {
                var assembly = FindAssembly(contents);
                var analyzers = GetAnalyzerPaths(assembly).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                if (analyzers.Length == 0)
                    return contents;

                return AddItems(contents, "Analyzer", analyzers);
            }
            catch (Exception exception)
            {
                ZedLogger.Create().LogWarning($"Could not add analyzers to '{path}': {exception.Message}");
                return contents;
            }
        }

        private static UnityEditor.Compilation.Assembly FindAssembly(string contents)
        {
            var assemblyName = GetElementValue(contents, "AssemblyName");
            if (string.IsNullOrEmpty(assemblyName))
                return null;

            const string playerSuffix = ".Player";
            var isPlayerAssembly = assemblyName.EndsWith(playerSuffix, StringComparison.Ordinal);
            var unityAssemblyName = isPlayerAssembly
                ? assemblyName.Substring(0, assemblyName.Length - playerSuffix.Length)
                : assemblyName;
            var assemblyType = isPlayerAssembly ? AssembliesType.Player : AssembliesType.Editor;

            return CompilationPipeline.GetAssemblies(assemblyType)
                .FirstOrDefault(assembly => assembly.name == unityAssemblyName);
        }

        private static IEnumerable<string> GetAnalyzerPaths(UnityEditor.Compilation.Assembly assembly)
        {
#if UNITY_2020_2_OR_NEWER
            if (assembly == null)
                return Array.Empty<string>();

            return (assembly.compilerOptions.RoslynAnalyzerDllPaths ?? Array.Empty<string>())
                .Where(analyzer => string.IsNullOrEmpty(analyzer) == false && File.Exists(analyzer))
                .Select(Path.GetFullPath);
#else
            return Array.Empty<string>();
#endif
        }

        private static string AddItems(string contents, string itemName, IEnumerable<string> paths)
        {
            var newPaths = paths
                .Where(path => contents.IndexOf($"Include=\"{SecurityElement.Escape(path)}\"", StringComparison.OrdinalIgnoreCase) < 0)
                .ToArray();
            if (newPaths.Length == 0)
                return contents;

            var projectEnd = contents.LastIndexOf("</Project>", StringComparison.Ordinal);
            if (projectEnd < 0)
                return contents;

            var itemGroup = new StringBuilder();
            itemGroup.Append("  <ItemGroup>").Append(kNewLine);
            foreach (var path in newPaths)
                itemGroup.Append("    <").Append(itemName).Append(" Include=\"").Append(SecurityElement.Escape(path)).Append("\" />").Append(kNewLine);
            itemGroup.Append("  </ItemGroup>").Append(kNewLine);

            return contents.Insert(projectEnd, itemGroup.ToString());
        }

        private static string GetElementValue(string contents, string elementName)
        {
            var opening = $"<{elementName}>";
            var start = contents.IndexOf(opening, StringComparison.Ordinal);
            if (start < 0)
                return null;

            start += opening.Length;
            var end = contents.IndexOf($"</{elementName}>", start, StringComparison.Ordinal);
            return end < 0 ? null : contents.Substring(start, end - start).Trim();
        }
    }
}
