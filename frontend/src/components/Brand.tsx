export function Brand({ compact = false }: { compact?: boolean }) {
  return <a className={`brand ${compact ? 'brand-compact' : ''}`} href="/" aria-label="AlmacenCloud, inicio">
    <img src={compact ? '/favicon.svg' : '/logo.svg'} alt="" aria-hidden="true" />
    {compact && <span><strong>AlmacenCloud</strong><small>Gestión inteligente de inventarios</small></span>}
  </a>
}
