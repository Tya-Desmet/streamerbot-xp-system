'use client';

import { useEffect, useRef, useState } from 'react';

// Reveal-on-scroll : ajoute la classe `in` (cf. .reveal/.reveal.in dans globals.css)
// quand l'élément entre dans le viewport. Animation transform/opacity (GPU).
export default function Reveal({
  children,
  className = '',
}: {
  children: React.ReactNode;
  className?: string;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const [shown, setShown] = useState(false);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const io = new IntersectionObserver(
      (entries) => {
        for (const e of entries) {
          if (e.isIntersecting) {
            setShown(true);
            io.disconnect();
          }
        }
      },
      { threshold: 0.12 },
    );
    io.observe(el);
    return () => io.disconnect();
  }, []);

  return (
    <div ref={ref} className={`reveal ${shown ? 'in' : ''} ${className}`.trim()}>
      {children}
    </div>
  );
}
