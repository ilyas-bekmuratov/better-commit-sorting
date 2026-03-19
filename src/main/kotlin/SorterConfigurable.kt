import com.intellij.openapi.fileChooser.FileChooser
import com.intellij.openapi.fileChooser.FileChooserDescriptorFactory
import com.intellij.openapi.options.Configurable
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.Messages
import com.intellij.openapi.vcs.changes.ChangeListManager
import com.intellij.ui.ToolbarDecorator
import com.intellij.ui.table.JBTable
import java.awt.BorderLayout
import java.awt.Dimension
import java.awt.FlowLayout
import javax.swing.BorderFactory
import javax.swing.BoxLayout
import javax.swing.JButton
import javax.swing.JCheckBox
import javax.swing.JComboBox
import javax.swing.JComponent
import javax.swing.JLabel
import javax.swing.JPanel
import javax.swing.JScrollPane
import javax.swing.table.DefaultTableModel

class SorterConfigurable(private val project: Project) : Configurable {

    private var myPanel: JPanel? = null
    private lateinit var unifiedTableModel: DefaultTableModel
    private lateinit var unifiedTable: JBTable
    private lateinit var assetClassTableModel: DefaultTableModel
    private lateinit var assetClassTable: JBTable
    private lateinit var groupMetaFilesCheckBox: JCheckBox
    private lateinit var sortSOByClassCheckBox: JCheckBox
    private lateinit var removeUnusedChangelistsCheckBox: JCheckBox
    private lateinit var sortUnityAssetsCheckBox: JCheckBox
    private lateinit var ignoreEmptyFolderMetasCheckBox: JCheckBox

    override fun getDisplayName(): String = "Changelist Sorter"

    override fun createComponent(): JComponent? {
        // Unified sorting rules table: Enabled | Changelist | Type | Pattern
        unifiedTableModel = object : DefaultTableModel(arrayOf("Enabled", "Changelist", "Type", "Pattern"), 0) {
            override fun getColumnClass(columnIndex: Int): Class<*> =
                if (columnIndex == 0) java.lang.Boolean::class.java else String::class.java
            override fun isCellEditable(row: Int, column: Int): Boolean = true
        }
        unifiedTable = JBTable(unifiedTableModel)
        unifiedTable.columnModel.getColumn(0).preferredWidth = 60
        unifiedTable.columnModel.getColumn(1).preferredWidth = 150
        unifiedTable.columnModel.getColumn(2).preferredWidth = 110
        unifiedTable.columnModel.getColumn(2).cellEditor =
            javax.swing.DefaultCellEditor(JComboBox(arrayOf("EXTENSION", "REGEX", "EXACT", "DIRECTORY")))

        val unifiedDecorator = ToolbarDecorator.createDecorator(unifiedTable)
            .setAddAction {
                val existingLists = ChangeListManager.getInstance(project).changeLists.map { it.name }
                val listOptions = (existingLists + "New Changelist...").toTypedArray()
                val listIndex = Messages.showChooseDialog(
                    "Select changelist:", "Add Sorting Rule", listOptions, listOptions.first(), null
                )
                if (listIndex < 0) return@setAddAction
                val changelistName = if (listIndex == listOptions.lastIndex) {
                    Messages.showInputDialog("Enter changelist name:", "New Changelist", null)
                        ?: return@setAddAction
                } else {
                    listOptions[listIndex]
                }

                val typeOptions = arrayOf("EXTENSION", "REGEX", "EXACT", "DIRECTORY")
                val typeIndex = Messages.showChooseDialog(
                    "Select match type:", "Add Sorting Rule", typeOptions, "EXTENSION", null
                )
                if (typeIndex < 0) return@setAddAction
                val matchType = typeOptions[typeIndex]

                val pattern = if (matchType == "DIRECTORY") {
                    val descriptor = FileChooserDescriptorFactory.createSingleFolderDescriptor()
                    val chosen = FileChooser.chooseFile(descriptor, project, null) ?: return@setAddAction
                    chosen.path
                } else {
                    Messages.showInputDialog(
                        "Enter pattern (e.g. cs, mat for EXTENSION):", "Add Sorting Rule", null
                    ) ?: return@setAddAction
                }

                unifiedTableModel.addRow(arrayOf<Any>(true, changelistName, matchType, pattern))
            }
            .setRemoveAction {
                val selected = unifiedTable.selectedRow
                if (selected >= 0) unifiedTableModel.removeRow(selected)
            }
            .setMoveUpAction {
                val selected = unifiedTable.selectedRow
                if (selected > 0) {
                    unifiedTableModel.moveRow(selected, selected, selected - 1)
                    unifiedTable.selectionModel.setSelectionInterval(selected - 1, selected - 1)
                }
            }
            .setMoveDownAction {
                val selected = unifiedTable.selectedRow
                if (selected >= 0 && selected < unifiedTableModel.rowCount - 1) {
                    unifiedTableModel.moveRow(selected, selected, selected + 1)
                    unifiedTable.selectionModel.setSelectionInterval(selected + 1, selected + 1)
                }
            }

        // Asset class rules table: Enabled | Unity Class | Changelist
        assetClassTableModel = object : DefaultTableModel(arrayOf("Enabled", "Unity Class", "Changelist"), 0) {
            override fun getColumnClass(columnIndex: Int): Class<*> =
                if (columnIndex == 0) java.lang.Boolean::class.java else String::class.java
            override fun isCellEditable(row: Int, column: Int): Boolean = true
        }
        assetClassTable = JBTable(assetClassTableModel)
        assetClassTable.columnModel.getColumn(0).preferredWidth = 60
        assetClassTable.columnModel.getColumn(1).preferredWidth = 150

        val assetClassDecorator = ToolbarDecorator.createDecorator(assetClassTable)
            .setAddAction {
                val unityClass = Messages.showInputDialog(
                    "Enter Unity class name (e.g. Material, Texture2D):", "Add Asset Class Rule", null
                ) ?: return@setAddAction
                val name = Messages.showInputDialog("Enter changelist name:", "Add Asset Class Rule", null)
                    ?: return@setAddAction
                assetClassTableModel.addRow(arrayOf<Any>(true, unityClass, name))
            }
            .setRemoveAction {
                val selected = assetClassTable.selectedRow
                if (selected >= 0) assetClassTableModel.removeRow(selected)
            }

        // Checkboxes
        groupMetaFilesCheckBox = JCheckBox("Group changes with meta files")
        sortSOByClassCheckBox = JCheckBox("Sort ScriptableObjects by class type")
        removeUnusedChangelistsCheckBox = JCheckBox("Remove empty changelists after sorting")
        sortUnityAssetsCheckBox = JCheckBox("Sort Unity Assets (inspect .asset file content)")
        ignoreEmptyFolderMetasCheckBox = JCheckBox("Unstage added meta files for empty folders")

        val checkboxPanel = JPanel()
        checkboxPanel.layout = BoxLayout(checkboxPanel, BoxLayout.Y_AXIS)
        checkboxPanel.add(groupMetaFilesCheckBox)
        checkboxPanel.add(sortSOByClassCheckBox)
        checkboxPanel.add(removeUnusedChangelistsCheckBox)
        checkboxPanel.add(sortUnityAssetsCheckBox)
        checkboxPanel.add(ignoreEmptyFolderMetasCheckBox)

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

        addSection("Sorting Rules:", unifiedDecorator.createPanel(), 200)
        addSection("Asset Class Rules:", assetClassDecorator.createPanel())

        val scrollPane = JScrollPane(tablesPanel)
        scrollPane.border = null

        val resetButton = JButton("Reset to Defaults")
        resetButton.background = java.awt.Color(180, 40, 40)
        resetButton.foreground = java.awt.Color.WHITE
        resetButton.isOpaque = true
        resetButton.addActionListener {
            val confirmed = Messages.showYesNoDialog(
                "This will reset all sorting rules and settings to their defaults. Continue?",
                "Reset to Defaults",
                Messages.getWarningIcon()
            )
            if (confirmed == Messages.YES) resetToDefaults()
        }
        val bottomPanel = JPanel(FlowLayout(FlowLayout.LEFT))
        bottomPanel.add(resetButton)

        val wrapperPanel = JPanel(BorderLayout())
        wrapperPanel.add(checkboxPanel, BorderLayout.NORTH)
        wrapperPanel.add(scrollPane, BorderLayout.CENTER)
        wrapperPanel.add(bottomPanel, BorderLayout.SOUTH)

        myPanel = wrapperPanel
        return myPanel
    }

    private fun resetToDefaults() {
        groupMetaFilesCheckBox.isSelected = true
        sortSOByClassCheckBox.isSelected = false
        removeUnusedChangelistsCheckBox.isSelected = false
        sortUnityAssetsCheckBox.isSelected = true
        ignoreEmptyFolderMetasCheckBox.isSelected = false

        unifiedTableModel.rowCount = 0
        listOf(
            arrayOf<Any>(true, "Assets",       "EXTENSION", "asset"),
            arrayOf<Any>(true, "Materials",    "EXTENSION", "mat"),
            arrayOf<Any>(true, "Scripts",      "EXTENSION", "cs"),
            arrayOf<Any>(true, "Meta Files",   "EXTENSION", "meta"),
            arrayOf<Any>(true, "Prefabs",      "EXTENSION", "prefab"),
            arrayOf<Any>(true, "Scenes",       "EXTENSION", "unity"),
            arrayOf<Any>(true, "Assembly",     "EXTENSION", "asmdef"),
            arrayOf<Any>(true, "InputActions", "EXTENSION", "inputactions"),
        ).forEach { unifiedTableModel.addRow(it) }

        assetClassTableModel.rowCount = 0
        listOf(
            arrayOf<Any>(true, "Material",          "Materials"),
            arrayOf<Any>(true, "AnimationClip",     "Animations"),
            arrayOf<Any>(true, "AudioClip",         "Audio"),
            arrayOf<Any>(true, "Texture2D",         "Textures"),
            arrayOf<Any>(true, "PhysicMaterial",    "Physics"),
            arrayOf<Any>(true, "LightingDataAsset", "Lighting"),
        ).forEach { assetClassTableModel.addRow(it) }
    }

    override fun isModified(): Boolean {
        val state = ChangelistSorterState.getInstance(project)
        if (state.groupMetaFiles != groupMetaFilesCheckBox.isSelected) return true
        if (state.sortScriptableObjectsByClass != sortSOByClassCheckBox.isSelected) return true
        if (state.removeUnusedChangelists != removeUnusedChangelistsCheckBox.isSelected) return true
        if (state.sortUnityAssets != sortUnityAssetsCheckBox.isSelected) return true
        if (state.ignoreEmptyFolderMetas != ignoreEmptyFolderMetasCheckBox.isSelected) return true

        if (state.sortingRules.size != unifiedTableModel.rowCount) return true
        for (i in 0 until unifiedTableModel.rowCount) {
            val rule = state.sortingRules.getOrNull(i) ?: return true
            if (rule.enabled != unifiedTableModel.getValueAt(i, 0) as Boolean) return true
            if (rule.changelistName != unifiedTableModel.getValueAt(i, 1) as String) return true
            if (rule.matchType != unifiedTableModel.getValueAt(i, 2) as String) return true
            if (rule.pattern != unifiedTableModel.getValueAt(i, 3) as String) return true
        }

        if (state.assetClassRules.size != assetClassTableModel.rowCount) return true
        for (i in 0 until assetClassTableModel.rowCount) {
            val rule = state.assetClassRules.getOrNull(i) ?: return true
            if (rule.enabled != assetClassTableModel.getValueAt(i, 0) as Boolean) return true
            if (rule.unityClass != assetClassTableModel.getValueAt(i, 1) as String) return true
            if (rule.changelistName != assetClassTableModel.getValueAt(i, 2) as String) return true
        }

        return false
    }

    override fun apply() {
        val state = ChangelistSorterState.getInstance(project)
        state.groupMetaFiles = groupMetaFilesCheckBox.isSelected
        state.sortScriptableObjectsByClass = sortSOByClassCheckBox.isSelected
        state.removeUnusedChangelists = removeUnusedChangelistsCheckBox.isSelected
        state.sortUnityAssets = sortUnityAssetsCheckBox.isSelected
        state.ignoreEmptyFolderMetas = ignoreEmptyFolderMetasCheckBox.isSelected

        state.sortingRules.clear()
        for (i in 0 until unifiedTableModel.rowCount) {
            state.sortingRules.add(SortingRule().apply {
                enabled        = unifiedTableModel.getValueAt(i, 0) as Boolean
                changelistName = unifiedTableModel.getValueAt(i, 1) as String
                matchType      = unifiedTableModel.getValueAt(i, 2) as String
                pattern        = unifiedTableModel.getValueAt(i, 3) as String
            })
        }

        state.assetClassRules.clear()
        for (i in 0 until assetClassTableModel.rowCount) {
            state.assetClassRules.add(AssetClassRule().apply {
                enabled        = assetClassTableModel.getValueAt(i, 0) as Boolean
                unityClass     = assetClassTableModel.getValueAt(i, 1) as String
                changelistName = assetClassTableModel.getValueAt(i, 2) as String
            })
        }
    }

    override fun reset() {
        val state = ChangelistSorterState.getInstance(project)
        groupMetaFilesCheckBox.isSelected = state.groupMetaFiles
        sortSOByClassCheckBox.isSelected = state.sortScriptableObjectsByClass
        removeUnusedChangelistsCheckBox.isSelected = state.removeUnusedChangelists
        sortUnityAssetsCheckBox.isSelected = state.sortUnityAssets
        ignoreEmptyFolderMetasCheckBox.isSelected = state.ignoreEmptyFolderMetas

        unifiedTableModel.rowCount = 0
        for (rule in state.sortingRules) {
            unifiedTableModel.addRow(arrayOf<Any>(rule.enabled, rule.changelistName, rule.matchType, rule.pattern))
        }

        assetClassTableModel.rowCount = 0
        for (rule in state.assetClassRules) {
            assetClassTableModel.addRow(arrayOf<Any>(rule.enabled, rule.unityClass, rule.changelistName))
        }
    }
}
