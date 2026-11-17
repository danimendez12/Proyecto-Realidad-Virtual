import { useEffect, useRef, useState } from "react";
import { ScreenHeader } from "../components/ScreenHeader";
import { MATERIALS, ORIGINAL_WALL } from "../data";
import {
  DAY_FOG,
  DAY_SUN,
  NIGHT_FOG,
  NIGHT_SUN,
  TRANSITION_MS,
  WALL_HEIGHT,
  WALL_WIDTH,
  formatMeters,
  inkOn,
  lerp,
  measure,
  smoothstep,
} from "../logic";
import type { DemoState, MeasurePoint } from "../types";

export function Decide({
  demo,
  setDemo,
}: {
  demo: DemoState;
  setDemo: (patch: (demo: DemoState) => DemoState) => void;
}) {
  const material = MATERIALS.find((item) => item.id === demo.materialId);
  const wallColor = material?.color ?? ORIGINAL_WALL.color;
  const wallInk = inkOn(wallColor);

  return (
    <section className="decide">
      <ScreenHeader
        kicker="05  ·  Decidir"
        title="Luz, acabado y medida."
        lede="El día cambia en cuatro segundos, la pared toma uno de los diez materiales y la cinta marca dos puntos."
      />
      <div className="decide-body">
        <Sky
          night={demo.night}
          onChange={(night) => setDemo((current) => ({ ...current, night }))}
        />
        <p className="fine sky-note">
          4 segundos con SmoothStep. El sol baja de 0.50 a 0.28, la niebla de 0.050 a 0.012 y el relleno exterior se apaga. Postes y lámparas siguen.
        </p>
        <div className="decide-bottom">
          <article className="panel-card">
            <header className="card-title">
              <h2>Paredes</h2>
              <p>{material?.label ?? ORIGINAL_WALL.label}</p>
            </header>
            <div className="wall-stage" style={{ background: wallColor, color: wallInk }}>
              <strong>{material?.label ?? "Acabado al arrancar"}</strong>
              <span>{material ? material.asset : "El que tenía cada pared"}</span>
            </div>
            <div className="swatches">
              <button
                type="button"
                className="swatch"
                style={{ background: ORIGINAL_WALL.color }}
                aria-pressed={demo.materialId === null}
                aria-label="Restaurar el material original"
                onClick={() => setDemo((current) => ({ ...current, materialId: null }))}
              />
              {MATERIALS.map((item) => (
                <button
                  key={item.id}
                  type="button"
                  className="swatch"
                  style={{ background: item.color }}
                  aria-pressed={demo.materialId === item.id}
                  aria-label={item.label}
                  title={item.label}
                  onClick={() => setDemo((current) => ({ ...current, materialId: item.id }))}
                />
              ))}
            </div>
            <p className="fine">Los colores son el _BaseColor de cada material en MaterialesParedes.</p>
          </article>

          <Measure
            pointA={demo.pointA}
            pointB={demo.pointB}
            setDemo={setDemo}
          />
        </div>
      </div>
      <p className="source">DayNightController · WallCustomizationController · MeasuringTape</p>
    </section>
  );
}

function Sky({ night, onChange }: { night: boolean; onChange: (night: boolean) => void }) {
  const [time, setTime] = useState(night ? 1 : 0);
  const [replay, setReplay] = useState(0);
  const valueRef = useRef(night ? 1 : 0);

  useEffect(() => {
    const to = night ? 1 : 0;
    if (replay === 0) {
      valueRef.current = to;
      setTime(to);
      return;
    }

    const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const from = Math.abs(valueRef.current - to) < 0.02 ? 1 - to : valueRef.current;
    if (reduce) {
      valueRef.current = to;
      setTime(to);
      return;
    }

    const start = performance.now();
    let frame = 0;
    const tick = (now: number) => {
      const progress = Math.min(1, (now - start) / TRANSITION_MS);
      const next = from + (to - from) * smoothstep(progress);
      valueRef.current = next;
      setTime(next);
      if (progress < 1) frame = requestAnimationFrame(tick);
    };
    frame = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frame);
  }, [night, replay]);

  function choose(next: boolean) {
    onChange(next);
    setReplay((value) => value + 1);
  }

  const sun = lerp(DAY_SUN, NIGHT_SUN, time);
  const fog = lerp(DAY_FOG, NIGHT_FOG, time);
  const fill = lerp(1, 0, time);

  return (
    <div className="sky" data-night={time > 0.55}>
      <div className="sky-day" style={{ opacity: 1 - time }} />
      <div className="sky-night" style={{ opacity: time }} />
      <div
        className="sun"
        style={{
          opacity: Math.max(0, 1 - time * 1.4),
          transform: `translateY(${time * 48}px)`,
          background: mixSun(time),
        }}
      />
      <div className="moon" style={{ opacity: time }} />
      <div className="sil">
        <span className="glow-win" />
      </div>
      <div className="lamp" aria-hidden="true">
        <i />
      </div>
      <div className="fill-light" style={{ opacity: fill }} />
      <div className="sky-ui">
        <div className="seg">
          <button type="button" aria-pressed={!night} onClick={() => choose(false)}>
            Día
          </button>
          <button type="button" aria-pressed={night} onClick={() => choose(true)}>
            Noche
          </button>
        </div>
        <div className="metrics">
          <p className="metric">
            <b>{sun.toFixed(2)}</b>
            <span>Sol</span>
          </p>
          <p className="metric">
            <b>{fog.toFixed(3)}</b>
            <span>Niebla</span>
          </p>
          <p className="metric">
            <b>{Math.round(fill * 100)}%</b>
            <span>Relleno</span>
          </p>
        </div>
      </div>
    </div>
  );
}

function mixSun(time: number) {
  const r = Math.round(lerp(255, 140, time));
  const g = Math.round(lerp(244, 171, time));
  const b = Math.round(lerp(210, 255, time));
  return `rgb(${r} ${g} ${b})`;
}

function Measure({
  pointA,
  pointB,
  setDemo,
}: {
  pointA: MeasurePoint | null;
  pointB: MeasurePoint | null;
  setDemo: (patch: (demo: DemoState) => DemoState) => void;
}) {
  const [hover, setHover] = useState<MeasurePoint | null>(null);
  const live = pointA && !pointB && hover ? measure(pointA, hover) : null;
  const fixed = pointA && pointB ? measure(pointA, pointB) : null;
  const shown = fixed ?? live;

  function pointFromEvent(event: {
    clientX: number;
    clientY: number;
    currentTarget: SVGSVGElement;
  }): MeasurePoint {
    const bounds = event.currentTarget.getBoundingClientRect();
    const x = ((event.clientX - bounds.left) / bounds.width) * WALL_WIDTH;
    const y = (1 - (event.clientY - bounds.top) / bounds.height) * WALL_HEIGHT;
    return {
      x: Math.min(WALL_WIDTH, Math.max(0, x)),
      y: Math.min(WALL_HEIGHT, Math.max(0, y)),
    };
  }

  function onClick(event: { clientX: number; clientY: number; currentTarget: SVGSVGElement }) {
    const point = pointFromEvent(event);
    setDemo((current) => {
      if (current.pointA && current.pointB) return { ...current, pointA: null, pointB: null };
      if (!current.pointA) return { ...current, pointA: point, pointB: null };
      return { ...current, pointB: point };
    });
  }

  const end = pointB ?? (pointA ? hover : null);

  return (
    <article className="panel-card">
      <header className="card-title">
        <h2>Cinta métrica</h2>
        <p>{fixed ? "Fija" : pointA ? "En vivo" : "Sin puntos"}</p>
      </header>
      <svg
        className="tape"
        viewBox={`0 0 ${WALL_WIDTH * 100} ${WALL_HEIGHT * 100}`}
        onPointerMove={(event) => setHover(pointFromEvent(event))}
        onPointerLeave={() => setHover(null)}
        onClick={onClick}
        role="img"
        aria-label="Muro de ejemplo. Clic para marcar los dos puntos de la medición."
      >
        <rect className="wall-fill" width={WALL_WIDTH * 100} height={WALL_HEIGHT * 100} />
        <rect className="opening" x="70" y="60" width="90" height="210" />
        <rect className="opening" x="230" y="55" width="110" height="110" />
        {pointA && end && (
          <line
            className="m-line"
            x1={pointA.x * 100}
            y1={(WALL_HEIGHT - pointA.y) * 100}
            x2={end.x * 100}
            y2={(WALL_HEIGHT - end.y) * 100}
          />
        )}
        {pointA && <circle className="m-point" cx={pointA.x * 100} cy={(WALL_HEIGHT - pointA.y) * 100} r="5" />}
        {pointB && <circle className="m-point" cx={pointB.x * 100} cy={(WALL_HEIGHT - pointB.y) * 100} r="5" />}
        {hover && !pointB && (
          <circle className="mira" cx={hover.x * 100} cy={(WALL_HEIGHT - hover.y) * 100} r="7" />
        )}
      </svg>
      <p className="readout">
        <strong>{shown ? formatMeters(shown.total) : "—"}</strong>
        <span>
          {shown
            ? `horiz. ${shown.horizontal.toFixed(2)} · alto ${shown.height.toFixed(2)}`
            : "Primer clic marca A"}
        </span>
      </p>
      <p className="fine">
        {pointA && pointB
          ? "Otro clic borra y empieza de nuevo. En el visor el tercer gatillo hace eso."
          : "Muro de ejemplo, 4.00 × 2.70 m. El rayo del visor solo mide superficies con collider."}
      </p>
    </article>
  );
}
