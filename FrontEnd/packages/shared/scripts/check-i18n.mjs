// Kiểm tra 4 file locale có cùng bộ key (vi là chuẩn). Thiếu/thừa key → exit 1.
// Chạy: npm run i18n:check -w @fishlover/shared   (Node 22.18+ tự bỏ type khi import .ts)
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const dir = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', 'src', 'i18n', 'locales');
const LANGS = ['vi', 'en', 'de', 'zh'];

const flatten = (obj, prefix = '') =>
  Object.entries(obj).flatMap(([k, v]) =>
    v !== null && typeof v === 'object' ? flatten(v, `${prefix}${k}.`) : [`${prefix}${k}`],
  );

const keys = {};
for (const lang of LANGS) {
  const mod = await import(`file://${path.join(dir, `${lang}.ts`).replace(/\\/g, '/')}`);
  keys[lang] = new Set(flatten(mod.default));
}

let failed = false;
for (const lang of LANGS.slice(1)) {
  const missing = [...keys.vi].filter((k) => !keys[lang].has(k));
  const extra = [...keys[lang]].filter((k) => !keys.vi.has(k));
  if (missing.length || extra.length) {
    failed = true;
    console.error(`✗ ${lang}: thiếu ${missing.length}, thừa ${extra.length}`);
    for (const k of missing.slice(0, 20)) console.error(`    thiếu  ${k}`);
    for (const k of extra.slice(0, 20)) console.error(`    thừa   ${k}`);
  } else {
    console.log(`✓ ${lang}: đủ ${keys[lang].size} key`);
  }
}
process.exit(failed ? 1 : 0);
