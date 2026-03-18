import com.intellij.openapi.fileChooser.FileChooser
import com.intellij.openapi.fileChooser.FileChooserDescriptorFactory
import com.intellij.openapi.options.Configurable
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.Messages
import com.intellij.ui.ToolbarDecorator
import com.intellij.ui.table.JBTable
import java.awt.BorderLayout
import java.awt.Dimension
import javax.swing.BorderFactory
import javax.swing.BoxLayout
import javax.swing.JCheckBox
import javax.swing.JComboBox
import javax.swing.JComponent
import javax.swing.JLabel
import javax.swing.JPanel
import javax.swing.JScrollPane
import javax.swing.table.DefaultTableCellRenderer
import javax.swing.table.DefaultTableModel

class SorterConfigurable(private val project: Project) : Configurable {

    private var myPanel: JPanel? = null
    private lateinit var extensionTableModel: DefaultTableModel
    private lateinit var extensionTable: JBTable
    private lateinit var patternTableModel: DefaultTableModel
    private lateinit var patternTable: JBTable
    private lateinit var assetClassTableModel: DefaultTableModel
    private lateinit var assetClassTable: JBTable
    private lateinit var directoryTableModel: DefaultTableModel
    private lateinit var directoryTable: JBTable
    private lateinit var groupMetaFilesCheckBox: JCheckBox
    private lateinit var sortSOByClassCheckBox: JCheckBox
    private lateinit var removeUnusedChangelistsCheckBox: JCheckBox

    override fun getDisplayName(): String = "Changelist Sorter"

    override fun createComponent(): JComponent? {
        // Extension rules table
        extensionTableModel = DefaultTableModel(arrayOf("Changelist Name", "Extensions (comma separated)"), 0)
        extensionTable = JBTable(extensionTableModel)
        val extensionDecorator = ToolbarDecorator.createDecorator(extensionTable)
            .setAddAction {
                val name = Messages.showInputDialog("Enter changelist name:", "Add Extension Rule", null)
                    ?: return@setAddAction
                val ext = Messages.showInputDialog("Enter extensions (e.g. mat, prefab):", "Add Extension Rule", null)
                    ?: return@setAddAction
                if (isExtensionDuplicate(name, ext)) {
                    Messages.showErrorDialog("A rule with this name or extension already exists.", "Duplicate Rule")
                } else {
                    extensionTableModel.addRow(arrayOf(name, ext))
                }
            }
            .setRemoveAction {
                val selected = extensionTable.selectedRow
                if (selected >= 0) extensionTableModel.removeRow(selected)
            }

        // Filename pattern rules table
        patternTableModel = DefaultTableModel(arrayOf("Changelist Name", "Pattern", "Type"), 0)
        patternTable = JBTable(patternTableModel)
        val matchModeCombo = JComboBox(arrayOf("REGEX", "EXACT", "EXTENSION"))
        patternTable.columnModel.getColumn(2).cellEditor = javax.swing.DefaultCellEditor(matchModeCombo)
        patternTable.columnModel.getColumn(2).cellRenderer = DefaultTableCellRenderer()
        val patternDecorator = ToolbarDecorator.createDecorator(patternTable)
            .setAddAction {
                val name = Messages.showInputDialog("Enter changelist name:", "Add Pattern Rule", null)
                    ?: return@setAddAction
                val pattern = Messages.showInputDialog("Enter pattern:", "Add Pattern Rule", null)
                    ?: return@setAddAction
                val types = arrayOf("REGEX", "EXACT", "EXTENSION")
                val typeIndex = Messages.showChooseDialog("Select match type:", "Add Pattern Rule", types, "REGEX", null)
                if (typeIndex >= 0) {
                    patternTableModel.addRow(arrayOf(name, pattern, types[typeIndex]))
                }
            }
            .setRemoveAction {
                val selected = patternTable.selectedRow
                if (selected >= 0) patternTableModel.removeRow(selected)
            }

        // Asset class rules table
        assetClassTableModel = DefaultTableModel(arrayOf("Unity Class Name", "Changelist Name"), 0)
        assetClassTable = JBTable(assetClassTableModel)
        val assetClassDecorator = ToolbarDecorator.createDecorator(assetClassTable)
            .setAddAction {
                val unityClass = Messages.showInputDialog(
                    "Enter Unity class name (e.g. Material, Texture2D):", "Add Asset Class Rule", null
                ) ?: return@setAddAction
                val name = Messages.showInputDialog("Enter changelist name:", "Add Asset Class Rule", null)
                    ?: return@setAddAction
                assetClassTableModel.addRow(arrayOf(unityClass, name))
            }
            .setRemoveAction {
                val selected = assetClassTable.selectedRow
                if (selected >= 0) assetClassTableModel.removeRow(selected)
            }

        // Directory rules table
        directoryTableModel = DefaultTableModel(arrayOf("Changelist Name", "Directory Path"), 0)
        directoryTable = JBTable(directoryTableModel)
        val directoryDecorator = ToolbarDecorator.createDecorator(directoryTable)
            .setAddAction {
                val descriptor = FileChooserDescriptorFactory.createSingleFolderDescriptor()
                val chosen = FileChooser.chooseFile(descriptor, project, null) ?: return@setAddAction
                val name = Messages.showInputDialog("Enter changelist name:", "Add Directory Rule", null)
                    ?: return@setAddAction
                directoryTableModel.addRow(arrayOf(name, chosen.path))
            }
            .setRemoveAction {
                val selected = directoryTable.selectedRow
                if (selected >= 0) directoryTableModel.removeRow(selected)
            }

        // Checkboxes
        groupMetaFilesCheckBox = JCheckBox("Group changes with meta files")
        sortSOByClassCheckBox = JCheckBox("Sort ScriptableObjects by class type")
        removeUnusedChangelistsCheckBox = JCheckBox("Remove empty changelists after sorting")

        val checkboxPanel = JPanel()
        checkboxPanel.layout = BoxLayout(checkboxPanel, BoxLayout.Y_AXIS)
        checkboxPanel.add(groupMetaFilesCheckBox)
        checkboxPanel.add(sortSOByClassCheckBox)
        checkboxPanel.add(removeUnusedChangelistsCheckBox)

        // Stack the three table sections vertically
        val tablesPanel = JPanel()
        tablesPanel.layout = BoxLayout(tablesPanel, BoxLayout.Y_AXIS)

        fun addSection(labelText: String, tablePanel: JPanel, height: Int = 160) {
            val section = JPanel(BorderLayout())
            val label = JLabel(labelText)
            label.border = BorderFactory.createEmptyBorder(8, 0, 2, 0)
            section.add(label, BorderLayout.NORTH)
            section.add(tablePanel, BorderLayout.CENTER)
            section.maximumSize = Dimension(Int.MAX_VALUE, height + 30)
            section.preferredSize = Dimension(600, height + 30)
            tablesPanel.add(section)
        }

        addSection("Extension Rules:", extensionDecorator.createPanel())
        addSection("Filename Pattern Rules:", patternDecorator.createPanel())
        addSection("Asset Class Rules:", assetClassDecorator.createPanel())
        addSection("Directory Rules (sort by folder):", directoryDecorator.createPanel())

        val scrollPane = JScrollPane(tablesPanel)
        scrollPane.border = null

        val wrapperPanel = JPanel(BorderLayout())
        wrapperPanel.add(checkboxPanel, BorderLayout.NORTH)
        wrapperPanel.add(scrollPane, BorderLayout.CENTER)

        myPanel = wrapperPanel
        return myPanel
    }

    private fun isExtensionDuplicate(name: String, extensions: String): Boolean {
        for (i in 0 until extensionTableModel.rowCount) {
            val existingName = extensionTableModel.getValueAt(i, 0) as String
            val existingExts = extensionTableModel.getValueAt(i, 1) as String
            if (existingName.equals(name, ignoreCase = true) || existingExts.equals(extensions, ignoreCase = true)) {
                return true
            }
        }
        return false
    }

    override fun isModified(): Boolean {
        val state = ChangelistSorterState.getInstance(project)
        if (state.groupMetaFiles != groupMetaFilesCheckBox.isSelected) return true
        if (state.sortScriptableObjectsByClass != sortSOByClassCheckBox.isSelected) return true
        if (state.removeUnusedChangelists != removeUnusedChangelistsCheckBox.isSelected) return true

        // Extension rules
        if (state.sortingRules.size != extensionTableModel.rowCount) return true
        for (i in 0 until extensionTableModel.rowCount) {
            val name = extensionTableModel.getValueAt(i, 0) as String
            val exts = extensionTableModel.getValueAt(i, 1) as String
            if (state.sortingRules[name] != exts) return true
        }

        // Filename pattern rules
        if (state.filenamePatternRules.size != patternTableModel.rowCount) return true
        for (i in 0 until patternTableModel.rowCount) {
            val name = patternTableModel.getValueAt(i, 0) as String
            val pattern = patternTableModel.getValueAt(i, 1) as String
            val mode = patternTableModel.getValueAt(i, 2) as String
            val rule = state.filenamePatternRules.getOrNull(i) ?: return true
            if (rule.changelistName != name || rule.pattern != pattern || rule.matchMode != mode) return true
        }

        // Asset class rules
        if (state.assetClassRules.size != assetClassTableModel.rowCount) return true
        for (i in 0 until assetClassTableModel.rowCount) {
            val unityClass = assetClassTableModel.getValueAt(i, 0) as String
            val name = assetClassTableModel.getValueAt(i, 1) as String
            if (state.assetClassRules[unityClass] != name) return true
        }

        // Directory rules
        if (state.directoryRules.size != directoryTableModel.rowCount) return true
        for (i in 0 until directoryTableModel.rowCount) {
            val name = directoryTableModel.getValueAt(i, 0) as String
            val path = directoryTableModel.getValueAt(i, 1) as String
            val rule = state.directoryRules.getOrNull(i) ?: return true
            if (rule.changelistName != name || rule.path != path) return true
        }

        return false
    }

    override fun apply() {
        val state = ChangelistSorterState.getInstance(project)
        state.groupMetaFiles = groupMetaFilesCheckBox.isSelected
        state.sortScriptableObjectsByClass = sortSOByClassCheckBox.isSelected
        state.removeUnusedChangelists = removeUnusedChangelistsCheckBox.isSelected

        state.sortingRules.clear()
        for (i in 0 until extensionTableModel.rowCount) {
            val name = extensionTableModel.getValueAt(i, 0) as String
            val extensions = extensionTableModel.getValueAt(i, 1) as String
            state.sortingRules[name] = extensions
        }

        state.filenamePatternRules.clear()
        for (i in 0 until patternTableModel.rowCount) {
            val rule = FilenamePatternRule().apply {
                changelistName = patternTableModel.getValueAt(i, 0) as String
                pattern = patternTableModel.getValueAt(i, 1) as String
                matchMode = patternTableModel.getValueAt(i, 2) as String
            }
            state.filenamePatternRules.add(rule)
        }

        state.assetClassRules.clear()
        for (i in 0 until assetClassTableModel.rowCount) {
            val unityClass = assetClassTableModel.getValueAt(i, 0) as String
            val name = assetClassTableModel.getValueAt(i, 1) as String
            state.assetClassRules[unityClass] = name
        }

        state.directoryRules.clear()
        for (i in 0 until directoryTableModel.rowCount) {
            val rule = DirectoryRule().apply {
                changelistName = directoryTableModel.getValueAt(i, 0) as String
                path = directoryTableModel.getValueAt(i, 1) as String
            }
            state.directoryRules.add(rule)
        }
    }

    override fun reset() {
        val state = ChangelistSorterState.getInstance(project)
        groupMetaFilesCheckBox.isSelected = state.groupMetaFiles
        sortSOByClassCheckBox.isSelected = state.sortScriptableObjectsByClass
        removeUnusedChangelistsCheckBox.isSelected = state.removeUnusedChangelists

        extensionTableModel.rowCount = 0
        for ((name, extensions) in state.sortingRules) {
            extensionTableModel.addRow(arrayOf(name, extensions))
        }

        patternTableModel.rowCount = 0
        for (rule in state.filenamePatternRules) {
            patternTableModel.addRow(arrayOf(rule.changelistName, rule.pattern, rule.matchMode))
        }

        assetClassTableModel.rowCount = 0
        for ((unityClass, name) in state.assetClassRules) {
            assetClassTableModel.addRow(arrayOf(unityClass, name))
        }

        directoryTableModel.rowCount = 0
        for (rule in state.directoryRules) {
            directoryTableModel.addRow(arrayOf(rule.changelistName, rule.path))
        }
    }
}
