// Copie les JSON exportés par le bot (exports/) vers public/data/.
// Lancé automatiquement avant `dev` et `build` (scripts predev/prebuild).
// Source : variable d'environnement EXPORT_DIR, sinon ../exports.
import fs from 'fs';
import path from 'path';

const SRC = process.env.EXPORT_DIR || path.join(process.cwd(), '..', 'exports');
const DEST = path.join(process.cwd(), 'public', 'data');

// Vide DEST (sauf .gitkeep) pour refléter exactement la source :
// un profil supprimé côté bot ne laisse pas de fichier fantôme.
function clean(dir) {
  if (!fs.existsSync(dir)) return;
  for (const entry of fs.readdirSync(dir)) {
    if (entry === '.gitkeep') continue;
    fs.rmSync(path.join(dir, entry), { recursive: true, force: true });
  }
}

function copyRecursive(src, dest) {
  fs.mkdirSync(dest, { recursive: true });
  for (const entry of fs.readdirSync(src, { withFileTypes: true })) {
    const s = path.join(src, entry.name);
    const d = path.join(dest, entry.name);
    if (entry.isDirectory()) copyRecursive(s, d);
    else if (entry.name.endsWith('.json')) fs.copyFileSync(s, d);
  }
}

fs.mkdirSync(DEST, { recursive: true });
clean(DEST);

if (!fs.existsSync(SRC)) {
  console.warn('[copy-exports] source absente: ' + SRC);
  console.warn('[copy-exports] public/data vidé — le site buildera avec des données vides.');
} else {
  copyRecursive(SRC, DEST);
  console.log('[copy-exports] ' + SRC + ' -> ' + DEST);
}
