import Sakura from './Sakura';

export default function Hero({ streamerName, twitchUrl }: { streamerName: string; twitchUrl: string }) {
  const name = streamerName || 'Mystya';

  return (
    <section className="hero" style={{ position: 'relative', overflow: 'hidden', padding: '90px 0 70px' }}>
      <Sakura />
      <div className="wrap" style={{ position: 'relative', zIndex: 1, textAlign: 'center' }}>
        <div className="eyebrow" style={{ justifyContent: 'center' }}>
          {name} · ストリーム
        </div>
        <h1 style={{ fontSize: 'clamp(40px, 7vw, 86px)', margin: '18px 0 14px' }}>
          Bienvenue sur le <span className="text-grad">hub</span> de {name}
        </h1>
        <p className="dim" style={{ maxWidth: '52ch', margin: '0 auto 28px' }}>
          Classement XP de la communauté, planning des streams et ressources à télécharger —
          le tout au même endroit.
        </p>
        <div className="flex center gap-m" style={{ justifyContent: 'center', flexWrap: 'wrap' }}>
          <a className="btn btn-primary" href="/leaderboard/">
            Voir le classement
          </a>
          <a className="btn btn-ghost" href={twitchUrl} target="_blank" rel="noopener noreferrer">
            Regarder le live
          </a>
        </div>
      </div>
    </section>
  );
}
