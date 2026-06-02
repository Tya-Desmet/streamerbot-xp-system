// Déclenche le téléchargement d'un objet en fichier JSON (côté navigateur).
// Sert à l'admin local : on édite en localStorage puis on exporte le fichier
// à recopier dans content/.
export function downloadJson(filename: string, data: unknown) {
  const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}
