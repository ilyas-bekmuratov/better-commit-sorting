import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.progress.ProgressIndicator
import com.intellij.openapi.progress.Task
import com.intellij.openapi.project.Project
import com.intellij.openapi.vcs.FilePath
import com.intellij.openapi.vcs.ProjectLevelVcsManager
import com.intellij.openapi.vcs.VcsException
import com.intellij.openapi.vcs.changes.Change
import com.intellij.openapi.vcs.changes.ChangeListManager
import com.intellij.openapi.vcs.rollback.RollbackProgressListener
import com.intellij.openapi.vfs.LocalFileSystem
import java.util.stream.Collectors

class SortChangelistAction : AnAction("Sort Changes") {

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        object : Task.Backgroundable(project, "Sorting changelists...") {
            override fun run(indicator: ProgressIndicator) {
                val changeListManager = ChangeListManager.getInstance(project)
                val state = ChangelistSorterState.getInstance(project)

                val allChanges = changeListManager.allChanges.toList()

                val changesToSort = if (state.ignoreEmptyFolderMetas) {
                    val (folderMetas, rest) = allChanges.partition { isFolderMetaChange(it) }
                    if (folderMetas.isNotEmpty()) unstageAddedFolderMetaChanges(project, folderMetas)
                    rest
                } else {
                    allChanges
                }

                // O(N) path→change index — replaces O(N²) firstOrNull scan per .meta
                val changeByPath = buildChangeByPath(changesToSort)

                // Parallel pre-read of all asset content — eliminates serial I/O during classify
                val contentCache = buildContentCache(changesToSort, state)

                val sortedChanges = mutableMapOf<String, MutableList<Change>>()
                val unassignedChanges = mutableListOf<Change>()
                val total = changesToSort.size

                changesToSort.forEachIndexed { index, change ->
                    indicator.fraction = if (total > 0) index.toDouble() / total else 0.0
                    val (category, ch) = classifyChange(change, state, changeByPath, contentCache)
                    if (category != null) {
                        sortedChanges.computeIfAbsent(category) { mutableListOf() }.add(ch)
                    } else {
                        unassignedChanges.add(ch)
                    }
                }
                indicator.fraction = 1.0

                // Single EDT commit — no N round-trips
                ApplicationManager.getApplication().invokeAndWait {
                    moveChangesToChangelists(sortedChanges, unassignedChanges, changeListManager)
                    if (state.removeUnusedChangelists) removeEmptyChangelists(changeListManager)
                }
            }
        }.queue()
    }

    internal fun buildChangeByPath(changes: List<Change>): Map<String, Change> =
        changes.mapNotNull { c ->
            val path = c.afterRevision?.file?.path ?: c.beforeRevision?.file?.path
            if (path != null) path to c else null
        }.toMap()

    internal fun buildContentCache(changes: List<Change>, state: ChangelistSorterState): Map<String, String?> =
        changes
            .filter { needsAssetRead(it, state) }
            .parallelStream()
            .map { c ->
                val path = c.afterRevision?.file?.path ?: c.beforeRevision?.file?.path
                Pair(path, readAssetContent(c))
            }
            .filter { it.first != null }
            .collect(Collectors.toList())
            .associate { it.first!! to it.second }

    internal fun needsAssetRead(change: Change, state: ChangelistSorterState): Boolean {
        if (!state.sortUnityAssets) return false
        val filePath = change.afterRevision?.file ?: change.beforeRevision?.file ?: return false
        val ext = filePath.name.substringAfterLast('.', "").lowercase()
        if (ext == "asset") return true
        if (ext == "meta") {
            val parentExt = filePath.name.removeSuffix(".meta").removeSuffix(".META")
                .substringAfterLast('.', "").lowercase()
            if (parentExt == "asset") return true
        }
        return false
    }

    private fun readAssetContent(change: Change): String? {
        val filePath = change.afterRevision?.file ?: change.beforeRevision?.file ?: return null
        val isDeleted = change.afterRevision == null
        val ext = filePath.name.substringAfterLast('.', "").lowercase()
        val isMeta = ext == "meta"

        return when {
            isMeta -> {
                // Read the parent .asset content on behalf of this .meta change
                val parentPath = filePath.path.removeSuffix(".meta")
                val parentVFile = LocalFileSystem.getInstance().findFileByPath(parentPath)
                if (parentVFile != null) {
                    try { parentVFile.inputStream.bufferedReader().readText() } catch (e: Exception) { null }
                } else {
                    null
                }
            }
            isDeleted -> try { change.beforeRevision?.content } catch (e: Exception) { null }
            else      -> try { filePath.virtualFile?.inputStream?.bufferedReader()?.readText() } catch (e: Exception) { null }
        }
    }

    internal fun resolveExtension(filePath: FilePath, groupMetaFiles: Boolean): String {
        val ext = filePath.name.substringAfterLast('.', "").lowercase()
        if (groupMetaFiles && ext == "meta") {
            val parentName = filePath.name.removeSuffix(".meta").removeSuffix(".META")
            val parentExt = parentName.substringAfterLast('.', "").lowercase()
            if (parentExt.isNotEmpty()) return parentExt
        }
        return ext
    }

    private fun classifyChange(
        change: Change,
        state: ChangelistSorterState,
        changeByPath: Map<String, Change>,
        contentCache: Map<String, String?>
    ): Pair<String?, Change> {
        val filePath = change.afterRevision?.file ?: change.beforeRevision?.file
            ?: return Pair(null, change)
        val isDeleted = change.afterRevision == null
        val rawExt = filePath.name.substringAfterLast('.', "").lowercase()
        val isMeta = state.groupMetaFiles && rawExt == "meta"
        val ext = resolveExtension(filePath, state.groupMetaFiles)
        val filename = filePath.name
        val filePathStr = filePath.path

        for (rule in state.sortingRules) {
            if (!rule.enabled) continue
            val matched = when (rule.matchType) {
                "EXTENSION" -> {
                    val exts = rule.pattern.split(",").map { it.trim().lowercase() }
                    if (exts.contains(ext)) {
                        if (ext == "asset" && state.sortUnityAssets) {
                            val assetCategory = resolveAssetCategory(change, filePath, isMeta, isDeleted, state, changeByPath, contentCache)
                            if (assetCategory != null) return Pair(assetCategory, change)
                        }
                        true
                    } else false
                }
                "EXACT"     -> filename == rule.pattern
                "REGEX"     -> try { Regex(rule.pattern).containsMatchIn(filename) } catch (e: Exception) { false }
                "DIRECTORY" -> filePathStr.startsWith(rule.pattern + "/") || filePathStr.startsWith(rule.pattern + "\\")
                else        -> false
            }
            if (matched) return Pair(rule.changelistName, change)
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
        changeByPath: Map<String, Change>,
        contentCache: Map<String, String?>
    ): String? {
        val content: String? = if (isMeta) {
            val parentPath = filePath.path.removeSuffix(".meta")
            // Cache for .meta path stores parent's content; fall back to parent's own cache entry
            // or beforeRevision for a deleted parent that has its own change
            contentCache[filePath.path]
                ?: contentCache[parentPath]
                ?: try { changeByPath[parentPath]?.beforeRevision?.content } catch (e: Exception) { null }
        } else {
            contentCache[filePath.path]
        }

        if (content == null) return null

        val (unityClass, soClass) = detectAssetClass(content) ?: return null

        if (unityClass != "MonoBehaviour") {
            return state.assetClassRules.firstOrNull { it.enabled && it.unityClass == unityClass }?.changelistName
        }

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
    internal fun detectAssetClass(content: String): Pair<String, String?>? {
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

    private fun isFolderMetaChange(change: Change): Boolean {
        val filePath = change.afterRevision?.file ?: change.beforeRevision?.file ?: return false
        if (!filePath.name.endsWith(".meta", ignoreCase = true)) return false

        val pathWithoutMeta = filePath.path.removeSuffix(".meta")
        val vFile = LocalFileSystem.getInstance().findFileByPath(pathWithoutMeta)
        if (vFile != null && vFile.isDirectory) {
            return vFile.children.isEmpty()
        }

        // Folder was deleted — check content of the before-revision for the Unity folder marker
        val content = try { change.beforeRevision?.content } catch (e: Exception) { null }
        return content?.contains("folderAsset: yes") == true
    }

    private fun unstageAddedFolderMetaChanges(project: Project, changes: List<Change>) {
        val addedOnly = changes.filter { it.beforeRevision == null }
        if (addedOnly.isEmpty()) return
        val vcsManager = ProjectLevelVcsManager.getInstance(project)
        val byVcs = addedOnly.groupBy { change ->
            val fp = change.afterRevision?.file ?: change.beforeRevision?.file
            fp?.let { vcsManager.getVcsFor(it) }
        }
        for ((vcs, vcsChanges) in byVcs) {
            vcs?.rollbackEnvironment?.rollbackChanges(vcsChanges, mutableListOf<VcsException>(), RollbackProgressListener.EMPTY)
        }
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
                ?: changeListManager.addChangeList(categoryName, null)
            changeListManager.moveChangesTo(targetList, *changes.toTypedArray())
        }

        if (unassigned.isNotEmpty()) {
            val otherListName = "Other Changes"
            val otherList = changeListManager.findChangeList(otherListName)
                ?: changeListManager.addChangeList(otherListName, null)
            changeListManager.moveChangesTo(otherList, *unassigned.toTypedArray())
        }
    }
}
