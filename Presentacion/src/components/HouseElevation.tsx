import type { ReactNode } from "react";
import type { FloorId } from "../types";

export function HouseElevation({
  selected,
  onSelect,
  glow = false,
}: {
  selected: FloorId | null;
  onSelect?: (floor: FloorId) => void;
  glow?: boolean;
}) {
  return (
    <div className={glow ? "elev glow" : "elev"} data-floor={selected ?? "none"}>
      <div className="elev-sky">
        <Zone id="exterior" label="Exterior" onSelect={onSelect} selected={selected === "exterior"} />
      </div>
      <div className="elev-house">
        <div className="elev-roof" />
        <div className="elev-body">
          <Zone id="alta" label="SalaUp" onSelect={onSelect} selected={selected === "alta"}>
            <span className="openings">
              <span className="window" />
              <span className="window" />
            </span>
          </Zone>
          <div className="elev-cut" aria-hidden="true">
            <span>Y = 4 m</span>
          </div>
          <Zone id="baja" label="Sala" onSelect={onSelect} selected={selected === "baja"}>
            <span className="openings">
              <span className="door" />
              <span className="window low" />
            </span>
          </Zone>
        </div>
      </div>
      <div className="elev-ground" />
      <p className="elev-caption">Corte de Casa 1</p>
    </div>
  );
}

function Zone({
  id,
  label,
  selected,
  onSelect,
  children,
}: {
  id: FloorId;
  label: string;
  selected: boolean;
  onSelect?: (floor: FloorId) => void;
  children?: ReactNode;
}) {
  if (!onSelect) {
    return (
      <div className="zone" data-selected={selected}>
        <span className="zone-name">{label}</span>
        {children}
      </div>
    );
  }

  return (
    <button
      type="button"
      className="zone"
      data-selected={selected}
      aria-pressed={selected}
      onClick={() => onSelect(id)}
    >
      <span className="zone-name">{label}</span>
      {children}
    </button>
  );
}
