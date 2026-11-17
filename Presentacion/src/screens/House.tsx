import { ScreenHeader } from "../components/ScreenHeader";
import { FLOORS, VIEWS } from "../data";
import type { DemoState, FloorId } from "../types";

const ORDER: FloorId[] = ["baja", "alta", "exterior"];

export function House({
  demo,
  setFloor,
}: {
  demo: DemoState;
  setFloor: (floor: DemoState["floor"]) => void;
}) {
  const floor = FLOORS[demo.floor];
  const view = VIEWS[demo.floor];

  return (
    <section className="split">
      <div className="split-copy">
        <ScreenHeader
          kicker="02  ·  La casa"
          title="Dos plantas y el exterior."
          lede="El proyecto es el recorrido. Elige una zona y mira la foto de la escena."
        />
        <article className="detail">
          <p className="detail-place">{floor.place}</p>
          <h2>{floor.name}</h2>
          <p>{floor.body}</p>
          <ul>
            {floor.facts.map((fact) => (
              <li key={fact}>{fact}</li>
            ))}
          </ul>
        </article>
        <p className="source">Casa1 · Sala · SalaUp · Exterior</p>
      </div>
      <figure className="shot">
        <div className="shot-frame">
          <img
            key={view.src}
            className={demo.floor === "exterior" ? "wide" : undefined}
            src={view.src}
            alt={view.alt}
          />
          <figcaption>{view.caption}</figcaption>
        </div>
        <div className="shot-bar">
          {ORDER.map((id) => (
            <button
              key={id}
              type="button"
              className="chip-button"
              aria-pressed={demo.floor === id}
              onClick={() => setFloor(id)}
            >
              {FLOORS[id].name}
            </button>
          ))}
        </div>
      </figure>
    </section>
  );
}
