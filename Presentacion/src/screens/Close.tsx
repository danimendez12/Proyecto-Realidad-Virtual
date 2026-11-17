import { ScreenHeader } from "../components/ScreenHeader";
import { QUESTIONS, STACK } from "../data";

export function Close({ go }: { go: (index: number) => void }) {
  return (
    <section className="close">
      <ScreenHeader
        kicker="07  ·  Cierre"
        title="Para responder, se vuelve a la pantalla."
        lede="Cada pregunta salta al demo que ya vimos. El visor sigue siendo SampleScene; esta sala solo lo cuenta."
      />
      <div className="close-grid">
        <ol className="questions">
          {QUESTIONS.map((item) => (
            <li key={item.q}>
              <button type="button" onClick={() => go(item.screen)}>
                <strong>{item.q}</strong>
                <span>{item.detail}</span>
              </button>
            </li>
          ))}
        </ol>
        <aside className="stack">
          <h2>En el proyecto</h2>
          <ul className="stack-list">
            {STACK.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
          <p className="fine">
            Scripts propios en Assets/Scripts. Las muestras de XR Interaction Toolkit y XR Hands no forman parte del relato.
          </p>
        </aside>
      </div>
      <p className="source">Teclas 1–7 en cualquier momento</p>
    </section>
  );
}
