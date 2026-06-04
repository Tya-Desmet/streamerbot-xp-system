// Affichage 404 réutilisable (présentatif, sans état). Utilise le design system du hub.
export default function NotFoundView() {
  return (
    <main className="section" style={{ minHeight: '68vh', display: 'flex', alignItems: 'center' }}>
      <div className="wrap" style={{ textAlign: 'center' }}>
        <div className="eyebrow" style={{ justifyContent: 'center' }}>
          Erreur 404 · 迷子
        </div>
        <h1 style={{ fontSize: 'clamp(64px, 14vw, 140px)', margin: '10px 0 0', lineHeight: 1 }}>
          <span className="text-grad">404</span>
        </h1>
        <h2 style={{ fontSize: 'clamp(22px, 4vw, 34px)', margin: '6px 0 14px' }}>Page introuvable</h2>
        <p className="dim" style={{ maxWidth: '46ch', margin: '0 auto 28px' }}>
          Cette page n&apos;existe pas, a été déplacée, ou n&apos;est pas activée sur ce site.
          Reviens en terrain connu :
        </p>
        <div className="flex center gap-m" style={{ justifyContent: 'center', flexWrap: 'wrap' }}>
          <a className="btn btn-primary" href="/">
            Retour à l&apos;accueil
          </a>
          <a className="btn btn-ghost" href="/leaderboard/">
            Voir le classement
          </a>
        </div>
      </div>
    </main>
  );
}
