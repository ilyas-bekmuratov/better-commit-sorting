import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.openapi.project.Project
import com.intellij.util.xmlb.XmlSerializerUtil
import com.intellij.util.xmlb.annotations.Tag
import com.intellij.util.xmlb.annotations.XCollection

@Tag("sortingRule")
class SortingRule {
    var changelistName: String = ""
    var matchType: String = "EXTENSION"  // "EXTENSION", "REGEX", "EXACT", "DIRECTORY"
    var pattern: String = ""
    var enabled: Boolean = true
}

@Tag("assetClassRule")
class AssetClassRule {
    var unityClass: String = ""
    var changelistName: String = ""
    var enabled: Boolean = true
}

@Tag("presetData")
class PresetData {
    @XCollection(style = XCollection.Style.v2)
    var sortingRules: MutableList<SortingRule> = mutableListOf()
    @XCollection(style = XCollection.Style.v2)
    var assetClassRules: MutableList<AssetClassRule> = mutableListOf()
    var groupMetaFiles: Boolean = true
    var sortScriptableObjectsByClass: Boolean = false
    var removeUnusedChangelists: Boolean = false
    var sortUnityAssets: Boolean = true
    var ignoreEmptyFolderMetas: Boolean = false
}

@Tag("namedPreset")
class NamedPreset {
    var name: String = ""
    var data: PresetData = PresetData()
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
    var sortUnityAssets: Boolean = true
    var ignoreEmptyFolderMetas: Boolean = false

    @XCollection(style = XCollection.Style.v2)
    var sortingRules: MutableList<SortingRule> = mutableListOf(
        SortingRule().apply { changelistName = "Assets";       matchType = "EXTENSION"; pattern = "asset" },
        SortingRule().apply { changelistName = "Materials";    matchType = "EXTENSION"; pattern = "mat" },
        SortingRule().apply { changelistName = "Scripts";      matchType = "EXTENSION"; pattern = "cs" },
        SortingRule().apply { changelistName = "Meta Files";   matchType = "EXTENSION"; pattern = "meta" },
        SortingRule().apply { changelistName = "Prefabs";      matchType = "EXTENSION"; pattern = "prefab" },
        SortingRule().apply { changelistName = "Scenes";       matchType = "EXTENSION"; pattern = "unity" },
        SortingRule().apply { changelistName = "Assembly";     matchType = "EXTENSION"; pattern = "asmdef" },
        SortingRule().apply { changelistName = "InputActions"; matchType = "EXTENSION"; pattern = "inputactions" },
    )

    @XCollection(style = XCollection.Style.v2)
    var assetClassRules: MutableList<AssetClassRule> = mutableListOf(
        AssetClassRule().apply { unityClass = "Material";          changelistName = "Materials" },
        AssetClassRule().apply { unityClass = "AnimationClip";     changelistName = "Animations" },
        AssetClassRule().apply { unityClass = "AudioClip";         changelistName = "Audio" },
        AssetClassRule().apply { unityClass = "Texture2D";         changelistName = "Textures" },
        AssetClassRule().apply { unityClass = "PhysicMaterial";    changelistName = "Physics" },
        AssetClassRule().apply { unityClass = "LightingDataAsset"; changelistName = "Lighting" },
    )

    @XCollection(style = XCollection.Style.v2)
    var presets: MutableList<NamedPreset> = mutableListOf()

    fun savePreset(name: String, data: PresetData) {
        val existing = presets.indexOfFirst { it.name == name }
        val preset = NamedPreset().apply { this.name = name; this.data = data }
        if (existing >= 0) presets[existing] = preset else presets.add(preset)
    }

    fun deletePreset(name: String) {
        presets.removeAll { it.name == name }
    }

    override fun getState(): ChangelistSorterState = this

    override fun loadState(state: ChangelistSorterState) {
        XmlSerializerUtil.copyBean(state, this)
    }

    companion object {
        fun getInstance(project: Project): ChangelistSorterState = project.getService(ChangelistSorterState::class.java)
    }
}
