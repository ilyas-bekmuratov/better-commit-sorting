# better-commit-sorting

![Build](https://github.com/ilyas-bekmuratov/better-commit-sorting/workflows/Build/badge.svg)
[![Version](https://img.shields.io/jetbrains/plugin/v/MARKETPLACE_ID.svg)](https://plugins.jetbrains.com/plugin/MARKETPLACE_ID)
[![Downloads](https://img.shields.io/jetbrains/plugin/d/MARKETPLACE_ID.svg)](https://plugins.jetbrains.com/plugin/MARKETPLACE_ID) 


## Template ToDo list
- [ ] Set the `MARKETPLACE_ID` in the above README badges. You can obtain it once the plugin is published to JetBrains Marketplace.
- [ ] Set the [Plugin Signing](https://plugins.jetbrains.com/docs/intellij/plugin-signing.html?from=IJPluginTemplate) related [secrets](https://github.com/JetBrains/intellij-platform-plugin-template#environment-variables).
- [ ] Set the [Deployment Token](https://plugins.jetbrains.com/docs/marketplace/plugin-upload.html?from=IJPluginTemplate).
- [ ] Click the <kbd>Watch</kbd> button on the top of the [IntelliJ Platform Plugin Template][template] to be notified about releases containing new features and fixes.
- [ ] Configure the [CODECOV_TOKEN](https://docs.codecov.com/docs/quick-start) secret for automated test coverage reports on PRs

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

  Go to [JetBrains Marketplace](https://plugins.jetbrains.com/plugin/MARKETPLACE_ID) and install it by clicking the <kbd>Install to ...</kbd> button in case your IDE is running.

  You can also download the [latest release](https://plugins.jetbrains.com/plugin/MARKETPLACE_ID/versions) from JetBrains Marketplace and install it manually using
  <kbd>Settings/Preferences</kbd> > <kbd>Plugins</kbd> > <kbd>⚙️</kbd> > <kbd>Install plugin from disk...</kbd>

- Manually:

  Download the [latest release](https://github.com/ilyas-bekmuratov/better-commit-sorting/releases/latest) and install it manually using
  <kbd>Settings/Preferences</kbd> > <kbd>Plugins</kbd> > <kbd>⚙️</kbd> > <kbd>Install plugin from disk...</kbd>


---
Plugin based on the [IntelliJ Platform Plugin Template][template].

[template]: https://github.com/JetBrains/intellij-platform-plugin-template
[docs:plugin-description]: https://plugins.jetbrains.com/docs/intellij/plugin-user-experience.html#plugin-description-and-presentation
