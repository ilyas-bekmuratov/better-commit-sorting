import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.vcs.changes.Change
import com.intellij.openapi.vcs.changes.ChangeListManager

class SortChangelistAction : AnAction("Sort Changes") {

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val changeListManager = ChangeListManager.getInstance(project)
        val allChanges = changeListManager.allChanges

        // FETCH THE SAVED SETTINGS HERE
        val state = ChangelistSorterState.getInstance(project)
        val settings = state.sortingRules
        val groupMetaFiles = state.groupMetaFiles

        val sortedChanges = mutableMapOf<String, MutableList<Change>>()
        val unassignedChanges = mutableListOf<Change>()

        for (change in allChanges) {
            val filePath = change.afterRevision?.file ?: change.beforeRevision?.file ?: continue
            var ext = filePath.name.substringAfterLast('.', "").lowercase()
            val isDeleted = change.afterRevision == null

            if (groupMetaFiles && ext == "meta") {
                val parentName = filePath.name.removeSuffix(".meta").removeSuffix(".META")
                val parentExt = parentName.substringAfterLast('.', "").lowercase()

                if (parentExt.isNotEmpty()) {
                    ext = parentExt
                }
            }

            var assignedCategory: String? = null

            for ((categoryName, extensionsString) in settings) {
                val extensionsList = extensionsString.split(",").map { it.trim().lowercase() }

                if (extensionsList.contains(ext)) {
                    if (ext == "asset" && !isDeleted) {
                        var isScriptableObject = false
                        val virtualFile = filePath.virtualFile
                        if (virtualFile != null) {
                            try {
                                virtualFile.inputStream.bufferedReader().useLines { lines ->
                                    isScriptableObject = lines.take(20).any { it.contains("MonoBehaviour:") }
                                }
                            } catch (ex: Exception) {
                                // Safely ignore files that are locked or unreadable
                            }
                        }

                        if (isScriptableObject) {
                            assignedCategory = "ScriptableObjects"
                            break
                        }
                    }

                    assignedCategory = categoryName
                    break
                }
            }

            if (assignedCategory != null) {
                sortedChanges.computeIfAbsent(assignedCategory) { mutableListOf() }.add(change)
            } else {
                unassignedChanges.add(change)
            }
        }

        for ((categoryName, changes) in sortedChanges) {
            var targetList = changeListManager.findChangeList(categoryName)
            if (targetList == null) {
                targetList = changeListManager.addChangeList(categoryName, "Auto-sorted")
            }
            changeListManager.moveChangesTo(targetList, *changes.toTypedArray())
        }

        if (unassignedChanges.isNotEmpty()) {
            val otherListName = "Other Changes"
            var otherList = changeListManager.findChangeList(otherListName)
            if (otherList == null) {
                otherList = changeListManager.addChangeList(otherListName, "Uncategorized files")
            }
            changeListManager.moveChangesTo(otherList, *unassignedChanges.toTypedArray())
        }
    }
}
