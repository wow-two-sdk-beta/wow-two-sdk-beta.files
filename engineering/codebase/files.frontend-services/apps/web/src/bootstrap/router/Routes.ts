import { createAppRouter, lazyRoute, RouterHistoryMode, type RouteConfig } from '@wow-two-beta/fe/router';

/** Holds the route table: the console at the root and the icon page; hosted share and request pages join here. */
export const routes: RouteConfig = [
  {
    path: '/',
    id: 'console',
    handle: { title: 'Console' },
    lazy: lazyRoute(() => import('../../presentation/console/pages/ConsolePage.vue')),
  },
  {
    path: '/icons',
    id: 'icons',
    handle: { title: 'File icons' },
    lazy: lazyRoute(() => import('../../presentation/icons/pages/IconsPage.vue')),
  },
];

/** Creates the app's router over `history`: the browser's in the app, memory in tests. */
export function createWebRouter(history: RouterHistoryMode = RouterHistoryMode.Browser) {
  return createAppRouter(routes, { titleSuffix: 'Files', routePersistence: false, history });
}
