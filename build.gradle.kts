import java.util.Base64
import org.jetbrains.changelog.Changelog
import org.jetbrains.changelog.markdownToHTML
import org.jetbrains.intellij.platform.gradle.TestFrameworkType

plugins {
    id("java") // Java support
    alias(libs.plugins.kotlin) // Kotlin support
    alias(libs.plugins.intelliJPlatform) // IntelliJ Platform Gradle Plugin
    alias(libs.plugins.changelog) // Gradle Changelog Plugin
    alias(libs.plugins.qodana) // Gradle Qodana Plugin
    alias(libs.plugins.kover) // Gradle Kover Plugin
}

group = providers.gradleProperty("pluginGroup").get()
version = providers.gradleProperty("pluginVersion").get()

// Set the JVM language level used to build the project.
kotlin {
    jvmToolchain(21)
    jvmToolchain(8)
}

// Configure project's dependencies
repositories {
    mavenCentral()

    // IntelliJ Platform Gradle Plugin Repositories Extension - read more: https://plugins.jetbrains.com/docs/intellij/tools-intellij-platform-gradle-plugin-repositories-extension.html
    intellijPlatform {
        defaultRepositories()
    }
}

// Dependencies are managed with Gradle version catalog - read more: https://docs.gradle.org/current/userguide/version_catalogs.html
dependencies {
    testImplementation(libs.junit)
    testImplementation(libs.opentest4j)
    testImplementation("io.mockk:mockk:1.13.10")

    // IntelliJ Platform Gradle Plugin Dependencies Extension - read more: https://plugins.jetbrains.com/docs/intellij/tools-intellij-platform-gradle-plugin-dependencies-extension.html
    intellijPlatform {
        intellijIdea(providers.gradleProperty("platformVersion"))

        // Plugin Dependencies. Uses `platformBundledPlugins` property from the gradle.properties file for bundled IntelliJ Platform plugins.
        bundledPlugins(providers.gradleProperty("platformBundledPlugins").map { it.split(',') })

        // Plugin Dependencies. Uses `platformPlugins` property from the gradle.properties file for plugin from JetBrains Marketplace.
        plugins(providers.gradleProperty("platformPlugins").map { it.split(',') })

        // Module Dependencies. Uses `platformBundledModules` property from the gradle.properties file for bundled IntelliJ Platform modules.
        bundledModules(providers.gradleProperty("platformBundledModules").map { it.split(',') })

        testFramework(TestFrameworkType.Platform)
    }
    implementation(kotlin("stdlib-jdk8"))
}

// Configure IntelliJ Platform Gradle Plugin - read more: https://plugins.jetbrains.com/docs/intellij/tools-intellij-platform-gradle-plugin-extension.html
intellijPlatform {
    pluginConfiguration {
        name = providers.gradleProperty("pluginName")
        version = providers.gradleProperty("pluginVersion")

        // Extract the <!-- Plugin description --> section from README.md and provide for the plugin's manifest
        description = providers.fileContents(layout.projectDirectory.file("README.md")).asText.map {
            val start = "<!-- Plugin description -->"
            val end = "<!-- Plugin description end -->"

            with(it.lines()) {
                if (!containsAll(listOf(start, end))) {
                    throw GradleException("Plugin description section not found in README.md:\n$start ... $end")
                }
                subList(indexOf(start) + 1, indexOf(end)).joinToString("\n").let(::markdownToHTML)
            }
        }

        val changelog = project.changelog // local variable for configuration cache compatibility
        // Get the latest available change notes from the changelog file
        changeNotes = providers.gradleProperty("pluginVersion").map { pluginVersion ->
            with(changelog) {
                renderItem(
                    (getOrNull(pluginVersion) ?: getUnreleased())
                        .withHeader(false)
                        .withEmptySections(false),
                    Changelog.OutputType.HTML,
                )
            }
        }

        ideaVersion {
            sinceBuild = providers.gradleProperty("pluginSinceBuild")
        }
    }

    signing {
        certificateChain = providers.environmentVariable("CERTIFICATE_CHAIN")
        privateKey = providers.environmentVariable("PRIVATE_KEY")
        password = providers.environmentVariable("PRIVATE_KEY_PASSWORD")
    }

    publishing {
        token = providers.environmentVariable("PUBLISH_TOKEN")
        // The pluginVersion is based on the SemVer (https://semver.org) and supports pre-release labels, like 2.1.7-alpha.3
        // Specify pre-release label to publish the plugin in a custom Release Channel automatically. Read more:
        // https://plugins.jetbrains.com/docs/intellij/publishing-plugin.html#specifying-a-release-channel
        channels = providers.gradleProperty("pluginVersion").map { listOf(it.substringAfter('-', "").substringBefore('.').ifEmpty { "default" }) }
    }

    pluginVerification {
        ides {
            recommended()
        }
    }
}

// Configure Gradle Changelog Plugin - read more: https://github.com/JetBrains/gradle-changelog-plugin
changelog {
    groups.empty()
    repositoryUrl = providers.gradleProperty("pluginRepositoryUrl")
    versionPrefix = ""
}

// Configure Gradle Kover Plugin - read more: https://kotlin.github.io/kotlinx-kover/gradle-plugin/#configuration-details
kover {
    currentProject {
        instrumentation {
            // JBR (JetBrains Runtime) used by IntelliJ Platform test sandbox is incompatible
            // with the kover javaagent — disable instrumentation for the test task to prevent
            // a JVM crash before any tests run.
            disabledForTestTasks.add("test")
        }
    }
    reports {
        total {
            xml {
                onCheck = true
            }
        }
    }
}

tasks {
    wrapper {
        gradleVersion = providers.gradleProperty("gradleVersion").get()
    }

    publishPlugin {
        dependsOn(patchChangelog)
    }

    // IU-2025.2.5 + IntelliJ Platform Plugin 2.11.0 incompatibility: the coroutines-javaagent
    // stub delegates to the IDE's bundled AgentPremain, which calls a method
    // (getEnableCreationStackTraces$kotlinx_coroutines_core) that no longer exists in the
    // coroutines version bundled by IU-2025.2.5, crashing the JVM before any test runs.
    // Fix: replace the stub with a real no-op agent jar that loads its own premain class.
    register("installNoOpCoroutinesAgent") {
        dependsOn("initializeIntellijPlatformPlugin")
        val agentFile = file(".intellijPlatform/coroutines-javaagent.jar")
        outputs.file(agentFile)
        @Suppress("SpellCheckingInspection")
        val noOpAgentBase64 = """
            UEsDBAoAAAgAAM+Ll1wAAAAAAAAAAAAAAAAJAAQATUVUQS1JTkYv/soAAFBLAwQUAAgICADPi5dc
            AAAAAAAAAAAAAAAAFAAAAE1FVEEtSU5GL01BTklGRVNULk1GJcwxDsIwDEDRPVLukBGGVC1jNshc
            QB3YLXCQJWoj2x24fSMx/6c/A1ND8/xANRIuaRrGGO6KKxDn+gGzkq5y+1ZR2ZwY7fxG9hgqcF7Q
            Fdia6Pq32LXrhj0rguMrX34lnfp0mMZ0mOmpYtL8GEMMO1BLBwgotr3ScAAAAH0AAABQSwMEFAAI
            CAgANouXXAAAAAAAAAAAAAAAABkAAABOb09wQ29yb3V0aW5lc0FnZW50LmNsYXNzbY/BagIxEIb/
            cdfd1toqlIIePHjTHroPsKUgQkEQPVi8Z21YIm4iMdv36qnQQx+gDyWdLKUK3Rz+mfx88yfzffz8
            AjBBt4UGghhhG01EhO5WvIlkJ3SeLLOt3DhC9Ki0ck+EYDRex7gg3C7Mcj811pROaXmY5FIzF07N
            qyR05uwtyiKT9kVkO3bivZWFUJqQjuan/JWzSufpmaP0wdmy4LRk9tcKp4xOx2vCXc2091srU9qN
            fFb+sV7N3x78GIaIeVd/ApDflvWSbwOuxLV5/wF654YTWaPKbDByhfYv2q8c/MeCCrs+w6gOC1lv
            qozOD1BLBwgcDlOT8QAAAIIBAABQSwECCgAKAAAIAADPi5dcAAAAAAAAAAAAAAAACQAEAAAAAAAA
            AAAAAAAAAAAATUVUQS1JTkYv/soAAFBLAQIUABQACAgIAM+Ll1wotr3ScAAAAH0AAAAUAAAAAAAA
            AAAAAAAAACsAAABNRVRBLUlORi9NQU5JRkVTVC5NRlBLAQIUABQACAgIADaLl1wcDlOT8QAAAIIB
            AAAZAAAAAAAAAAAAAAAAAN0AAABOb09wQ29yb3V0aW5lc0FnZW50LmNsYXNzUEsFBgAAAAADAAMA
            xAAAABUCAAAAAA==
        """.trimIndent().replace("\n", "")
        doLast {
            agentFile.writeBytes(Base64.getDecoder().decode(noOpAgentBase64.replace("\\s".toRegex(), "")))
        }
    }

    test {
        dependsOn("installNoOpCoroutinesAgent")
    }
}

intellijPlatformTesting {
    runIde {
        register("runIdeForUiTests") {
            task {
                jvmArgumentProviders += CommandLineArgumentProvider {
                    listOf(
                        "-Drobot-server.port=8082",
                        "-Dide.mac.message.dialogs.as.sheets=false",
                        "-Djb.privacy.policy.text=<!--999.999-->",
                        "-Djb.consents.confirmation.enabled=false",
                    )
                }
            }

            plugins {
                robotServerPlugin()
            }
        }
    }
}
