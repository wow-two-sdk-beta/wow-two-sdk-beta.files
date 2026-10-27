<script setup lang="ts">
import { Badge, Card } from '@wow-two-beta/fe/presentation/display';
import { FileIcon } from '@wow-two-beta/fe-files/icons';
import { RouterLink } from 'vue-router';
import { useSystemStatus } from '../../../application/system/UseSystemStatus';

/** Renders the console's start: the running versions and the way to the file type registry. */
defineOptions({ name: 'ConsolePage' });

const appVersion = __APP_VERSION__;
const { status } = useSystemStatus();
</script>

<template>
  <main class="mx-auto min-h-screen max-w-2xl px-6 py-16 font-sans text-foreground">
    <header class="flex flex-wrap items-center gap-3">
      <h1 class="text-3xl font-semibold">Files console</h1>
      <div class="contents" aria-label="App version">
        <Badge>app {{ appVersion }}</Badge>
      </div>
      <div v-if="status" class="contents" aria-label="API version">
        <Badge>api {{ status.version }}</Badge>
      </div>
    </header>

    <section class="mt-8" aria-labelledby="types-heading">
      <Card class="flex items-center gap-4 p-6">
        <FileIcon ext="pdf" :size="48" />
        <div>
          <h2 id="types-heading" class="text-sm font-medium text-muted-foreground">File types</h2>
          <RouterLink to="/icons" class="mt-1 inline-block font-medium underline underline-offset-4">
            Every type Files recognizes, with its icon
          </RouterLink>
        </div>
      </Card>
    </section>
  </main>
</template>
