import com.intellij.openapi.vcs.FilePath
import com.intellij.openapi.vcs.changes.Change
import com.intellij.openapi.vcs.changes.ContentRevision
import io.mockk.every
import io.mockk.mockk
import org.junit.Assert.*
import org.junit.Test
import java.io.File

class SortChangelistActionTest {

    private val action = SortChangelistAction()

    // --- detectAssetClass ---

    @Test
    fun `material asset returns Material class`() {
        val content = File("src/test/testData/SimplygonCastMaterial.asset").readText()
        assertEquals(Pair("Material", null), action.detectAssetClass(content))
    }

    @Test
    fun `monobehaviour with identifier returns assembly and class name`() {
        val content = File("src/test/testData/GameInputController/InputStorageSO/bool_circleMenu.asset").readText()
        assertEquals(Pair("MonoBehaviour", "SOAP::BoolEvent"), action.detectAssetClass(content))
    }

    @Test
    fun `monobehaviour without identifier returns null soClass`() {
        val content = """
            %YAML 1.1
            %TAG !u! tag:unity3d.com,2011:
            --- !u!114 &11400000
            MonoBehaviour:
              m_ObjectHideFlags: 0
              m_EditorClassIdentifier:
        """.trimIndent()
        val result = action.detectAssetClass(content)
        assertEquals("MonoBehaviour", result?.first)
        assertNull(result?.second)
    }

    @Test
    fun `empty content returns null`() {
        assertNull(action.detectAssetClass(""))
    }

    @Test
    fun `yaml header with no class line returns null`() {
        assertNull(action.detectAssetClass("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:"))
    }

    // --- resolveExtension ---

    @Test
    fun `cs file resolves to cs`() {
        assertEquals("cs", action.resolveExtension(mockFilePath("foo.cs"), groupMetaFiles = true))
    }

    @Test
    fun `cs meta with groupMetaFiles true resolves to cs`() {
        assertEquals("cs", action.resolveExtension(mockFilePath("foo.cs.meta"), groupMetaFiles = true))
    }

    @Test
    fun `meta with groupMetaFiles false resolves to meta`() {
        assertEquals("meta", action.resolveExtension(mockFilePath("foo.meta"), groupMetaFiles = false))
    }

    @Test
    fun `no extension resolves to empty string`() {
        assertEquals("", action.resolveExtension(mockFilePath("LICENSE"), groupMetaFiles = true))
    }

    // --- buildChangeByPath ---

    @Test
    fun `map built from 3 changes contains correct path keys`() {
        val changes = listOf(
            makeChange("/project/foo.cs", null),
            makeChange("/project/bar.asset", null),
            makeChange(null, "/project/deleted.cs")
        )
        val map = action.buildChangeByPath(changes)
        assertTrue(map.containsKey("/project/foo.cs"))
        assertTrue(map.containsKey("/project/bar.asset"))
        assertTrue(map.containsKey("/project/deleted.cs"))
        assertEquals(3, map.size)
    }

    @Test
    fun `meta parent lookup via map returns correct change`() {
        val assetChange = makeChange("/project/foo.asset", null)
        val metaChange  = makeChange("/project/foo.asset.meta", null)
        val map = action.buildChangeByPath(listOf(assetChange, metaChange))

        val parentPath = "/project/foo.asset.meta".removeSuffix(".meta")
        assertSame(assetChange, map[parentPath])
    }

    @Test
    fun `deleted change uses beforeRevision path`() {
        val map = action.buildChangeByPath(listOf(makeChange(null, "/project/old.cs")))
        assertTrue(map.containsKey("/project/old.cs"))
    }

    // --- needsAssetRead ---

    @Test
    fun `asset file with sortUnityAssets true needs read`() {
        val state = ChangelistSorterState().apply { sortUnityAssets = true }
        assertTrue(action.needsAssetRead(makeChange("/project/foo.asset", null), state))
    }

    @Test
    fun `asset meta file with sortUnityAssets true needs read`() {
        val state = ChangelistSorterState().apply { sortUnityAssets = true }
        assertTrue(action.needsAssetRead(makeChange("/project/foo.asset.meta", null), state))
    }

    @Test
    fun `cs file does not need read`() {
        val state = ChangelistSorterState().apply { sortUnityAssets = true }
        assertFalse(action.needsAssetRead(makeChange("/project/foo.cs", null), state))
    }

    @Test
    fun `asset file with sortUnityAssets false does not need read`() {
        val state = ChangelistSorterState().apply { sortUnityAssets = false }
        assertFalse(action.needsAssetRead(makeChange("/project/foo.asset", null), state))
    }

    // --- buildContentCache parallel pre-read ---

    @Test
    fun `contentCache contains entry for each asset change`() {
        val state = ChangelistSorterState().apply { sortUnityAssets = true }
        val paths = (1..5).map { "/nonexistent/path/file$it.asset" }
        val changes = paths.map { makeChange(it, null) }

        val cache = action.buildContentCache(changes, state)

        paths.forEach { path ->
            assertTrue("Missing cache entry for $path", cache.containsKey(path))
        }
    }

    @Test
    fun `contentCache excludes non-asset changes`() {
        val state = ChangelistSorterState().apply { sortUnityAssets = true }
        val changes = listOf(
            makeChange("/project/foo.cs", null),
            makeChange("/project/bar.asset", null)
        )
        val cache = action.buildContentCache(changes, state)
        assertFalse(cache.containsKey("/project/foo.cs"))
        assertTrue(cache.containsKey("/project/bar.asset"))
    }

    // --- helpers ---

    private fun mockFilePath(name: String): FilePath {
        val fp = mockk<FilePath>()
        every { fp.name } returns name
        return fp
    }

    private fun makeChange(afterPath: String?, beforePath: String?): Change {
        val change = mockk<Change>()
        if (afterPath != null) {
            val afterRevision = mockk<ContentRevision>()
            val afterFp = mockk<FilePath>()
            every { afterFp.path } returns afterPath
            every { afterFp.name } returns afterPath.substringAfterLast('/')
            every { afterFp.virtualFile } returns null
            every { afterRevision.file } returns afterFp
            every { change.afterRevision } returns afterRevision
            every { change.beforeRevision } returns null
        } else {
            every { change.afterRevision } returns null
            val beforeRevision = mockk<ContentRevision>()
            val beforeFp = mockk<FilePath>()
            every { beforeFp.path } returns beforePath!!
            every { beforeFp.name } returns beforePath.substringAfterLast('/')
            every { beforeRevision.file } returns beforeFp
            every { change.beforeRevision } returns beforeRevision
        }
        return change
    }
}
