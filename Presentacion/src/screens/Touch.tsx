import { useRef, useState, type PointerEvent as ReactPointerEvent } from "react";
import { ScreenHeader } from "../components/ScreenHeader";
import { DOOR_MAX, clamp, releaseDoor, sashTravel } from "../logic";
import type { DemoState } from "../types";

const TRAVEL = sashTravel();
const FULL_DRAG = 0.46;

export function Touch({
  demo,
  setDemo,
}: {
  demo: DemoState;
  setDemo: (patch: (demo: DemoState) => DemoState) => void;
}) {
  return (
    <section className="touch">
      <ScreenHeader
        kicker="04  ·  Tocar"
        title="La casa responde a la mano."
        lede="Puertas y ventanas se agarran con el Grip. El interruptor enciende solo las luces de su cuarto."
      />
      <div className="toy-grid">
        <Door angle={demo.door} setAngle={(door) => setDemo((current) => ({ ...current, door }))} />
        <Window open={demo.windowOpen} setOpen={(windowOpen) => setDemo((current) => ({ ...current, windowOpen }))} />
        <Switch lightsOn={demo.lightsOn} setDemo={setDemo} />
      </div>
      <p className="source">PuertaVR · HojaCorrediza · VentanaVRManos · Casa1LightSwitch</p>
    </section>
  );
}

function Door({ angle, setAngle }: { angle: number; setAngle: (angle: number) => void }) {
  const drag = useRef<{ x: number; angle: number } | null>(null);
  const angleRef = useRef(angle);
  angleRef.current = angle;
  const [hold, setHold] = useState<"idle" | "near" | "grab">("idle");
  const [glide, setGlide] = useState(true);

  function onDown(event: ReactPointerEvent<HTMLDivElement>) {
    event.currentTarget.setPointerCapture(event.pointerId);
    drag.current = { x: event.clientX, angle: angleRef.current };
    setGlide(false);
    setHold("grab");
  }

  function onMove(event: ReactPointerEvent<HTMLDivElement>) {
    if (!drag.current) return;
    const next = clamp(drag.current.angle + (event.clientX - drag.current.x) * 0.55, 0, DOOR_MAX);
    angleRef.current = next;
    setAngle(next);
  }

  function onUp() {
    if (!drag.current) return;
    drag.current = null;
    setGlide(true);
    setHold("idle");
    setAngle(releaseDoor(angleRef.current));
  }

  function nudge(delta: number) {
    const next = clamp(angleRef.current + delta, 0, DOOR_MAX);
    angleRef.current = next;
    setGlide(false);
    setAngle(next);
  }

  return (
    <article className="toy">
      <header>
        <h2>Puerta</h2>
        <p>Arrastra la hoja. Hasta 95°.</p>
      </header>
      <div
        className="door-stage"
        onPointerDown={onDown}
        onPointerMove={onMove}
        onPointerUp={onUp}
        onPointerCancel={onUp}
      >
        <div className="door-frame">
          <div className="door-leaf" data-glide={glide} style={{ transform: `rotateY(${-angle}deg)` }}>
            <span className="door-panel" />
            <span className="door-panel short" />
            <button
              type="button"
              className="handle"
              data-keys="local"
              data-hold={hold}
              aria-label="Manija de la puerta. Flechas para girarla."
              onKeyDown={(event) => {
                if (event.key === "ArrowRight" || event.key === "ArrowUp") {
                  event.preventDefault();
                  nudge(5);
                }
                if (event.key === "ArrowLeft" || event.key === "ArrowDown") {
                  event.preventDefault();
                  nudge(-5);
                }
              }}
              onKeyUp={(event) => {
                if (event.key.startsWith("Arrow")) {
                  setGlide(true);
                  setAngle(releaseDoor(angleRef.current));
                }
              }}
            />
          </div>
        </div>
      </div>
      <p className="readout">
        <strong>{Math.round(angle)}°</strong>
        <span>{hold === "grab" ? "Agarrada" : angle === 0 ? "Cerrada" : angle < DOOR_MAX ? "Abierta" : "Tope"}</span>
      </p>
      <p className="fine">Si sueltas por debajo de 3°, la hoja vuelve a cerrarse. Las dos caras tienen manija.</p>
    </article>
  );
}

function Window({ open, setOpen }: { open: number; setOpen: (open: number) => void }) {
  const drag = useRef<{ x: number; open: number; width: number } | null>(null);
  const openRef = useRef(open);
  openRef.current = open;
  const frameRef = useRef<HTMLDivElement>(null);

  function onDown(event: ReactPointerEvent<HTMLButtonElement>) {
    const bounds = frameRef.current?.getBoundingClientRect();
    event.currentTarget.setPointerCapture(event.pointerId);
    drag.current = { x: event.clientX, open: openRef.current, width: bounds?.width ?? 240 };
  }

  function onMove(event: ReactPointerEvent<HTMLButtonElement>) {
    if (!drag.current) return;
    const span = Math.max(80, drag.current.width * FULL_DRAG);
    const next = clamp(drag.current.open + (event.clientX - drag.current.x) / span, 0, 1);
    openRef.current = next;
    setOpen(next);
  }

  function onUp() {
    drag.current = null;
  }

  function step(delta: number) {
    const next = clamp(openRef.current + delta, 0, 1);
    openRef.current = next;
    setOpen(next);
  }

  return (
    <article className="toy">
      <header>
        <h2>Ventana</h2>
        <p>La hoja sigue la mano en su eje.</p>
      </header>
      <div className="win-stage" ref={frameRef}>
        <div className="win-frame">
          <div className="sash fixed" />
          <div className="sash moving" style={{ transform: `translateX(${open * 78}%)` }}>
            <button
              type="button"
              className="sash-handle"
              data-keys="local"
              aria-label="Manija de la ventana"
              onPointerDown={onDown}
              onPointerMove={onMove}
              onPointerUp={onUp}
              onPointerCancel={onUp}
              onKeyDown={(event) => {
                if (event.key === "ArrowRight" || event.key === "ArrowUp") {
                  event.preventDefault();
                  step(0.08);
                }
                if (event.key === "ArrowLeft" || event.key === "ArrowDown") {
                  event.preventDefault();
                  step(-0.08);
                }
              }}
            />
          </div>
        </div>
      </div>
      <p className="readout">
        <strong>{open.toFixed(2)}</strong>
        <span>{(open * TRAVEL).toFixed(3)} m de {TRAVEL.toFixed(3)} m</span>
      </p>
      <p className="fine">Ejemplo con una hoja de 0.65 m. El recorrido es el ancho menos 7.7 cm de holgura.</p>
    </article>
  );
}

function Switch({
  lightsOn,
  setDemo,
}: {
  lightsOn: boolean;
  setDemo: (patch: (demo: DemoState) => DemoState) => void;
}) {
  const last = useRef(0);

  function toggle() {
    const now = performance.now();
    if (now - last.current < 350) return;
    last.current = now;
    setDemo((current) => ({ ...current, lightsOn: !current.lightsOn }));
  }

  return (
    <article className="toy">
      <header>
        <h2>Interruptor</h2>
        <p>Un clic cambia solo este cuarto.</p>
      </header>
      <div className={lightsOn ? "room on" : "room"}>
        <div className="ceiling">
          <span className="cone" />
          <span className="cone" />
          <span className="cone" />
        </div>
        <button type="button" className="switch" onClick={toggle} aria-pressed={lightsOn} aria-label="Interruptor del cuarto">
          <span className="indicator" />
          <span className={lightsOn ? "lever on" : "lever"} />
        </button>
        <button type="button" className="switch peer" onClick={toggle} aria-pressed={lightsOn} aria-label="Interruptor parejo del mismo cuarto">
          <span className={lightsOn ? "lever on" : "lever"} />
        </button>
      </div>
      <p className="readout">
        <strong>{lightsOn ? "Encendida" : "Apagada"}</strong>
        <span>Palanca en 0.2 s</span>
      </p>
      <p className="fine">La pareja, a menos de 0.6 m, copia el estado. En planta baja el spot mira hacia abajo.</p>
    </article>
  );
}
