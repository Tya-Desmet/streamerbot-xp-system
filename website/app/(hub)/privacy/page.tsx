import { getSocials } from '@/lib/content';
import { buildMetadata } from '@/lib/seo';

export const metadata = buildMetadata({
  title: 'Confidentialité',
  description: 'Données affichées, confidentialité, cookies et retrait (opt-out).',
  path: '/privacy/',
});

export default function PrivacyPage() {
  const socials = getSocials();

  return (
    <main className="section">
      <div className="wrap" style={{ maxWidth: 760 }}>
        <div className="eyebrow">Confidentialité · プライバシー</div>
        <h1>Confidentialité</h1>

        <div className="dim" style={{ display: 'flex', flexDirection: 'column', gap: 20, marginTop: 22, lineHeight: 1.75 }}>
          <section>
            <h3>Quelles données sont affichées ?</h3>
            <p>
              Le hub affiche des données publiques liées à ta participation au chat Twitch :
              <b> pseudo Twitch, niveau, XP, watchtime, rang</b>. Aucune donnée privée (email,
              adresse, etc.) n&apos;est collectée ni affichée.
            </p>
          </section>

          <section>
            <h3>Pourquoi&nbsp;?</h3>
            <p>
              Pour animer la communauté (classement, profils). L&apos;XP est calculé par le bot du
              stream (Streamer.bot) à partir de l&apos;activité <b>publique</b> du chat.
            </p>
          </section>

          <section>
            <h3>Cookies &amp; pistage</h3>
            <p>
              Aucun cookie de pistage, aucune publicité, aucun analytics tiers. Quelques préférences
              (ex.&nbsp;«&nbsp;trouve-toi&nbsp;», thème, son) sont stockées <b>localement</b> dans ton
              navigateur (localStorage) et ne sont jamais envoyées.
            </p>
          </section>

          <section>
            <h3>Retrait (opt-out)</h3>
            <p>
              Tu ne veux pas apparaître&nbsp;? Demande le retrait&nbsp;: ton pseudo sera <b>exclu</b> de
              l&apos;export du bot et disparaîtra du site (classement et profil).
              {socials.discord ? (
                <>
                  {' '}Contact&nbsp;:{' '}
                  <a href={socials.discord} target="_blank" rel="noopener noreferrer" style={{ textDecoration: 'underline' }}>
                    Discord
                  </a>.
                </>
              ) : (
                ' Contacte le streamer pour la demande.'
              )}
            </p>
          </section>

          <section>
            <h3>Hébergement</h3>
            <p>Le site et ses données sont hébergés chez Infomaniak (Suisse / UE).</p>
          </section>
        </div>
      </div>
    </main>
  );
}
