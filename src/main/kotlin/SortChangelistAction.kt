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
            var matched = false

            // Check against our dynamic settings map
            for ((categoryName, extensionsString) in settings) {
                // Split the comma-separated string into a list and trim spaces
                val extensionsList = extensionsString.split(",").map { it.trim().lowercase() }
                if (!extensionsList.contains(ext)) {
                    unassignedChanges.add(change)
                    continue
                }

                if (groupMetaFiles && ext == "meta") {
                    // Remove the .meta suffix (e.g. Player.prefab.meta -> Player.prefab)
                    val parentName = filePath.name.removeSuffix(".meta").removeSuffix(".META")
                    // Get the parent's extension (e.g. prefab)
                    val parentExt = parentName.substringAfterLast('.', "").lowercase()

                    // If it successfully found a parent extension (meaning it wasn't just a folder named "folder.meta")
                    if (parentExt.isNotEmpty()) {
                        ext = parentExt
                    }
                }

                if (ext == "asset" && !isDeleted) {
                    val virtualFile = filePath.virtualFile
                    if (virtualFile != null) {
                        try {
                            virtualFile.inputStream.bufferedReader().useLines { lines ->
                                val isScriptableObject = lines.take(20).any { it.contains("MonoBehaviour:") }
                                if (isScriptableObject) {
                                    sortedChanges.computeIfAbsent("ScriptableObjects") { mutableListOf() }.add(change)
                                    matched = true
                                }
                            }
                        } catch (ex: Exception) {
                            // Safely ignore files that are locked or unreadable
                        }
                    }
                }

                if (!matched) {
                    sortedChanges.computeIfAbsent(categoryName) { mutableListOf() }.add(change)
                }
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
