import Foundation

enum AppConfig {
    /// Hosted dashboard. Override with env `QIKLOG_APP_URL` when launching from Xcode schemes.
    static var startURL: URL {
        if let raw = ProcessInfo.processInfo.environment["QIKLOG_APP_URL"]?
            .trimmingCharacters(in: .whitespacesAndNewlines),
           !raw.isEmpty,
           let url = URL(string: raw)
        {
            return url
        }

        return URL(string: "https://qiklog.up.railway.app")!
    }

    static let userAgentSuffix = "QikLogConsole-iOS/1.0"
}
