import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.vcs.changes.Change
import com.intellij.openapi.vcs.changes.ChangeListManager

class SortChangelistAction : AnAction("Sort Changes") {

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val changeListManager = ChangeListManager.getInstance(project)
        val state = ChangelistSorterState.getInstance(project)

        val sortedChanges = mutableMapOf<String, MutableList<Change>>()
        val unassignedChanges = mutableListOf<Change>()

        for (change in changeListManager.allChanges) {
            val (category, ch) = classifyChange(change, state)
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

    private fun resolveExtension(filePath: com.intellij.openapi.vcs.FilePath, groupMetaFiles: Boolean): String {
        val ext = filePath.name.substringAfterLast('.', "").lowercase()
        if (groupMetaFiles && ext == "meta") {
            val parentName = filePath.name.removeSuffix(".meta").removeSuffix(".META")
            val parentExt = parentName.substringAfterLast('.', "").lowercase()
            if (parentExt.isNotEmpty()) return parentExt
        }
        return ext
    }

    private fun classifyChange(change: Change, state: ChangelistSorterState): Pair<String?, Change> {
        val filePath = change.afterRevision?.file ?: change.beforeRevision?.file
            ?: return Pair(null, change)
        val isDeleted = change.afterRevision == null
        val ext = resolveExtension(filePath, state.groupMetaFiles)

        for ((categoryName, extensionsString) in state.sortingRules) {
            val extensionsList = extensionsString.split(",").map { it.trim().lowercase() }
            if (!extensionsList.contains(ext)) continue

            if (ext == "asset" && !isDeleted) {
                val soClass = detectScriptableObjectClass(filePath.virtualFile)
                if (soClass != null) {
                    val category = if (state.sortScriptableObjectsByClass) "SO: $soClass" else "ScriptableObjects"
                    return Pair(category, change)
                }
            }

            return Pair(categoryName, change)
        }

        return Pair(null, change)
    }

    private fun detectScriptableObjectClass(virtualFile: VirtualFile?): String? {
        if (virtualFile == null) return null
        return try {
            virtualFile.inputStream.bufferedReader().useLines { lines ->
                lines.take(20)
                    .map { it.trim() }
                    .firstOrNull { it.startsWith("m_EditorClassIdentifier:") }
                    ?.substringAfter("m_EditorClassIdentifier:")
                    ?.trim()
                    ?.takeIf { it.isNotEmpty() }
                    ?.let { identifier ->
                        // "SOAP::_Car_Parking.Scripts.SOAP.BoolEvent" → "SOAP::BoolEvent"
                        val parts = identifier.split("::")
                        if (parts.size == 2) {
                            val assembly = parts[0]
                            val className = parts[1].substringAfterLast('.')
                            "$assembly::$className"
                        } else {
                            identifier.substringAfterLast('.')
                        }
                    }
            }
        } catch (ex: Exception) {
            null
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
