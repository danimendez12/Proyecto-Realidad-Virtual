export type FloorId = "baja" | "alta" | "exterior";

export type MenuId = "main" | "teleport" | "lights" | "walls" | "measure";

export type DestinationId = "piso" | "sala" | "exterior";

export type StairMode = "box" | "mesh";

export type MeasurePoint = {
  x: number;
  y: number;
};

export type DemoState = {
  floor: FloorId;
  menuOpen: boolean;
  menu: MenuId;
  destination: DestinationId | null;
  stairs: StairMode;
  door: number;
  windowOpen: number;
  lightsOn: boolean;
  night: boolean;
  materialId: string | null;
  pointA: MeasurePoint | null;
  pointB: MeasurePoint | null;
};

export const initialDemo: DemoState = {
  floor: "baja",
  menuOpen: true,
  menu: "main",
  destination: null,
  stairs: "box",
  door: 0,
  windowOpen: 0,
  lightsOn: true,
  night: false,
  materialId: null,
  pointA: null,
  pointB: null,
};
