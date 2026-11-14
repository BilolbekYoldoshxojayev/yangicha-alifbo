import org.jetbrains.kotlin.gradle.dsl.JvmTarget

plugins {
    id("com.android.application")
}

// The Android app has its own version (not the Windows app's): raise versionCode on every release.
val appVersion = "1.0.0"
val appVersionCode = 1

android {
    namespace = "uz.yangialifbo.keyboard"
    compileSdk = 37

    defaultConfig {
        applicationId = "uz.yangialifbo.keyboard"
        minSdk = 26
        targetSdk = 36
        versionName = appVersion
        versionCode = appVersionCode
    }

    signingConfigs {
        // Release key from CI secrets (see .github/workflows/android.yml). Without it the
        // release APK is signed with the debug key and cannot update an APK signed otherwise.
        val keystore = System.getenv("ANDROID_KEYSTORE_FILE")
        if (keystore != null) {
            create("release") {
                storeFile = file(keystore)
                storePassword = System.getenv("ANDROID_KEYSTORE_PASSWORD")
                keyAlias = System.getenv("ANDROID_KEY_ALIAS")
                keyPassword = System.getenv("ANDROID_KEY_PASSWORD")
            }
        }
    }

    buildTypes {
        release {
            isMinifyEnabled = true
            isShrinkResources = true
            proguardFiles(getDefaultProguardFile("proguard-android-optimize.txt"), "proguard-rules.pro")
            signingConfig = signingConfigs.findByName("release") ?: signingConfigs.getByName("debug")
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    packaging {
        resources {
            // Brotli dictionary files are already compressed; keep them stored as-is.
            excludes += "/META-INF/{AL2.0,LGPL2.1}"
        }
    }

    androidResources {
        noCompress += "br"
    }
}

kotlin {
    compilerOptions {
        jvmTarget.set(JvmTarget.JVM_17)
    }
}

dependencies {
    implementation(project(":engine"))
    implementation("androidx.core:core-ktx:1.16.0")
    testImplementation("junit:junit:4.13.2")
}
