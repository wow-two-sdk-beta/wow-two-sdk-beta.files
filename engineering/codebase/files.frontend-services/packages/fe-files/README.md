# @wow-two-beta/fe-files

The Files domain's UI package:

- **Contracts** — the TypeScript mirror of the Files contract: `FileModel`, `FilePage`, `FileReference`,
  `SignedFileUrl`, the audit trail's types, `FilesRouteConstants` and `FilesScopeConstants`.
- **Icons** — an icon for any file extension, from the be registry; usable without a Files instance.

```bash
pnpm add @wow-two-beta/fe-files @wow-two-beta/fe vue
```

```ts
import { FileIcon, fileTypeOf } from '@wow-two-beta/fe-files/icons';
import { FilesRouteConstants, type FileModel } from '@wow-two-beta/fe-files/contracts';
```

| Export | Holds |
|---|---|
| `./contracts` | `FileKind`, `FileModel`, `FilePage`, `FileReference`, `SignedFileUrl`, `FileAuditEntryModel`, `FilesRouteConstants`, `FilesScopeConstants` |
| `./icons` | `FileIcon`, `renderFileIconSvg`, `renderFileKindIconSvg`, `fileTypeOf`, `fileKind`, `extensionOf`, `fileTypes` |

`<FileIcon :name | :ext | :mime :size :monochrome>` draws a page in the kind's colour, the kind's glyph and, from 24 px,
the extension label; recolour a kind with `--files-icon-{kind}`. The type table is generated from the be registry
(`pnpm gen:file-types`), and a test fails while the two differ. `renderFileIconSvg` returns the same drawing as
standalone SVG markup; a Files service serves it for every extension at `/icons/{ext}.svg`. The upload kit, previews and share UI join as `./kit`.
