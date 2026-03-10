import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.openapi.project.Project
import com.intellij.util.xmlb.XmlSerializerUtil

@Service(Service.Level.PROJECT)
@State(
    name = "ChangelistSorterSettings",
    storages = [Storage("ChangelistSorterSettings.xml")]
)
class ChangelistSorterState : PersistentStateComponent<ChangelistSorterState> {

    var groupMetaFiles: Boolean = true

    // These are your default groups
    var sortingRules: MutableMap<String, String> = mutableMapOf(
        "Assets" to "asset",
        "Materials" to "mat",
        "Scripts" to "cs",
        "Meta Files" to "meta",
        "Prefabs" to "prefab",
        "Scenes" to "unity",
        "Assembly" to "asmdef"
    )

    override fun getState(): ChangelistSorterState = this

    override fun loadState(state: ChangelistSorterState) {
        XmlSerializerUtil.copyBean(state, this)
    }

    companion object {
        fun getInstance(project: Project): ChangelistSorterState = project.getService(ChangelistSorterState::class.java)
    }
}