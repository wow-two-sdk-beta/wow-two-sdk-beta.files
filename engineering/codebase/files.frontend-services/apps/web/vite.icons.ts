import { fileKindVisuals, fileTypes, renderFileIconSvg, renderFileKindIconSvg } from '@wow-two-beta/fe-files/icons';
import type { FileKind } from '@wow-two-beta/fe-files/contracts';
import type { Plugin } from 'vite';

/**
 * Returns every icon file the service serves from its web root: `icons/{ext}.svg` per registry extension, labelled,
 * and `icons/kinds/{kind}.svg` per kind, unlabelled, for small sizes. Files.Api answers any other `.svg` under
 * `/icons` with the generic kind.
 */
export function fileIconFiles(): Map<string, string> {
  const files = new Map<string, string>();
  for (const type of fileTypes()) files.set(`icons/${type.ext}.svg`, renderFileIconSvg({ ext: type.ext }));
  for (const kind of Object.keys(fileKindVisuals) as FileKind[])
    files.set(`icons/kinds/${kind}.svg`, renderFileKindIconSvg(kind));
  return files;
}

/** Ships the icon files in the bundle, so they land in the service's wwwroot, and serves them on the dev server. */
export function fileIcons(): Plugin {
  const files = fileIconFiles();
  return {
    name: 'files:icons',
    configureServer(server) {
      server.middlewares.use((request, response, next) => {
        const svg = files.get((request.url ?? '').split('?')[0]!.slice(1));
        if (!svg) return next();
        response.setHeader('Content-Type', 'image/svg+xml');
        response.end(svg);
      });
    },
    generateBundle() {
      for (const [fileName, source] of files) this.emitFile({ type: 'asset', fileName, source });
    },
  };
}
