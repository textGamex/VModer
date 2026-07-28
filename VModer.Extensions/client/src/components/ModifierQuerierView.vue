<template>
  <div style="display: flex; flex-direction: column; height: 100vh; overflow: hidden">
    <div style="display: flex; margin-top: 8px">
      <vscode-textfield
        @keyup.enter="filterList"
        v-model.trim="searchText"
        :placeholder="i18n.searchPlaceholder"
      ></vscode-textfield>
      <vscode-button style="margin-left: 4px" @click="filterList">{{
        i18n.searchButton
      }}</vscode-button>
      <label style="margin: 0 4px; font-size: medium" for="typeSelect"
        >{{ i18n.categories }}:</label
      >
      <vscode-multi-select ref="categoriesSelection" id="typeSelect" @change="filterList">
        <vscode-option v-for="category in modiiferCategories" :key="category" :value="category">
          {{ category }}
        </vscode-option>
      </vscode-multi-select>
      <label style="font-size: medium; margin-left: 4px; text-align: center;">Count: {{ modifierList.length }}</label>
    </div>
    <vscode-table
      style="flex-grow: 1; margin-top: 4px; min-height: 0"
      zebra
      bordered-rows
      resizable
    >
      <vscode-table-header slot="header">
        <vscode-table-header-cell>{{ i18n.localizedName }}</vscode-table-header-cell>
        <vscode-table-header-cell>{{ i18n.name }}</vscode-table-header-cell>
        <vscode-table-header-cell>{{ i18n.categories }}</vscode-table-header-cell>
      </vscode-table-header>
      <vscode-table-body slot="body">
        <vscode-table-row v-for="(item, index) in modifierList" :key="index">
          <vscode-table-cell v-html="marked.parseInline(item.LocalizedName)"></vscode-table-cell>
          <vscode-table-cell>{{ item.Name }}</vscode-table-cell>
          <vscode-table-cell>{{ item.Categories.join(", ") }}</vscode-table-cell>
        </vscode-table-row>
      </vscode-table-body>
    </vscode-table>
  </div>
</template>

<script lang="ts" setup>
import { WebviewApi } from "@tomjs/vscode-webview";
import { onMounted, onUnmounted, ref } from "vue";
import type { ModifierDto } from "../dto/ModifierDto";
import { marked } from "marked";
import type { VscodeMultiSelect } from "@vscode-elements/elements";
import type { ModifierQuerierViewI18n } from "../types/ModifierQuerierViewI18";
import { initMarked } from "../helpers/markdownHelper";
import { flatten, uniq } from 'es-toolkit';

initMarked();

const modifierListKey = "modifierList";
const i18nKey = "i18n";

const modifierList = ref<ModifierDto[]>([]);
let rawModifierList: ModifierDto[] = [];
const searchText = ref<string>("");
const modiiferCategories = ref<string[]>([]);
const vscode = new WebviewApi();
const categoriesSelection = ref<VscodeMultiSelect | null>(null);
const i18n = ref<ModifierQuerierViewI18n>({
  searchPlaceholder: "本地化名称 | 名称",
  searchButton: "搜索",
  categories: "类别:",
  name: "名称",
  localizedName: "本地化名称",
});

onMounted(() => {
  vscode.postMessage("init_complete");
});

vscode.on<ModifierDto[]>(modifierListKey, (data) => {
  rawModifierList = data;
  modifierList.value = data;
  modiiferCategories.value = uniq(flatten(data.map((item) => item.Categories)));
});

vscode.on<ModifierQuerierViewI18n>(i18nKey, (data) => {
  i18n.value = data;
});

onUnmounted(() => {
  vscode.off(modifierListKey);
  vscode.off(i18nKey);
});

function filterList() {
  const selectedCategories = new Set(categoriesSelection.value?.value || []);
  if (searchText.value === "" && selectedCategories.size === 0) {
    modifierList.value = rawModifierList;
    return;
  }
  const search = searchText.value.toLowerCase();
  modifierList.value = rawModifierList.filter((item) => {
    return (
      (item.LocalizedName.toLowerCase().includes(search) ||
        item.Name.toLowerCase().includes(search)) &&
      (selectedCategories.size === 0 ||
        item.Categories.some((category) => selectedCategories.has(category)))
    );
  });
}
</script>
