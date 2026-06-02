/** @type {import('next').NextConfig} */
module.exports = {
  // Export statique : génère un dossier out/ déployable sur tout hébergeur statique.
  output: 'export',
  // Pas d'optimisation d'images serveur (incompatible avec l'export statique).
  images: { unoptimized: true },
  // URLs avec slash final → compatibilité maximale des hébergeurs statiques.
  trailingSlash: true,
};
