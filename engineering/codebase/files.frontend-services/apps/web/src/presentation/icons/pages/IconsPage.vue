<script setup lang="ts">
import { computed, ref } from 'vue';
import { FileIcon, fileTypes, type FileKind, type FileTypeInfo } from '@wow-two-beta/fe-files/icons';

/** Renders every registry type with its icon, grouped by kind, with a filter by extension or content type. */
defineOptions({ name: 'IconsPage' });

const sizes = [16, 20, 24, 32, 48, 64];
const filter = ref('');

const groups = computed(() => {
  const term = filter.value.trim().toLowerCase().replace(/^\./, '');
  const byKind = new Map<FileKind, FileTypeInfo[]>();
  for (const type of fileTypes()) {
    if (term && !type.ext.includes(term) && !type.mime.includes(term)) continue;
    byKind.set(type.kind, [...(byKind.get(type.kind) ?? []), type]);
  }
  return [...byKind.entries()].map(([kind, types]) => ({ kind, types }));
});
</script>

<template>
  <main class="mx-auto min-h-screen max-w-5xl px-6 py-12 font-sans text-foreground">
    <header class="flex flex-wrap items-end justify-between gap-4">
      <div>
        <h1 class="text-3xl font-semibold">File icons</h1>
        <p class="mt-1 text-sm text-muted-foreground">{{ fileTypes().length }} types from the Files registry</p>
      </div>
      <label class="text-sm">
        <span class="sr-only">Filter by extension or content type</span>
        <input
          v-model="filter"
          name="filter"
          type="search"
          placeholder="Filter: pdf, image/…"
          class="w-64 rounded-md border border-border bg-background px-3 py-2"
        />
      </label>
    </header>

    <section class="mt-8 flex items-end gap-6" aria-label="Sizes">
      <figure v-for="size in sizes" :key="size" class="flex flex-col items-center gap-1">
        <FileIcon ext="pdf" :size="size" />
        <figcaption class="text-xs text-muted-foreground">{{ size }}</figcaption>
      </figure>
    </section>

    <section v-for="group in groups" :key="group.kind" class="mt-10" :aria-labelledby="`kind-${group.kind}`">
      <h2 :id="`kind-${group.kind}`" class="text-sm font-medium text-muted-foreground">
        {{ group.kind }} · {{ group.types.length }}
      </h2>
      <ul class="mt-3 grid grid-cols-[repeat(auto-fill,minmax(5.5rem,1fr))] gap-3">
        <li
          v-for="type in group.types"
          :key="type.ext"
          :title="type.mime"
          class="flex flex-col items-center gap-1 rounded-md p-2 hover:bg-muted"
        >
          <FileIcon :ext="type.ext" :size="48" />
          <span class="text-xs">.{{ type.ext }}</span>
        </li>
      </ul>
    </section>

    <p v-if="groups.length === 0" class="mt-10 text-sm text-muted-foreground">No type matches “{{ filter }}”.</p>
  </main>
</template>
