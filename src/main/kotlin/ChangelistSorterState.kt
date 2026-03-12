import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.openapi.project.Project
import com.intellij.util.xmlb.XmlSerializerUtil
import com.intellij.util.xmlb.annotations.Tag
import com.intellij.util.xmlb.annotations.XCollection

@Tag("filenameRule")
class FilenamePatternRule {
    var changelistName: String = ""
    var pattern: String = ""
    var matchMode: String = "REGEX"  // "EXTENSION", "EXACT", "REGEX"
}

@Service(Service.Level.PROJECT)
@State(
    name = "ChangelistSorterSettings",
    storages = [Storage("ChangelistSorterSettings.xml")]
)
class ChangelistSorterState : PersistentStateComponent<ChangelistSorterState> {

    var groupMetaFiles: Boolean = true
    var sortScriptableObjectsByClass: Boolean = false
    var removeUnusedChangelists: Boolean = false

    var sortingRules: MutableMap<String, String> = mutableMapOf(
        "Assets" to "asset",
        "Materials" to "mat",
        "Scripts" to "cs",
        "Meta Files" to "meta",
        "Prefabs" to "prefab",
        "Scenes" to "unity",
        "Assembly" to "asmdef",
        "InputActions" to "inputactions",
    )

    @XCollection(style = XCollection.Style.v2)
    var filenamePatternRules: MutableList<FilenamePatternRule> = mutableListOf()

    var assetClassRules: MutableMap<String, String> = mutableMapOf(
        "Material" to "Materials",
        "AnimationClip" to "Animations",
        "AudioClip" to "Audio",
        "Texture2D" to "Textures",
        "PhysicMaterial" to "Physics",
        "LightingDataAsset" to "Lighting"
    )

    override fun getState(): ChangelistSorterState = this

    override fun loadState(state: ChangelistSorterState) {
        XmlSerializerUtil.copyBean(state, this)
    }

    companion object {
        fun getInstance(project: Project): ChangelistSorterState = project.getService(ChangelistSorterState::class.java)
    }
}
