/**
 * The one line a visitor pastes to send to their log link. `__URL__` is replaced
 * with the link. No key, no JSON, no headers: the request body is the line.
 */
export type LinkTab = {
  id: string;
  label: string;
  template: string;
};

export const LINK_TABS: LinkTab[] = [
  {
    id: 'curl',
    label: 'curl',
    template: `curl __URL__ -d "it broke"`,
  },
  {
    id: 'javascript',
    label: 'JavaScript',
    template: `await fetch("__URL__", { method: "POST", body: "it broke" });`,
  },
  {
    id: 'csharp',
    label: 'C#',
    template: `await new HttpClient().PostAsync("__URL__", new StringContent("it broke"));`,
  },
];

/** Shown before a link exists, so the page is not empty. */
export const PLACEHOLDER_URL = 'https://qiklog.com/l/your-link';
