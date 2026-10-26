# T7 — File icon pack

*Last updated: 2026-10-26*

> An icon for every file extension, from one registry shared by the be and fe packages: a small set of hand-drawn
> kind glyphs, a generated page per extension with its label and colour, and hand-tuned icons only where a format
> earns one.

- `(U)` = unverified; licences of any outside set are checked before reuse.

---

## 1 · Registry

One JSON source, `file-types.json`, in the files repo; a build step generates the be `Files.Formats` tables and the fe
icon map from it.

| Field | Example | Used by |
|---|---|---|
| `ext` | `docx` | both |
| `mime` | `application/vnd.openxmlformats-officedocument.wordprocessingml.document` | both |
| `kind` | `document` | icon glyph, colour, policy groups |
| `label` | `DOCX` | the icon's text band |
| `magic` | `50 4B 03 04` + `word/` entry | be sniffing |
| `preview` | `pdf-render` | which previewer opens it |
| `risk` | `active` · `executable` · `none` | forced download, scan priority |

- seeded from the `mime-db` extension list (MIT (U)), plus curated rows for dev, 3D, CAD and design formats it lacks.
- one table means the be and fe never disagree on what `.heic` or `.glb` is.

---

## 2 · Kinds

About 24 glyphs, each with a colour token:

| Group | Kinds |
|---|---|
| media | image · raw photo · vector · video · audio |
| documents | pdf · document · spreadsheet · presentation · text · markdown · ebook |
| data and code | code · data (JSON, XML, YAML, CSV) · database · config |
| packages | archive · disk image · executable / installer |
| design and 3D | font · 3D model · CAD · design file |
| other | calendar / contact · certificate / key · generic |

- folders: folder, shared folder, locked folder, file request.
- state badges: uploading, scanning, rejected, locked (password), expiring.

---

## 3 · Rendering

- one SVG page template (folded corner) + the kind glyph + the extension label + the kind colour → every extension in
  the registry has an icon with no hand-drawn file.
- hand-tuned icons for the most common formats (PDF, DOCX, XLSX, PPTX, JPG, PNG, MP4, ZIP, …) in P2.
- sizes 16, 20, 24, 32, 48, 64; the label hides below 24.
- light and dark through CSS tokens; a monochrome `currentColor` mode for dense lists.
- no brand logos (Photoshop, Figma, Sketch) in P1: trademark risk; the label carries the format. A brand pack is P3.

---

## 4 · Delivery

- fe: `<FileIcon :name | :ext | :mime size>`, `fileKind()`, an SVG sprite, all under `@wow-two-beta/fe-files/icons`.
- be: `FileKinds` and the raw SVGs in `Files.Formats`, for emails, share pages rendered server-side and archive listings.
- the console's file browser, share pages and every product's attachment lists use the same set.

---

## 5 · Benchmarks

- VS Code icon themes (vscode-icons, Material Icon Theme) and file-icon-vectors are references for coverage and
  naming; licences and asset terms to verify before reusing any glyph (U).
- the generated approach keeps the set ours, so no outside licence binds the published package.
