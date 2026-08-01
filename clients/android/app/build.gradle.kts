plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
}

android {
    namespace = "com.qiklog.console"
    compileSdk = 34

    defaultConfig {
        applicationId = "com.qiklog.console"
        minSdk = 26
        targetSdk = 34
        versionCode = 1
        versionName = "1.0"

        val startUrl = (project.findProperty("QIKLOG_APP_URL") as String?)
            ?: System.getenv("QIKLOG_APP_URL")
            ?: "https://qiklog.up.railway.app"
        buildConfigField("String", "START_URL", "\"$startUrl\"")
    }

    buildFeatures {
        buildConfig = true
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = "17"
    }
}

dependencies {
    implementation("androidx.core:core-ktx:1.13.1")
    implementation("androidx.appcompat:appcompat:1.7.0")
    implementation("com.google.android.material:material:1.12.0")
    implementation("androidx.activity:activity-ktx:1.9.1")
    implementation("androidx.webkit:webkit:1.11.0")
}
