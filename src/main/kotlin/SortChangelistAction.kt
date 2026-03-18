import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.vcs.FilePath
import com.intellij.openapi.vcs.changes.Change
import com.intellij.openapi.vcs.changes.ChangeListManager
import com.intellij.openapi.vfs.LocalFileSystem
import com.intellij.openapi.vfs.VirtualFile

class SortChangelistAction : AnAction("Sort Changes") {

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val changeListManager = ChangeListManager.getInstance(project)
        val state = ChangelistSorterState.getInstance(project)

        val sortedChanges = mutableMapOf<String, MutableList<Change>>()
        val unassignedChanges = mutableListOf<Change>()

        val allChanges = changeListManager.allChanges
        for (change in allChanges) {
            val (category, ch) = classifyChange(change, state, allChanges)
            if (category != null) {
                sortedChanges.computeIfAbsent(category) { mutableListOf() }.add(ch)
            } else {
                unassignedChanges.add(ch)
            }
        }

        moveChangesToChangelists(sortedChanges, unassignedChanges, changeListManager)

        if (state.removeUnusedChangelists) {
            removeEmptyChangelists(changeListManager)
        }
    }

    private fun resolveExtension(filePath: FilePath, groupMetaFiles: Boolean): String {
        val ext = filePath.name.substringAfterLast('.', "").lowercase()
        if (groupMetaFiles && ext == "meta") {
            val parentName = filePath.name.removeSuffix(".meta").removeSuffix(".META")
            val parentExt = parentName.substringAfterLast('.', "").lowercase()
            if (parentExt.isNotEmpty()) return parentExt
        }
        return ext
    }

    private fun classifyChange(change: Change, state: ChangelistSorterState, allChanges: Collection<Change>): Pair<String?, Change> {
        val filePath = change.afterRevision?.file ?: change.beforeRevision?.file
            ?: return Pair(null, change)
        val isDeleted = change.afterRevision == null
        val rawExt = filePath.name.substringAfterLast('.', "").lowercase()
        val isMeta = state.groupMetaFiles && rawExt == "meta"
        val ext = resolveExtension(filePath, state.groupMetaFiles)

        for ((categoryName, extensionsString) in state.sortingRules) {
            val extensionsList = extensionsString.split(",").map { it.trim().lowercase() }
            if (!extensionsList.contains(ext)) continue

            if (ext == "asset") {
                val assetCategory = resolveAssetCategory(change, filePath, isMeta, isDeleted, state, allChanges)
                if (assetCategory != null) return Pair(assetCategory, change)
            }

            return Pair(categoryName, change)
        }

        // Filename pattern rules (checked after extension rules fail)
        val filename = filePath.name
        for (rule in state.filenamePatternRules) {
            val matched = when (rule.matchMode) {
                "EXTENSION" -> ext == rule.pattern.lowercase()
                "EXACT" -> filename == rule.pattern
                "REGEX" -> try { Regex(rule.pattern).containsMatchIn(filename) } catch (e: Exception) { false }
                else -> false
            }
            if (matched) return Pair(rule.changelistName, change)
        }

        // Directory rules (checked after pattern rules, before fallback)
        val filePathStr = filePath.path
        for (rule in state.directoryRules) {
            if (filePathStr.startsWith(rule.path + "/") || filePathStr.startsWith(rule.path + "\\")) {
                return Pair(rule.changelistName, change)
            }
        }

        return Pair(null, change)
    }

    /**
     * Determines the category for an `.asset` file (or its `.meta` proxy) by inspecting content.
     * Returns null if the file should fall through to the default extension-based category.
     */
    private fun resolveAssetCategory(
        change: Change,
        filePath: FilePath,
        isMeta: Boolean,
        isDeleted: Boolean,
        state: ChangelistSorterState,
        allChanges: Collection<Change>
    ): String? {
        val content: String? = when {
            isMeta -> {
                // Read the parent .asset file instead of the .meta file itself
                val parentPath = filePath.path.removeSuffix(".meta")
                val parentVFile = LocalFileSystem.getInstance().findFileByPath(parentPath)
                if (parentVFile != null) {
                    try { parentVFile.inputStream.bufferedReader().readText() } catch (e: Exception) { null }
                } else {
                    // Parent also deleted — look it up in allChanges by path
                    val parentChange = allChanges.firstOrNull { c ->
                        (c.afterRevision?.file?.path ?: c.beforeRevision?.file?.path) == parentPath
                    }
                    try { parentChange?.beforeRevision?.content } catch (e: Exception) { null }
                }
            }
            isDeleted -> {
                // Read content from before revision for deleted files
                try { change.beforeRevision?.content } catch (e: Exception) { null }
            }
            else -> {
                try { filePath.virtualFile?.inputStream?.bufferedReader()?.readText() } catch (e: Exception) { null }
            }
        }
        if (content == null) return null

        val (unityClass, soClass) = detectAssetClass(content) ?: return null

        if (unityClass != "MonoBehaviour") {
            return state.assetClassRules[unityClass]
        }

        // MonoBehaviour = ScriptableObject
        if (soClass != null) {
            return if (state.sortScriptableObjectsByClass) "SO: $soClass" else "ScriptableObjects"
        }
        return null
    }

    /**
     * Parses the Unity YAML class identifier from asset content.
     * Returns (unityClass, soClass) where soClass is non-null only for MonoBehaviour assets
     * that have a valid m_EditorClassIdentifier.
     */
    private fun detectAssetClass(content: String): Pair<String, String?>? {
        val lines = content.lines().take(30).map { it.trim() }
        val classLineRegex = Regex("^([A-Za-z][A-Za-z0-9_]+):\\s*$")

        val unityClass = lines
            .firstOrNull { classLineRegex.matches(it) }
            ?.let { classLineRegex.find(it)?.groupValues?.get(1) }
            ?: return null

        if (unityClass == "MonoBehaviour") {
            val soClass = lines
                .firstOrNull { it.startsWith("m_EditorClassIdentifier:") }
                ?.substringAfter("m_EditorClassIdentifier:")
                ?.trim()
                ?.takeIf { it.isNotEmpty() }
                ?.let { identifier ->
                    val parts = identifier.split("::")
                    if (parts.size == 2) {
                        val assembly = parts[0]
                        val className = parts[1].substringAfterLast('.')
                        "$assembly::$className"
                    } else {
                        identifier.substringAfterLast('.')
                    }
                }
            return Pair(unityClass, soClass)
        }

        return Pair(unityClass, null)
    }

    private fun removeEmptyChangelists(changeListManager: ChangeListManager) {
        val toRemove = changeListManager.changeLists.filter {
            !it.isDefault && it.changes.isEmpty()
        }
        for (changelist in toRemove) {
            changeListManager.removeChangeList(changelist)
        }
    }

    private fun moveChangesToChangelists(
        sortedChanges: Map<String, List<Change>>,
        unassigned: List<Change>,
        changeListManager: ChangeListManager
    ) {
        for ((categoryName, changes) in sortedChanges) {
            val targetList = changeListManager.findChangeList(categoryName)
                ?: changeListManager.addChangeList(categoryName, "Auto-sorted")
            changeListManager.moveChangesTo(targetList, *changes.toTypedArray())
        }

        if (unassigned.isNotEmpty()) {
            val otherListName = "Other Changes"
            val otherList = changeListManager.findChangeList(otherListName)
                ?: changeListManager.addChangeList(otherListName, "Uncategorized files")
            changeListManager.moveChangesTo(otherList, *unassigned.toTypedArray())
        }
    }
}
