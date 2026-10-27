import { mount } from '@vue/test-utils';
import { afterEach, describe, expect, it } from 'vitest';
import {
  FileIcon,
  extensionOf,
  fileKind,
  fileTypeOf,
  fileTypes,
  renderFileIconSvg,
  renderFileKindIconSvg,
} from '../src/icons';

const wrappers: ReturnType<typeof mount>[] = [];

afterEach(() => {
  for (const wrapper of wrappers.splice(0)) wrapper.unmount();
});

function render(props: InstanceType<typeof FileIcon>['$props']) {
  const wrapper = mount(FileIcon, { props });
  wrappers.push(wrapper);
  return wrapper;
}

describe('file types', () => {
  it('resolves a type by name, extension and content type', () => {
    expect(fileTypeOf({ name: 'Report.PDF' })).toMatchObject({ ext: 'pdf', kind: 'pdf', mime: 'application/pdf' });
    expect(fileTypeOf({ ext: '.glb' }).kind).toBe('model-3d');
    expect(fileTypeOf({ mime: 'image/jpeg; q=1' }).ext).toBe('jpg');
    expect(fileKind({ name: 'archive.tar.gz' })).toBe('archive');
  });

  it('keeps an unknown extension as a generic type with its own label', () => {
    expect(fileTypeOf({ name: 'model.xyz' })).toEqual({
      ext: 'xyz',
      kind: 'generic',
      label: 'XYZ',
      mime: 'application/octet-stream',
    });
    expect(fileTypeOf({}).label).toBe('');
  });

  it('reads the extension from the last path part only', () => {
    expect(extensionOf('dir.v2/README')).toBe('');
    expect(extensionOf('C:\\docs\\a.TXT')).toBe('txt');
    expect(fileTypes().length).toBeGreaterThan(300);
  });
});

describe('file icon', () => {
  it('draws the kind and labels it from 24 px', () => {
    const wrapper = render({ name: 'clip.mp4', size: 32 });

    expect(wrapper.get('svg').attributes()).toMatchObject({ 'data-kind': 'video', 'aria-label': 'MP4 file' });
    expect(wrapper.get('text').text()).toBe('MP4');
  });

  it('drops the label below 24 px', () => {
    expect(render({ ext: 'docx', size: 16 }).find('text').exists()).toBe(false);
  });

  it('fits a long label into the band', () => {
    expect(render({ ext: 'sqlite3' }).get('text').attributes('textLength')).toBe('23');
  });

  it('draws in currentColor when monochrome', () => {
    const wrapper = render({ ext: 'png', monochrome: true });

    expect(wrapper.findAll('path').every((path) => path.attributes('stroke') === 'currentColor')).toBe(true);
  });
});

describe('standalone icon', () => {
  const parse = (markup: string) => new DOMParser().parseFromString(markup, 'image/svg+xml').documentElement;

  it('draws what the component draws', () => {
    const component = render({ name: 'clip.mp4', size: 32 }).get('svg').element;
    const markup = parse(renderFileIconSvg({ name: 'clip.mp4' }));

    // The component wraps each colour in its kind's custom property; the standalone markup writes the colour itself.
    const colour = (value: string | null) => value?.replace(/^var\(--files-icon-[\w-]+, (.+)\)$/, '$1');
    const shapes = (svg: Element) =>
      [...svg.querySelectorAll('path, rect, text')].map((node) => [
        node.getAttribute('d'),
        node.getAttribute('transform'),
        colour(node.getAttribute('fill')),
        colour(node.getAttribute('stroke')),
        node.textContent?.trim(),
      ]);
    expect(shapes(markup)).toEqual(shapes(component));
    expect(markup.getAttribute('xmlns')).toBe('http://www.w3.org/2000/svg');
  });

  it('labels from 24 px unless told otherwise', () => {
    expect(parse(renderFileIconSvg({ ext: 'docx' }, { size: 16 })).querySelector('text')).toBeNull();
    expect(
      parse(renderFileIconSvg({ ext: 'docx' }, { size: 16, label: true })).querySelector('text')?.textContent,
    ).toBe('DOCX');
    expect(
      parse(renderFileIconSvg({ ext: 'sqlite3' }))
        .querySelector('text')
        ?.getAttribute('textLength'),
    ).toBe('23');
  });

  it('draws a kind without a label', () => {
    const markup = parse(renderFileKindIconSvg('spreadsheet', 20));

    expect(markup.getAttribute('width')).toBe('20');
    expect(markup.querySelector('text')).toBeNull();
    expect(markup.querySelector('path')?.getAttribute('stroke')).toBe('#16a34a');
  });

  it('escapes what it writes', () => {
    expect(renderFileIconSvg({ ext: 'a<b' })).toContain('A&lt;B');
  });
});
