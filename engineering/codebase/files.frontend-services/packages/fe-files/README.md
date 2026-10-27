# @wow-two-beta/fe-files

The Files domain's UI: the contract mirror, the file icon pack, and a sample in two modes:

- **Embed** — `GreetingWidget` greets through the Files contract. It calls the consumer's own backend (Form 1, the
  default same-origin client) or the running Files service (Forms 2 and 3, `createGreetingsClient({ baseUrl })`).
- **Forward** — `forwardToHostedGreeting` sends the user to the service's hosted greeting page, which links back to
  `returnUrl` when done.

```bash
pnpm add @wow-two-beta/fe-files @wow-two-beta/fe vue
```

```ts
import { FileIcon, fileTypeOf } from '@wow-two-beta/fe-files/icons';
import { GreetingWidget, createGreetingsClient, forwardToHostedGreeting } from '@wow-two-beta/fe-files/kit';
import { GreetingRouteConstants, type GreetingModel } from '@wow-two-beta/fe-files/contracts';
```

The widget is styled with Tailwind utilities on the fe sdk theme; add the package to the consumer's Tailwind sources:

```css
@source '../../node_modules/@wow-two-beta/fe-files/dist';
```

| Export | Holds |
|---|---|
| `./contracts` | `FileKind`, `GreetingModel`, `GreetingConstants`, `GreetingRouteConstants` — the TypeScript mirror of the be contract |
| `./icons` | `FileIcon`, `fileTypeOf`, `fileKind`, `extensionOf`, `fileTypes` — an icon for any extension, from the be registry; usable without a Files instance |
| `./kit` | `GreetingWidget`, `createGreetingsClient`, `hostedGreetingUrl`, `forwardToHostedGreeting` |

`<FileIcon :name | :ext | :mime :size :monochrome>` draws a page in the kind's colour, the kind's glyph and, from 24 px,
the extension label; recolour a kind with `--files-icon-{kind}`. The type table is generated from the be registry
(`pnpm gen:file-types`), and a test fails while the two differ.
