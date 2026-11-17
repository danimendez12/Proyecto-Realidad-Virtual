import { HouseElevation } from "../components/HouseElevation";
import { ScreenHeader } from "../components/ScreenHeader";
import { FLOORS } from "../data";
import type { DemoState } from "../types";

export function House({
  demo,
  setFloor,
}: {
  demo: DemoState;
  setFloor: (floor: DemoState["floor"]) => void;
}) {
  const floor = FLOORS[demo.floor];

  return (
    <section className="split">
      <div className="split-copy">
        <ScreenHeader
          kicker="02  ·  La casa"
          title="Dos plantas y el exterior."
          lede="Casa 1 es el recorrido. Elige una zona del corte para ver qué vive ahí."
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
      <HouseElevation selected={demo.floor} onSelect={setFloor} />
    </section>
  );
}
