import org.jetbrains.kotlin.gradle.dsl.JvmTarget

// Pure-JVM port of src/YangiAlifbo.Core (no Android API), so it can be tested on a
// desktop JVM against the C# engine. The Android app depends on this module.
plugins {
    id("org.jetbrains.kotlin.jvm")
}

java {
    sourceCompatibility = JavaVersion.VERSION_17
    targetCompatibility = JavaVersion.VERSION_17
}

kotlin {
    compilerOptions {
        jvmTarget.set(JvmTarget.JVM_17)
    }
}

sourceSets {
    main {
        // The same Brotli dictionaries and neural weights the C# engine embeds.
        // Never copied: the .br files in src/YangiAlifbo.Core/Dictionaries are the single source.
        resources.srcDir("../../src/YangiAlifbo.Core/Dictionaries")
        resources.include("*.br")
    }
}

dependencies {
    implementation("org.brotli:dec:0.1.2")
    testImplementation(kotlin("test"))
}

tasks.test {
    useJUnitPlatform()
}

// ./gradlew :engine:parity -Pwords=words.txt -Pout=kotlin.tsv
// Prints "<n> words, sha256 <hash>"; must equal tests/data/parity.sha256.
tasks.register<JavaExec>("parity") {
    group = "verification"
    classpath = sourceSets["main"].runtimeClasspath
    mainClass.set("uz.yangialifbo.engine.ParityKt")
    args(
        providers.gradleProperty("words").getOrElse("words.txt"),
        providers.gradleProperty("out").getOrElse("kotlin.tsv"),
    )
    maxHeapSize = "2g"
}
