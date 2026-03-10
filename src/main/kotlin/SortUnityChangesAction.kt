import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.vcs.changes.Change
import com.intellij.openapi.vcs.changes.ChangeListManager

class SortUnityChangesAction : AnAction("Sort Unity Changes") {

    override fun actionPerformed(e: AnActionEvent) {
        // 1. Get the current project and the VCS ChangeListManager
        val project = e.project ?: return
        val changeListManager = ChangeListManager.getInstance(project)

        // 2. Grab every single file that currently has uncommitted changes
        val allChanges = changeListManager.allChanges

        // 3. Define our Unity categories and their target extensions
        val categories = mapOf(
            "Unity Scenes" to listOf("unity"),
            "Unity Prefabs" to listOf("prefab"),
            "Unity Materials" to listOf("mat"),
            "Unity ScriptableObjects" to listOf("asset"),
            "Unity Meta Files" to listOf("meta")
        )

        // Map to hold the changes as we sort them
        val sortedChanges = mutableMapOf<String, MutableList<Change>>()

        // 4. Sort the changes based on file extension
        for (change in allChanges) {
            // Get the file (handling both modifications and deletions)
            val file = change.virtualFile ?: change.beforeRevision?.file?.virtualFile ?: continue
            val ext = file.extension?.lowercase() ?: continue

            for ((categoryName, extensions) in categories) {
                if (extensions.contains(ext)) {
                    sortedChanges.computeIfAbsent(categoryName) { mutableListOf() }.add(change)
                    break
                }
            }
        }

        // 5. Create the changelists (if they don't exist) and move the files
        for ((categoryName, changes) in sortedChanges) {
            var targetList = changeListManager.findChangeList(categoryName)
            if (targetList == null) {
                targetList = changeListManager.addChangeList(categoryName, "Auto-sorted Unity files")
            }

            // Move the grouped changes into their new home
            changeListManager.moveChangesTo(targetList, *changes.toTypedArray())
        }
    }
}