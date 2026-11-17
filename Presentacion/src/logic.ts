import type { MeasurePoint } from "./types";

export const DOOR_MAX = 95;
export const DOOR_AUTOCLOSE = 3;
export const SASH_WIDTH = 0.65;
export const SASH_CLEARANCE = 0.077;
export const WALL_WIDTH = 4;
export const WALL_HEIGHT = 2.7;
export const DAY_SUN = 0.5;
export const NIGHT_SUN = 0.28;
export const DAY_FOG = 0.05;
export const NIGHT_FOG = 0.012;
export const TRANSITION_MS = 4000;

export function clamp(value: number, min: number, max: number) {
  return Math.min(max, Math.max(min, value));
}

export function smoothstep(value: number) {
  const t = clamp(value, 0, 1);
  return t * t * (3 - 2 * t);
}

/** Suelta la puerta: por debajo de 3° vuelve a cerrarse. */
export function releaseDoor(angle: number) {
  const next = clamp(angle, 0, DOOR_MAX);
  return next < DOOR_AUTOCLOSE ? 0 : next;
}

/** Recorrido de la hoja: ancho menos holgura, con un mínimo de 5 cm. */
export function sashTravel(width = SASH_WIDTH, clearance = SASH_CLEARANCE) {
  return Math.max(0.05, width - clearance);
}

export function measure(a: MeasurePoint, b: MeasurePoint) {
  const horizontal = Math.abs(b.x - a.x);
  const height = Math.abs(b.y - a.y);
  return {
    total: Math.hypot(horizontal, height),
    horizontal,
    height,
  };
}

export function formatMeters(value: number) {
  return `${value.toFixed(2)} m`;
}

export function lerp(from: number, to: number, t: number) {
  return from + (to - from) * t;
}

export function inkOn(hex: string) {
  const r = Number.parseInt(hex.slice(1, 3), 16);
  const g = Number.parseInt(hex.slice(3, 5), 16);
  const b = Number.parseInt(hex.slice(5, 7), 16);
  const luminance = (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255;
  return luminance > 0.62 ? "#1a1814" : "#f4efe6";
}
