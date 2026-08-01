import SwiftUI

struct ContentView: View {
    var body: some View {
        ConsoleWebView(url: AppConfig.startURL)
            .ignoresSafeArea()
    }
}

#Preview {
    ContentView()
}
