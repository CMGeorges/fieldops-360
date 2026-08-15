export type WorkOrderEvent = {
  type: string;
  from: string;
  to: string;
  reason: string | null;
  actorId: string;
  occurredAtUtc: string;
  version: number;
};

export type WorkOrder = {
  id: string;
  organizationId: string;
  title: string;
  description: string;
  priority: "P1" | "P2" | "P3" | "P4";
  status: string;
  contractorId: string | null;
  offerExpiresAtUtc: string | null;
  scheduledForUtc: string | null;
  slaDueAtUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  version: number;
  timeline: WorkOrderEvent[];
};

export type CreateWorkOrder = Pick<WorkOrder, "title" | "description" | "priority">;

