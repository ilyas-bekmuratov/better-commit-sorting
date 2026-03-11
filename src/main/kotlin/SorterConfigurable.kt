import com.intellij.openapi.options.Configurable
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.Messages
import com.intellij.ui.ToolbarDecorator
import com.intellij.ui.table.JBTable
import java.awt.BorderLayout
import javax.swing.JCheckBox
import javax.swing.JComponent
import javax.swing.JPanel
import javax.swing.table.DefaultTableModel

class SorterConfigurable(private val project: Project) : Configurable {

    private var myPanel: JPanel? = null
    private lateinit var tableModel: DefaultTableModel
    private lateinit var rulesTable: JBTable
    private lateinit var groupMetaFilesCheckBox: JCheckBox
    private lateinit var sortSOByClassCheckBox: JCheckBox

    override fun getDisplayName(): String = "Changelist Sorter"

    override fun createComponent(): JComponent? {
        // Define columns: Changelist Name, Criteria (Extensions)
        tableModel = DefaultTableModel(arrayOf("Changelist Name", "Extensions (comma separated)"), 0)
        rulesTable = JBTable(tableModel)

        // The ToolbarDecorator automatically gives us the +, -, and Edit buttons
        val decorator = ToolbarDecorator.createDecorator(rulesTable)
            .setAddAction {
                // Basic validation for adding new entries
                val name = Messages.showInputDialog("Enter changelist name:", "Add Rule", null) ?: return@setAddAction
                val ext = Messages.showInputDialog("Enter Extensions (e.g. mat, prefab):", "Add Rule", null) ?: return@setAddAction

                if (isDuplicate(name, ext)) {
                    Messages.showErrorDialog("A rule with this name or extension already exists.", "Duplicate Rule")
                } else {
                    tableModel.addRow(arrayOf(name, ext))
                }
            }

        val tablePanel = decorator.createPanel()
        groupMetaFilesCheckBox = JCheckBox("Group changes with meta files")
        sortSOByClassCheckBox = JCheckBox("Sort ScriptableObjects by class type")

        val checkboxPanel = JPanel()
        checkboxPanel.layout = javax.swing.BoxLayout(checkboxPanel, javax.swing.BoxLayout.Y_AXIS)
        checkboxPanel.add(groupMetaFilesCheckBox)
        checkboxPanel.add(sortSOByClassCheckBox)

        val wrapperPanel = JPanel(BorderLayout())
        wrapperPanel.add(checkboxPanel, BorderLayout.NORTH)
        wrapperPanel.add(tablePanel, BorderLayout.CENTER)

        myPanel = wrapperPanel
        return myPanel
    }

    private fun isDuplicate(name: String, extensions: String): Boolean {
        for (i in 0 until tableModel.rowCount) {
            val existingName = tableModel.getValueAt(i, 0) as String
            val existingExts = tableModel.getValueAt(i, 1) as String
            if (existingName.equals(name, ignoreCase = true) || existingExts.equals(extensions, ignoreCase = true)) {
                return true
            }
        }
        return false
    }

    override fun isModified(): Boolean {
        // Compare UI state to Saved state to enable/disable the "Apply" button
        val state = ChangelistSorterState.getInstance(project)
        if (state.groupMetaFiles != groupMetaFilesCheckBox.isSelected) return true
        if (state.sortScriptableObjectsByClass != sortSOByClassCheckBox.isSelected) return true
        if (state.sortingRules.size != tableModel.rowCount) return true
        return true
    }

    override fun apply() {
        val state = ChangelistSorterState.getInstance(project)
        state.groupMetaFiles = groupMetaFilesCheckBox.isSelected
        state.sortScriptableObjectsByClass = sortSOByClassCheckBox.isSelected
        state.sortingRules.clear()
        for (i in 0 until tableModel.rowCount) {
            val name = tableModel.getValueAt(i, 0) as String
            val extensions = tableModel.getValueAt(i, 1) as String
            state.sortingRules[name] = extensions
        }
    }

    override fun reset() {
        // Load settings from state into the UI
        val state = ChangelistSorterState.getInstance(project)
        groupMetaFilesCheckBox.isSelected = state.groupMetaFiles
        sortSOByClassCheckBox.isSelected = state.sortScriptableObjectsByClass
        tableModel.rowCount = 0
        for ((name, extensions) in state.sortingRules) {
            tableModel.addRow(arrayOf(name, extensions))
        }
    }
}