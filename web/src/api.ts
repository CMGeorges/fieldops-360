import type { CreateWorkOrder, WorkOrder } from "./types";

const API_URL = import.meta.env.VITE_API_URL ?? "";
export const DEMO_CONTRACTOR_ID = "33333333-3333-3333-3333-333333333333";

type ApiOptions = {
  method?: "GET" | "POST";
  body?: unknown;
  actorId?: string;
};

async function request<T>(path: string, options: ApiOptions = {}): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    method: options.method ?? "GET",
    headers: {
      "Content-Type": "application/json",
      "Idempotency-Key": crypto.randomUUID(),
      ...(options.actorId ? { "X-Actor-Id": options.actorId } : {})
    },
    body: options.body ? JSON.stringify(options.body) : undefined
  });

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
    throw new Error(problem?.detail ?? `La requête a échoué (${response.status}).`);
  }

  return response.json() as Promise<T>;
}

export const fieldOpsApi = {
  list: () => request<WorkOrder[]>("/api/work-orders/"),
  create: (body: CreateWorkOrder) => request<WorkOrder>("/api/work-orders/", { method: "POST", body }),
  triage: (id: string) => request<WorkOrder>(`/api/work-orders/${id}/triage`, { method: "POST", body: { priority: "P1", slaHours: 4 } }),
  offer: (id: string) => request<WorkOrder>(`/api/work-orders/${id}/offer`, { method: "POST", body: { contractorId: DEMO_CONTRACTOR_ID, responseMinutes: 60 } }),
  accept: (id: string) => request<WorkOrder>(`/api/work-orders/${id}/accept`, { method: "POST", actorId: DEMO_CONTRACTOR_ID }),
  schedule: (id: string) => request<WorkOrder>(`/api/work-orders/${id}/schedule`, {
    method: "POST",
    actorId: DEMO_CONTRACTOR_ID,
    body: { scheduledForUtc: new Date(Date.now() + 60 * 60 * 1000).toISOString() }
  }),
  start: (id: string) => request<WorkOrder>(`/api/work-orders/${id}/start`, { method: "POST", actorId: DEMO_CONTRACTOR_ID }),
  review: (id: string) => request<WorkOrder>(`/api/work-orders/${id}/review`, { method: "POST", actorId: DEMO_CONTRACTOR_ID }),
  close: (id: string) => request<WorkOrder>(`/api/work-orders/${id}/close`, { method: "POST" })
};

