import { useEffect, useState } from "react";
import { SCREENS } from "./data";
import { Close } from "./screens/Close";
import { Controls } from "./screens/Controls";
import { Cover } from "./screens/Cover";
import { Decide } from "./screens/Decide";
import { House } from "./screens/House";
import { Move } from "./screens/Move";
import { Touch } from "./screens/Touch";
import { initialDemo, type DemoState } from "./types";

export function App() {
  const [index, setIndex] = useState(0);
  const [notes, setNotes] = useState(false);
  const [demo, setDemo] = useState<DemoState>(initialDemo);
  const screen = SCREENS[index];

  useEffect(() => {
    function onKey(event: KeyboardEvent) {
      if (event.repeat || event.metaKey || event.ctrlKey || event.altKey) return;
      const target = event.target instanceof Element ? event.target : null;
      if (target?.closest("[data-keys='local']")) return;

      if (event.key === "ArrowRight" || event.key === "ArrowDown") {
        event.preventDefault();
        setIndex((current) => Math.min(SCREENS.length - 1, current + 1));
      } else if (event.key === "ArrowLeft" || event.key === "ArrowUp") {
        event.preventDefault();
        setIndex((current) => Math.max(0, current - 1));
      } else if (event.key === "n" || event.key === "N") {
        event.preventDefault();
        setNotes((current) => !current);
      } else if (event.key >= "1" && event.key <= "9") {
        const next = Number(event.key) - 1;
        if (next < SCREENS.length) {
          event.preventDefault();
          setIndex(next);
        }
      } else if (event.key === "Escape") {
        event.preventDefault();
        setIndex(0);
      }
    }

    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  useEffect(() => {
    document.title = `${screen.label} — El proyecto`;
  }, [screen.label]);

  return (
    <div className="shell">
      <nav className="rail" aria-label="Pantallas">
        <p className="brand">El proyecto</p>
        {SCREENS.map((item, itemIndex) => (
          <button
            key={item.id}
            type="button"
            aria-current={itemIndex === index ? "page" : undefined}
            onClick={() => setIndex(itemIndex)}
          >
            <span className="num">{item.num}</span>
            <span className="rail-label">{item.label}</span>
          </button>
        ))}
      </nav>

      <div className="column">
        <main className="main" data-screen={screen.id}>
          <p className="sr" aria-live="polite">
            {screen.num} {screen.label}
          </p>
          {index === 0 && <Cover onEnter={() => setIndex(1)} />}
          {index === 1 && (
            <House demo={demo} setFloor={(floor) => setDemo((current) => ({ ...current, floor }))} />
          )}
          {index === 2 && <Controls />}
          {index === 3 && <Move demo={demo} setDemo={setDemo} />}
          {index === 4 && <Touch demo={demo} setDemo={setDemo} />}
          {index === 5 && <Decide demo={demo} setDemo={setDemo} />}
          {index === 6 && <Close go={setIndex} />}
        </main>
        <footer className="footer">
          <p>
            <kbd>←</kbd> <kbd>→</kbd> pantallas
            <span className="dot">·</span>
            <kbd>1</kbd>–<kbd>{SCREENS.length}</kbd> saltar
            <span className="dot">·</span>
            <kbd>N</kbd> {notes ? "ocultar notas" : "notas"}
          </p>
          <p>
            {screen.num} / {String(SCREENS.length).padStart(2, "0")}
          </p>
        </footer>
        <div className="progress" aria-hidden="true">
          <span style={{ width: `${((index + 1) / SCREENS.length) * 100}%` }} />
        </div>
      </div>

      {notes && (
        <aside className="notes" aria-label="Nota del presentador">
          <strong>Nota</strong>
          <p>{screen.note}</p>
        </aside>
      )}
    </div>
  );
}
