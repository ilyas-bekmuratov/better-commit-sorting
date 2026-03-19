# Better-Changelist

![Build](https://github.com/ilyas-bekmuratov/better-commit-sorting/workflows/Build/badge.svg)
[![Version](https://img.shields.io/jetbrains/plugin/v/30597.svg)](https://plugins.jetbrains.com/plugin/30597)
[![Downloads](https://img.shields.io/jetbrains/plugin/d/30597.svg)](https://plugins.jetbrains.com/plugin/30597)

<!-- Plugin description -->
**Better-Changelist** is a time-saving version control plugin designed to automatically sort your messy Git changes into neatly organized, logical changelists. It is especially powerful for Unity developers who struggle with massive commits containing a mix of code, assets, and meta files.

**Key Features:**
* **Intelligent Auto-Sorting:** Automatically categorizes your modified files into designated changelists based on file extensions (e.g., Materials, Scripts, Prefabs, Scenes, Assembly).
* **Smart Meta File Grouping:** Keeps your `.meta` files tethered to their parent assets. If you modify `Player.prefab` and `Player.prefab.meta`, they are grouped together automatically.
* **ScriptableObject Detection:** Deep-scans `.asset` files to detect if they are ScriptableObjects (by checking for `MonoBehaviour:`) and isolates them into their own changelist for safer reviews.
* **Fully Customizable:** Don't like the default categories? Use the built-in settings panel to add, remove, or modify sorting rules and extensions to fit your exact workflow.
* **Catch-All:** Any files that don't match your rules are safely placed in an "Other Changes" changelist, ensuring nothing slips through the cracks.

**How to use:**
Simply open your Commit tool window and press <kbd>Alt</kbd> + <kbd>S</kbd> (or find "Sort Changelists" in the VCS actions menu) to instantly organize your working tree.

<!-- Plugin description end -->

## Installation

- Using the IDE built-in plugin system:

  <kbd>Settings/Preferences</kbd> > <kbd>Plugins</kbd> > <kbd>Marketplace</kbd> > <kbd>Search for "better-commit-sorting"</kbd> >
  <kbd>Install</kbd>

- Using JetBrains Marketplace:

  Go to [JetBrains Marketplace](https://plugins.jetbrains.com/plugin/30597) and install it by clicking the <kbd>Install to ...</kbd> button in case your IDE is running.

  You can also download the [latest release](https://plugins.jetbrains.com/plugin/30597/versions) from JetBrains Marketplace and install it manually using
  <kbd>Settings/Preferences</kbd> > <kbd>Plugins</kbd> > <kbd>⚙️</kbd> > <kbd>Install plugin from disk...</kbd>

- Manually:

  Download the [latest release](https://github.com/ilyas-bekmuratov/better-commit-sorting/releases/latest) and install it manually using
  <kbd>Settings/Preferences</kbd> > <kbd>Plugins</kbd> > <kbd>⚙️</kbd> > <kbd>Install plugin from disk...</kbd>


---
Plugin based on the [IntelliJ Platform Plugin Template][template].

[template]: https://github.com/JetBrains/intellij-platform-plugin-template
