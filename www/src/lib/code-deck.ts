export type DeckEscape = 'json' | 'csharp' | 'js';

export type DeckTab = {
  id: string;
  label: string;
  escape: DeckEscape;
  install: string;
  template: string;
};

export const DECK_TABS: DeckTab[] = [
  {
    id: 'curl',
    label: 'curl',
    escape: 'json',
    install: 'Already on your machine. (brew install curl if not)',
    template: `curl -X POST https://api.qiklog.com/v1/logs \\
  -H "Content-Type: application/json" \\
  -H "Authorization: Bearer $QIKLOG_API_KEY" \\
  -d '{"source":"demo","level":"info","message":"__MSG__"}'`,
  },
  {
    id: 'csharp',
    label: 'C#',
    escape: 'csharp',
    install: 'No install. Plain HttpClient.',
    template: `using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization =
    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "YOUR_API_KEY");

await client.PostAsJsonAsync("https://api.qiklog.com/v1/logs", new
{
    source = "demo",
    level = "info",
    message = "__MSG__"
});`,
  },
  {
    id: 'javascript',
    label: 'JavaScript',
    escape: 'js',
    install: 'No install. Plain fetch.',
    template: `await fetch("https://api.qiklog.com/v1/logs", {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    Authorization: "Bearer YOUR_API_KEY"
  },
  body: JSON.stringify({
    source: "demo",
    level: "info",
    message: "__MSG__"
  })
});`,
  },
];
