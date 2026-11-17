import { useState } from "react";
import { ScreenHeader } from "../components/ScreenHeader";

const CONTROLS = [
  {
    id: "x",
    name: "X",
    where: "Control izquierdo · botón principal",
    body: "Abre y cierra el menú de mano. En el asset de entrada esa acción se llama Open Menu y está en el Primary Button izquierdo.",
    uses: ["El panel queda a 1.5 m, medio metro bajo la vista", "Volver a pulsar X lo esconde"],
  },
  {
    id: "gatillo",
    name: "Gatillo",
    where: "Dedo índice · las dos manos",
    body: "Confirma lo que el rayo está apuntando. En la cinta, el gatillo derecho marca los puntos.",
    uses: [
      "En el menú, elige Teletransporte, Luces, Paredes o Medida",
      "Cinta: primer toque, punto A",
      "Segundo toque, punto B",
      "Tercer toque, borra y empieza otra",
    ],
  },
  {
    id: "grip",
    name: "Grip",
    where: "Botón lateral · las dos manos",
    body: "Es el Select del interactor. Se aprieta cerca de la manija y la mano mueve la pieza.",
    uses: [
      "Puerta: agarra la manija y gira hasta 95°",
      "Ventana: agarra la hoja y la desliza",
      "Interruptor: el mismo Select cambia la luz del cuarto",
    ],
  },
  {
    id: "menu",
    name: "Menú",
    where: "Botones del panel · se confirman con el gatillo",
    body: "Cuatro entradas. Cada una abre lo que ya se recorre en las pantallas siguientes.",
    uses: [
      "Teletransporte: Segundo piso, Sala o Exterior",
      "Luces: día o noche, en 4 segundos",
      "Paredes: uno de los diez materiales",
      "Medida: esconde el menú y deja el rayo de la cinta",
    ],
  },
] as const;

type ControlId = (typeof CONTROLS)[number]["id"];

export function Controls() {
  const [id, setId] = useState<ControlId>("x");
  const control = CONTROLS.find((item) => item.id === id) ?? CONTROLS[0];

  return (
    <section className="controls">
      <figure className="control-photo">
        <img
          src="/fotos/controles.png"
          alt="Controles de Meta Quest: el izquierdo de perfil, con grip y gatillo, y el derecho de frente, con A, B y palanca"
        />
        <figcaption>Controles del visor</figcaption>
      </figure>
      <div className="control-copy">
        <ScreenHeader
          kicker="03  ·  Controles"
          title="Tres botones y un menú."
          lede="Casa 1 usa la X, el gatillo y el grip. El resto del control no tiene un script propio."
        />
        <div className="shot-bar" role="group" aria-label="Botones">
          {CONTROLS.map((item) => (
            <button
              key={item.id}
              type="button"
              className="chip-button"
              aria-pressed={item.id === id}
              onClick={() => setId(item.id)}
            >
              {item.name}
            </button>
          ))}
        </div>
        <article className="detail">
          <p className="detail-place">{control.where}</p>
          <h2>{control.name}</h2>
          <p>{control.body}</p>
          <ul>
            {control.uses.map((use) => (
              <li key={use}>{use}</li>
            ))}
          </ul>
        </article>
        <p className="fine">
          A, B, la palanca y el botón de sistema no disparan funciones de Casa 1. La salida de la cinta quedó sin botón.
        </p>
      </div>
    </section>
  );
}
