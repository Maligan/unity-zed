# Changelog

All notable changes to this package are documented in this file.

## [0.4.2-preview] - 2026-07-30

### Added

- Cross-platform discovery for stable, preview, portable, PATH, Nix, and custom Zed installations.
- SDK-style Unity project generation with support for analyzers registered by Unity.
- Project-local Roslyn, csharp-ls, OmniSharp, Unity file-type, and scan-exclusion settings.
- Documentation for the Zed Unity Snippets, shader-language, and Unity Debugger extensions.
- A disposable Unity CLI smoke test for real package import, compilation, and domain reload validation.

### Changed

- Project synchronization now refreshes package information and tolerates missing or null asset changes.
- Settings synchronization preserves existing values, malformed files, JSONC comments, and read-only projects.
- Warnings from persistent configuration problems are emitted once instead of on every asset import.

### Fixed

- Player projects now receive analyzers from the matching player compilation assembly.
- Missing or unsupported files are no longer passed to Zed.
- A failed compatibility cache reset no longer interrupts project synchronization.
