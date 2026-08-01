import SwiftUI
import WebKit

/// Full-screen WKWebView hosting the QikLog dashboard.
struct ConsoleWebView: UIViewRepresentable {
    let url: URL

    func makeCoordinator() -> Coordinator {
        Coordinator()
    }

    func makeUIView(context: Context) -> WKWebView {
        let config = WKWebViewConfiguration()
        config.allowsInlineMediaPlayback = true
        config.defaultWebpagePreferences.allowsContentJavaScript = true

        let webView = WKWebView(frame: .zero, configuration: config)
        webView.navigationDelegate = context.coordinator
        webView.allowsBackForwardNavigationGestures = true
        webView.scrollView.contentInsetAdjustmentBehavior = .never
        webView.customUserAgent = (webView.value(forKey: "userAgent") as? String)
            .map { "\($0) \(AppConfig.userAgentSuffix)" }
            ?? AppConfig.userAgentSuffix

        webView.load(URLRequest(url: url))
        return webView
    }

    func updateUIView(_ webView: WKWebView, context: Context) {
        // Intentionally empty — this shell loads once and stays on the SPA.
    }

    final class Coordinator: NSObject, WKNavigationDelegate {
        func webView(
            _ webView: WKWebView,
            decidePolicyFor navigationAction: WKNavigationAction,
            decisionHandler: @escaping (WKNavigationActionPolicy) -> Void
        ) {
            guard let requestURL = navigationAction.request.url else {
                decisionHandler(.allow)
                return
            }

            // Keep OIDC / same-host navigations inside the shell; open strangers externally.
            if shouldOpenExternally(requestURL) {
                UIApplication.shared.open(requestURL)
                decisionHandler(.cancel)
                return
            }

            decisionHandler(.allow)
        }

        private func shouldOpenExternally(_ url: URL) -> Bool {
            guard let scheme = url.scheme?.lowercased(), scheme == "http" || scheme == "https" else {
                return false
            }

            let host = url.host?.lowercased() ?? ""
            let allowed: Set<String> = [
                "qiklog.up.railway.app",
                "qiklog.com",
                "www.qiklog.com",
                "signin.qiklog.com",
                "qiklog-prod-bnimdu.us1.zitadel.cloud",
                "localhost",
                "127.0.0.1"
            ]

            if allowed.contains(host) { return false }
            if host.hasSuffix(".zitadel.cloud") { return false }
            if host.hasSuffix(".qiklog.com") { return false }

            return true
        }
    }
}
