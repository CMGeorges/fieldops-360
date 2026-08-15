import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { fieldOpsApi } from "./api";
import type { CreateWorkOrder, WorkOrder } from "./types";

const STATUS_LABELS: Record<string, string> = {
  Submitted: "À qualifier",
  Triaged: "Prêt à assigner",
  Offered: "Offre envoyée",
  Accepted: "Accepté",
  Scheduled: "Planifié",
  InProgress: "En intervention",
  ReadyForReview: "À valider",
  Closed: "Fermé",
  NoResponse: "Sans réponse",
  NoShow: "Absent"
};

const NEXT_ACTION: Record<string, { label: string; action: keyof typeof fieldOpsApi }> = {
  Submitted: { label: "Qualifier P1", action: "triage" },
  Triaged: { label: "Offrir au contracteur", action: "offer" },
  Offered: { label: "Accepter l'offre", action: "accept" },
  Accepted: { label: "Planifier +1 h", action: "schedule" },
  Scheduled: { label: "Démarrer", action: "start" },
  InProgress: { label: "Soumettre pour validation", action: "review" },
  ReadyForReview: { label: "Fermer le dossier", action: "close" }
};

function formatDate(value: string | null) {
  if (!value) return "—";
  return new Intl.DateTimeFormat("fr-CA", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

function StatusBadge({ status }: { status: string }) {
  return <span className={`status status--${status.toLowerCase()}`}>{STATUS_LABELS[status] ?? status}</span>;
}

function Metric({ value, label, accent }: { value: number; label: string; accent?: boolean }) {
  return (
    <article className={`metric${accent ? " metric--accent" : ""}`}>
      <strong>{value.toString().padStart(2, "0")}</strong>
      <span>{label}</span>
    </article>
  );
}

function WorkOrderCard({ order, selected, onSelect }: { order: WorkOrder; selected: boolean; onSelect: (order: WorkOrder) => void }) {
  return (
    <button className={`work-card${selected ? " work-card--selected" : ""}`} onClick={() => onSelect(order)} type="button">
      <div className="work-card__top">
        <span className={`priority priority--${order.priority.toLowerCase()}`}>{order.priority}</span>
        <StatusBadge status={order.status} />
      </div>
      <strong>{order.title}</strong>
      <p>{order.description}</p>
      <div className="work-card__meta">
        <span>#{order.id.slice(-4).toUpperCase()}</span>
        <span>v{order.version}</span>
      </div>
    </button>
  );
}

function DetailPanel({ order, busy, onAdvance }: { order: WorkOrder | null; busy: boolean; onAdvance: (order: WorkOrder) => void }) {
  if (!order) {
    return <aside className="detail detail--empty">Sélectionnez un ordre de travail pour voir sa chronologie.</aside>;
  }

  const next = NEXT_ACTION[order.status];
  return (
    <aside className="detail">
      <div className="detail__heading">
        <div>
          <span className="eyebrow">Dossier #{order.id.slice(-4).toUpperCase()}</span>
          <h2>{order.title}</h2>
        </div>
        <StatusBadge status={order.status} />
      </div>

      <dl className="facts">
        <div><dt>Priorité</dt><dd>{order.priority}</dd></div>
        <div><dt>SLA</dt><dd>{formatDate(order.slaDueAtUtc)}</dd></div>
        <div><dt>Rendez-vous</dt><dd>{formatDate(order.scheduledForUtc)}</dd></div>
        <div><dt>Version</dt><dd>{order.version}</dd></div>
      </dl>

      {next ? (
        <button className="primary-action" disabled={busy} onClick={() => onAdvance(order)} type="button">
          {busy ? "Traitement…" : next.label}
        </button>
      ) : (
        <div className="resolved">Cycle terminé — preuve et historique conservés.</div>
      )}

      <div className="timeline-heading">
        <h3>Journal d'audit</h3>
        <span>{order.timeline.length} événements</span>
      </div>
      <ol className="timeline">
        {[...order.timeline].reverse().map((event) => (
          <li key={`${event.version}-${event.type}`}>
            <span className="timeline__dot" />
            <div>
              <strong>{event.type}</strong>
              <span>{event.from} → {event.to}</span>
              <time>{formatDate(event.occurredAtUtc)}</time>
            </div>
          </li>
        ))}
      </ol>
    </aside>
  );
}

function CreatePanel({ open, onClose, onCreate }: { open: boolean; onClose: () => void; onCreate: (request: CreateWorkOrder) => Promise<void> }) {
  const [saving, setSaving] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setSaving(true);
    try {
      await onCreate({
        title: String(form.get("title")),
        description: String(form.get("description")),
        priority: String(form.get("priority")) as CreateWorkOrder["priority"]
      });
      event.currentTarget.reset();
      onClose();
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className={`modal${open ? " modal--open" : ""}`} aria-hidden={!open}>
      <button className="modal__backdrop" type="button" aria-label="Fermer" onClick={onClose} />
      <section className="modal__panel" role="dialog" aria-modal="true" aria-labelledby="create-title">
        <span className="eyebrow">Nouvelle demande</span>
        <h2 id="create-title">Créer un ordre de travail</h2>
        <form onSubmit={submit}>
          <label>Titre<input name="title" required maxLength={160} placeholder="Ex. Fuite sous l'évier" /></label>
          <label>Description<textarea name="description" required maxLength={4000} rows={4} placeholder="Décrivez le problème et son emplacement." /></label>
          <label>Priorité<select name="priority" defaultValue="P2"><option>P1</option><option>P2</option><option>P3</option><option>P4</option></select></label>
          <div className="modal__actions">
            <button className="secondary-action" type="button" onClick={onClose}>Annuler</button>
            <button className="primary-action" disabled={saving} type="submit">{saving ? "Création…" : "Créer"}</button>
          </div>
        </form>
      </section>
    </div>
  );
}

export default function App() {
  const [orders, setOrders] = useState<WorkOrder[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [filter, setFilter] = useState("All");
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const result = await fieldOpsApi.list();
      setOrders(result);
      setSelectedId((current) => current ?? result[0]?.id ?? null);
      setError(null);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Impossible de joindre l'API.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  const selected = orders.find((item) => item.id === selectedId) ?? null;
  const filtered = filter === "All" ? orders : orders.filter((item) => item.status === filter);
  const metrics = useMemo(() => ({
    critical: orders.filter((item) => item.priority === "P1" && item.status !== "Closed").length,
    active: orders.filter((item) => ["Accepted", "Scheduled", "InProgress", "ReadyForReview"].includes(item.status)).length,
    exceptions: orders.filter((item) => ["NoResponse", "NoShow"].includes(item.status)).length,
    closed: orders.filter((item) => item.status === "Closed").length
  }), [orders]);

  async function advance(order: WorkOrder) {
    const next = NEXT_ACTION[order.status];
    if (!next) return;
    setBusy(true);
    try {
      const action = fieldOpsApi[next.action] as (id: string) => Promise<WorkOrder>;
      const updated = await action(order.id);
      setOrders((current) => current.map((item) => item.id === updated.id ? updated : item));
      setError(null);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "L'action a échoué.");
    } finally {
      setBusy(false);
    }
  }

  async function create(request: CreateWorkOrder) {
    const created = await fieldOpsApi.create(request);
    setOrders((current) => [created, ...current]);
    setSelectedId(created.id);
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <a className="brand" href="#top" aria-label="FieldOps 360 accueil"><span>F360</span><div><strong>FieldOps 360</strong><small>Operations control</small></div></a>
        <div className="topbar__right"><span className="live"><i /> Démo active</span><button className="primary-action" onClick={() => setCreateOpen(true)} type="button">+ Nouvelle demande</button></div>
      </header>

      <main id="top">
        <section className="hero">
          <div><span className="eyebrow">Pilotage multi-entreprises</span><h1>Chaque intervention.<br /><em>Sous contrôle.</em></h1><p>Responsabilité, SLA et preuve — du signalement jusqu'à la fermeture.</p></div>
          <div className="metrics"><Metric value={metrics.critical} label="Critiques" accent /><Metric value={metrics.active} label="Actives" /><Metric value={metrics.exceptions} label="Exceptions" /><Metric value={metrics.closed} label="Fermées" /></div>
        </section>

        {error ? <div className="error-banner" role="alert"><span>{error}</span><button type="button" onClick={() => void load()}>Réessayer</button></div> : null}

        <section className="workspace">
          <div className="worklist">
            <div className="section-heading"><div><span className="eyebrow">File opérationnelle</span><h2>Ordres de travail</h2></div><select aria-label="Filtrer par statut" value={filter} onChange={(event) => setFilter(event.target.value)}><option value="All">Tous les statuts</option>{Object.entries(STATUS_LABELS).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></div>
            <div className="cards">{loading ? <div className="loading">Chargement de la file…</div> : filtered.map((order) => <WorkOrderCard key={order.id} order={order} selected={order.id === selectedId} onSelect={(item) => setSelectedId(item.id)} />)}</div>
          </div>
          <DetailPanel order={selected} busy={busy} onAdvance={advance} />
        </section>
      </main>

      <CreatePanel open={createOpen} onClose={() => setCreateOpen(false)} onCreate={create} />
    </div>
  );
}

