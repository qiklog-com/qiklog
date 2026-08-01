package com.qiklog.console

object AppConfig {
    val startUrl: String = BuildConfig.START_URL

    val userAgentSuffix: String = "QikLogConsole-Android/1.0"

    private val allowedHosts = setOf(
        "qiklog.up.railway.app",
        "qiklog.com",
        "www.qiklog.com",
        "signin.qiklog.com",
        "qiklog-prod-bnimdu.us1.zitadel.cloud",
        "localhost",
        "127.0.0.1",
        "10.0.2.2", // Android emulator → host machine
    )

    fun shouldOpenExternally(host: String?): Boolean {
        if (host.isNullOrBlank()) return false
        val h = host.lowercase()
        if (h in allowedHosts) return false
        if (h.endsWith(".zitadel.cloud")) return false
        if (h.endsWith(".qiklog.com")) return false
        return true
    }
}
