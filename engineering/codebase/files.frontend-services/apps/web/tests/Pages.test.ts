import { flushPromises, mount } from '@vue/test-utils';
import { RouterHistoryMode } from '@wow-two-beta/fe/router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { fileTypeRows } from '@wow-two-beta/fe-files/icons';
import AppRoot from '../src/bootstrap/AppRoot.vue';
import { createWebRouter } from '../src/bootstrap/router/Routes';

const wrappers: ReturnType<typeof mount>[] = [];

afterEach(() => {
  for (const wrapper of wrappers.splice(0)) wrapper.unmount();
  vi.unstubAllGlobals();
});

const status = { data: { service: 'files', status: 'ok', version: '0.1.4+abc1234' } };

/** Mounts the app at `path`, routed as in the browser but over memory history. */
async function renderAt(path: string) {
  const router = createWebRouter(RouterHistoryMode.Memory);
  await router.push(path);
  await router.isReady();
  const wrapper = mount(AppRoot, { global: { plugins: [router] } });
  wrappers.push(wrapper);
  await flushPromises();
  return wrapper;
}

describe('console route', () => {
  it('shows the running versions and the way to the file types', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => Response.json(status)),
    );
    const wrapper = await renderAt('/');

    expect(wrapper.get('[aria-label="App version"]').text()).toBe('app test');
    expect(wrapper.get('[aria-label="API version"]').text()).toBe('api 0.1.4+abc1234');
    expect(wrapper.get('a[href="/icons"]').text()).toContain('Every type');
  });
});

describe('icons route', () => {
  it('shows every registry extension with its icon', async () => {
    const wrapper = await renderAt('/icons');

    const tiles = wrapper.findAll('section[aria-labelledby] li');
    expect(tiles.map((tile) => tile.get('span').text())).toEqual(fileTypeRows.map(([ext]) => `.${ext}`));
    expect(tiles.every((tile) => tile.find('svg[role="img"]').exists())).toBe(true);
  });

  it('filters by extension', async () => {
    const wrapper = await renderAt('/icons');

    await wrapper.get('input[name="filter"]').setValue('.docx');

    expect(wrapper.findAll('section[aria-labelledby] li').map((tile) => tile.get('span').text())).toEqual(['.docx']);
  });
});
