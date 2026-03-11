# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Better-Changelist** is a JetBrains IDE plugin (IntelliJ Platform) that auto-sorts Git changes into organized changelists by file extension. Built primarily for Unity developers dealing with large commits of mixed code, assets, and meta files.

Key behavior: Triggered via `Alt+S`, it categorizes VCS changes by extension rules, groups `.meta` files with their parents, and detects ScriptableObjects in `.asset` files by scanning for `MonoBehaviour:`.

## Build Commands

```bash
./gradlew build              # Full build (compile + test + checks)
./gradlew test               # Run tests only
./gradlew runIde             # Launch sandbox IDE with plugin loaded
./gradlew buildPlugin        # Build distributable plugin ZIP
./gradlew verifyPlugin       # Verify plugin compatibility
```

Gradle wrapper uses version 9.3.1. JVM toolchain targets JDK 8 (runtime) and JDK 21 (build). Kotlin 2.3.x.

## Architecture

Three source files implement the entire plugin:

- **`SortChangelistAction`** — The `AnAction` entry point. Reads all VCS changes, applies sorting rules from state, handles `.meta` grouping and ScriptableObject detection, then moves changes into named changelists via `ChangeListManager`.
- **`ChangelistSorterState`** — Project-level `PersistentStateComponent` storing sorting rules (category→extensions map) and the `groupMetaFiles` toggle. Persisted to `ChangelistSorterSettings.xml`.
- **`SorterConfigurable`** — Settings UI under Tools. Editable table of rules + meta-grouping checkbox, wired to the state component.

All source files are in the default package under `src/main/kotlin/`. No package prefix is used for production code.

## Plugin Registration

`plugin.xml` registers:
- Action `SortUnityChangesAction` with shortcut `Alt+S` in `VcsGroup`
- Configurable `ChangelistConfigurable` under Tools settings

Dependencies: `com.intellij.modules.platform`, `com.intellij.modules.vcs`, bundled module `intellij.platform.vcs.impl`.

## Test Data

`src/test/testData/` contains Unity project files (`.cs`, `.asset`, `.asmdef`, `.meta`) used as test fixtures. The test class references a `MyProjectService` that appears to be leftover from the template and may need updating.

## Configuration

- Plugin metadata in `gradle.properties` (version, platform compatibility, bundled plugins)
- Dependency versions in `gradle/libs.versions.toml`
- IntelliJ Platform Gradle Plugin 2.x configuration in `build.gradle.kts`
