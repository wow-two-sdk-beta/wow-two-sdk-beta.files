<script setup lang="ts">
import { computed } from 'vue';
import {
  fileIconFold,
  fileIconGlyphTransform,
  fileIconLabelLength,
  fileIconLabelSize,
  fileIconPage,
} from './FileIconDrawing';
import { fileKindVisuals } from './FileKindVisuals';
import { fileTypeOf } from './FileTypes';

/**
 * Renders a file's icon: a page in its kind's colour, the kind's glyph and, from 24 px, the extension label. Any
 * extension gets one; an unknown one shows the generic glyph with its own label.
 */
defineOptions({ name: 'FileIcon' });

const props = withDefaults(
  defineProps<{
    /** The file name; its extension picks the type. */
    name?: string | null;
    /** The extension, when there is no name. */
    ext?: string | null;
    /** The content type, when there is neither. */
    mime?: string | null;
    /** The edge in pixels: 16, 20, 24, 32, 48 or 64 fit the drawing best. */
    size?: number;
    /** Draws in `currentColor` only, for dense lists. */
    monochrome?: boolean;
  }>(),
  { name: null, ext: null, mime: null, size: 32, monochrome: false },
);

const type = computed(() => fileTypeOf({ name: props.name, ext: props.ext, mime: props.mime }));
const color = computed(() =>
  props.monochrome ? 'currentColor' : `var(--files-icon-${type.value.kind}, ${fileKindVisuals[type.value.kind].color})`,
);
const glyph = computed(() => fileKindVisuals[type.value.kind].glyph);
const showsLabel = computed(() => props.size >= fileIconLabelSize && type.value.label.length > 0);
const glyphTransform = computed(() => fileIconGlyphTransform(showsLabel.value));
const fitsLabel = computed(() => fileIconLabelLength(type.value.label));
const title = computed(() => (type.value.label ? `${type.value.label} file` : 'File'));
const dataAttributes = computed<Record<string, string>>(() => ({ 'data-kind': type.value.kind }));
</script>

<template>
  <svg
    :width="size"
    :height="size"
    viewBox="0 0 32 32"
    role="img"
    :aria-label="title"
    v-bind="dataAttributes"
    xmlns="http://www.w3.org/2000/svg"
  >
    <path
      :d="fileIconPage"
      :fill="color"
      fill-opacity="0.1"
      :stroke="color"
      stroke-width="1.25"
      stroke-linejoin="round"
    />
    <path :d="fileIconFold" fill="none" :stroke="color" stroke-width="1.25" stroke-linejoin="round" />
    <path
      :d="glyph"
      :transform="glyphTransform"
      fill="none"
      :stroke="color"
      stroke-width="2"
      stroke-linecap="round"
      stroke-linejoin="round"
    />
    <g v-if="showsLabel">
      <rect x="3" y="19.5" width="26" height="8" rx="1.5" :fill="color" />
      <text
        x="16"
        y="25.6"
        text-anchor="middle"
        font-family="ui-sans-serif, system-ui, sans-serif"
        font-size="6.5"
        font-weight="700"
        :fill="monochrome ? 'var(--files-icon-label, #fff)' : '#fff'"
        :textLength="fitsLabel"
        :lengthAdjust="fitsLabel ? 'spacingAndGlyphs' : undefined"
      >
        {{ type.label }}
      </text>
    </g>
  </svg>
</template>
