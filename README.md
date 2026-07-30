# Zed for Unity [![openupm](https://img.shields.io/npm/v/com.maligan.unity-zed?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.maligan.unity-zed/)

First-class [Zed](https://zed.dev) integration for Unity. It connects Unity's
project-generation pipeline to Zed's C# language server and creates the workspace
configuration needed for a productive Unity C# experience.

## Features

- Discovers stable, preview, portable, PATH, Nix, and standard Zed installations on
  Windows, macOS, and Linux and registers them in Unity's
  **External Script Editor** preference.
- Opens the project, file, line, and column selected in Unity.
- Generates and incrementally refreshes SDK-style solutions and projects through
  Unity's official Visual Studio Editor package.
- Makes Unity assemblies, package sources, defines, references, and analyzers
  registered with Unity's compilation pipeline available to Zed's C# language server.
- Enables OmniSharp analyzer diagnostics, import completion, full-solution analysis,
  and `.editorconfig` formatting without replacing existing user preferences.
- Configures Zed's default Roslyn server for full-solution compiler/analyzer
  diagnostics, unimported-name completion, argument-list completion, decompiled
  navigation, and Source Link navigation; it also enables analyzers for `csharp-ls`.
- Works with Zed's marketplace **Unity Snippets** extension to provide Unity message
  completions such as `Awake`, `Update`, `OnDestroy`, physics callbacks, rendering
  callbacks, serialization callbacks, and state-machine callbacks.
- Associates `.asmdef`, `.asmref`, `.shader`, `.compute`, `.cginc`, `.hlsl`,
  `.glslinc`, `.raytrace`, `.uxml`, and `.uss` with their corresponding Zed
  languages for Unity-aware syntax coloration.
- Merges Unity file exclusions into an existing `.zed/settings.json` instead of
  overwriting the user's Zed configuration.

## Requirements

Install Zed's C# extension and a .NET SDK supported by that extension. Select this
package's Zed entry under **Unity > Preferences > External Tools > External Script
Editor**, then press **Regenerate project files** once. Opening a script from Unity
also synchronizes everything automatically.

The generated integration files are:

| File | Purpose |
| --- | --- |
| `.sln` and `.csproj` | C# completion, navigation, refactoring, package references, source generators, and project analyzers |
| `.zed/settings.json` | Fast project scanning that ignores Unity-generated and binary files |
| `omnisharp.json` | Analyzer, import-completion, solution-analysis, and EditorConfig support |

All JSON configuration is idempotent. Existing values win, unknown settings are
preserved, invalid JSON is left untouched, and unchanged files are not rewritten.
JSON-with-comments files are also left untouched so synchronization never
silently removes a user's comments. A read-only or malformed file cannot prevent Zed
from opening a script; Unity reports the affected integration file as a warning and
continues synchronizing the others.

## Installation

```sh
# 1. Via OpenUPM
openupm add com.maligan.unity-zed

# 2. Via Package Manager & GitHub URL
https://github.com/maligan/unity-zed.git

# 3. Via copy this repository content into Packages/ folder
```

## Required Zed extensions

Install the following from Zed's Extensions page:

- **C#** — Roslyn language-server completion, navigation, refactoring, formatting,
  and code actions.
- [**Unity Snippets**](https://github.com/Abdallah-Alwarawreh/unity-zed-snippets) — Unity API message and template completions. Zed only loads
  snippets from installed extensions or its user configuration directory; project
  `.zed/snippets` directories are not supported.
- [**HLSL**](https://github.com/igordreher/zed-hlsl) and **GLSL** — shader syntax highlighting for the file associations this
  package adds.
- [**Unity Debugger**](https://github.com/tomires/zed-unity-debugger) — Unity Editor/player debugging through a separately supplied,
  Unity-compatible Debug Adapter Protocol implementation. Follow that extension's
  setup instructions and use an adapter whose license permits use outside Microsoft
  IDEs.

## C# language-server notes

The generated Unity projects are the source of truth for semantic completion,
go-to-definition, references, rename, formatting, code actions, and diagnostics.
The default Zed C# server is Roslyn. `omnisharp.json` additionally enables analyzers
when the C# extension is explicitly configured to use OmniSharp. Other Roslyn-based
servers read analyzer and source-generator references from the generated projects.

This package does not redistribute analyzer binaries or download executable code.
Analyzers and source generators installed through Unity packages or assembly
definitions and exposed by Unity's compilation pipeline are forwarded to the generated
projects automatically.

This package deliberately does not download or execute a debugger binary. Microsoft's
VS Code Unity debug adapter is not licensed for use in Zed; use the Zed **Unity
Debugger** extension with a compatible independently licensed adapter instead.

## Unity CLI smoke test

The package includes a disposable-project smoke test that performs a real Unity
batch-mode import and domain reload, compiles the package against the installed Unity
Editor, and executes settings synchronization and discovery:

```sh
UNITY_EDITOR=/path/to/Unity Tests~/run-unity-smoke-test.sh
```

The script creates its project under the system temporary directory and always removes
it on exit. It never modifies the calling Unity project or adds it to Unity Hub's recent
project list. Use an already activated Unity Editor; the script intentionally never
accepts, stores, or forwards license credentials.
