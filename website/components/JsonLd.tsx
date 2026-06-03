// Rend un bloc de données structurées schema.org (JSON-LD).
export default function JsonLd({ data }: { data: Record<string, unknown> }) {
  return (
    <script
      type="application/ld+json"
      // contenu contrôlé (pas d'entrée utilisateur arbitraire) → sûr
      dangerouslySetInnerHTML={{ __html: JSON.stringify(data) }}
    />
  );
}
