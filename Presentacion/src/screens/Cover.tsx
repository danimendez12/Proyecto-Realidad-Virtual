import { HouseElevation } from "../components/HouseElevation";

export function Cover({ onEnter }: { onEnter: () => void }) {
  return (
    <section className="cover">
      <div className="cover-copy">
        <p className="kicker">Proyecto de realidad virtual</p>
        <h1>
          Una casa que se puede <em>recorrer</em>.
        </h1>
        <p className="lede">
          Casa 1 se camina, se abre y se cambia de acabado antes de construirla.
          Esta presentación enseña lo que ya hace el visor.
        </p>
        <ul className="chips">
          <li>Unity 6</li>
          <li>URP</li>
          <li>OpenXR</li>
          <li>Meta</li>
          <li>Android XR</li>
        </ul>
        <button type="button" className="enter" onClick={onEnter}>
          Entrar a la casa
        </button>
        <p className="source">SampleScene · Unity 6000.5.8f1</p>
      </div>
      <HouseElevation selected={null} glow />
    </section>
  );
}
