import { ScreenHeader } from "../components/ScreenHeader";
import { DESTINATIONS, MATERIALS, MENU_LABEL } from "../data";
import type { DemoState, DestinationId, MenuId } from "../types";

const PINS: Record<DestinationId, { x: string; y: string }> = {
  piso: { x: "38%", y: "34%" },
  sala: { x: "58%", y: "62%" },
  exterior: { x: "82%", y: "22%" },
};

export function Move({
  demo,
  setDemo,
}: {
  demo: DemoState;
  setDemo: (patch: (demo: DemoState) => DemoState) => void;
}) {
  function toggleMenu() {
    setDemo((current) => {
      if (current.menuOpen) return { ...current, menuOpen: false };
      return {
        ...current,
        menuOpen: true,
        menu: current.menu === "measure" ? "main" : current.menu,
      };
    });
  }

  function openMenu(menu: MenuId) {
    setDemo((current) => ({ ...current, menu, menuOpen: true }));
  }

  function chooseDestination(destination: DestinationId) {
    setDemo((current) => ({ ...current, destination, menu: "teleport", menuOpen: true }));
  }

  return (
    <section className="move">
      <ScreenHeader
        kicker="04  ·  Moverse"
        title="El menú queda delante de la vista."
        lede="Se abre con la X del control izquierdo, a 1.5 m y medio metro bajo los ojos. Desde ahí se salta, se cambia la luz o se mide."
      />
      <div className="move-grid">
        <article className="panel-card">
          <header className="card-title">
            <h2>Menú de mano</h2>
            <button type="button" className="text-button" onClick={toggleMenu} aria-pressed={demo.menuOpen}>
              {demo.menuOpen ? "Cerrar menú" : "Botón de menú"}
            </button>
          </header>
          <div className="menu-stage" data-open={demo.menuOpen}>
            <div className="viewer" aria-hidden="true">
              <span />
              Vista
            </div>
            <div className="dim-col" aria-hidden="true">
              <span>1.5 m</span>
              <i />
              <span>−0.5 m</span>
            </div>
            {demo.menuOpen ? (
              <div className="vr-panel" aria-label={`Menú ${MENU_LABEL[demo.menu]}`}>
                <p className="vr-kicker">{MENU_LABEL[demo.menu]}</p>
                <MenuBody demo={demo} openMenu={openMenu} chooseDestination={chooseDestination} setDemo={setDemo} />
              </div>
            ) : (
              <p className="menu-off">
                {demo.menu === "measure"
                  ? "Modo medición. El menú se ocultó para que el gatillo no lo pulse."
                  : "Menú oculto. El botón del control lo vuelve a colocar delante."}
              </p>
            )}
          </div>
        </article>

        <div className="move-side">
          <article className="panel-card">
            <header className="card-title">
              <h2>Tres destinos</h2>
              <p>{demo.destination ? DESTINATIONS.find((item) => item.id === demo.destination)?.name : "Sin salto"}</p>
            </header>
            <div className="plan">
              <svg viewBox="0 0 320 210" aria-hidden="true">
                <rect className="plot" x="8" y="8" width="304" height="194" />
                <text className="plot-label" x="214" y="28">
                  Exterior
                </text>
                <rect className="foot" x="70" y="48" width="168" height="140" />
                <text className="plot-label dark" x="128" y="132">
                  Sala
                </text>
                <rect className="loft" x="82" y="60" width="78" height="52" />
                <text className="plot-label dark" x="92" y="90">
                  Piso 2
                </text>
              </svg>
              {demo.destination && (
                <span className="pin" style={{ left: PINS[demo.destination].x, top: PINS[demo.destination].y }} />
              )}
              <div className="plan-actions">
                {DESTINATIONS.map((destination) => (
                  <button
                    key={destination.id}
                    type="button"
                    className="chip-button"
                    aria-pressed={demo.destination === destination.id}
                    onClick={() => chooseDestination(destination.id)}
                  >
                    {destination.num} {destination.name}
                  </button>
                ))}
              </div>
            </div>
            <p className="fine">
              El origen XR mueve la cámara al punto y alinea el arriba con el adelante de ese destino.
            </p>
          </article>

          <article className="panel-card stairs">
            <header className="card-title">
              <h2>Escaleras</h2>
              <div className="seg">
                <button type="button" aria-pressed={demo.stairs === "box"} onClick={() => setDemo((current) => ({ ...current, stairs: "box" }))}>
                  Caja
                </button>
                <button type="button" aria-pressed={demo.stairs === "mesh"} onClick={() => setDemo((current) => ({ ...current, stairs: "mesh" }))}>
                  Malla
                </button>
              </div>
            </header>
            <img
              className="stair-photo"
              src="/fotos/fachada.jpg"
              alt="Escalera exterior de El proyecto, del estacionamiento a la planta alta"
            />
            <StairFigure mode={demo.stairs} />
            <p className="fine">
              {demo.stairs === "box"
                ? "La caja cubre el tramo entero. El controlador la lee como un muro más alto que el step offset."
                : "En SampleScene la caja se quita, entra la malla de los peldaños y la gravedad queda encendida para bajar."}
            </p>
          </article>
        </div>
      </div>
      <p className="source">HandMenuController · VRMenuController · TeleportMenu · StairWalkSurface</p>
    </section>
  );
}

function MenuBody({
  demo,
  openMenu,
  chooseDestination,
  setDemo,
}: {
  demo: DemoState;
  openMenu: (menu: MenuId) => void;
  chooseDestination: (destination: DestinationId) => void;
  setDemo: (patch: (demo: DemoState) => DemoState) => void;
}) {
  if (demo.menu === "teleport") {
    return (
      <div className="vr-list">
        {DESTINATIONS.map((destination) => (
          <button key={destination.id} type="button" onClick={() => chooseDestination(destination.id)} aria-pressed={demo.destination === destination.id}>
            {destination.num}. {destination.name}
          </button>
        ))}
        <button type="button" className="back" onClick={() => openMenu("main")}>
          Volver
        </button>
      </div>
    );
  }

  if (demo.menu === "lights") {
    return (
      <div className="vr-list">
        <button type="button" aria-pressed={!demo.night} onClick={() => setDemo((current) => ({ ...current, night: false }))}>
          Día
        </button>
        <button type="button" aria-pressed={demo.night} onClick={() => setDemo((current) => ({ ...current, night: true }))}>
          Noche
        </button>
        <button type="button" className="back" onClick={() => openMenu("main")}>
          Volver
        </button>
      </div>
    );
  }

  if (demo.menu === "walls") {
    return (
      <div className="vr-list walls">
        {MATERIALS.map((material) => (
          <button
            key={material.id}
            type="button"
            aria-pressed={demo.materialId === material.id}
            onClick={() => setDemo((current) => ({ ...current, materialId: material.id }))}
          >
            <i style={{ background: material.color }} />
            {material.label}
          </button>
        ))}
        <button type="button" className="back" onClick={() => openMenu("main")}>
          Volver
        </button>
      </div>
    );
  }

  return (
    <div className="vr-list">
      <button type="button" onClick={() => openMenu("teleport")}>
        Teletransporte
      </button>
      <button type="button" onClick={() => openMenu("lights")}>
        Luces
      </button>
      <button type="button" onClick={() => openMenu("walls")}>
        Paredes
      </button>
      <button
        type="button"
        onClick={() => setDemo((current) => ({ ...current, menu: "measure", menuOpen: false }))}
      >
        Medida
      </button>
    </div>
  );
}

function StairFigure({ mode }: { mode: DemoState["stairs"] }) {
  return (
    <svg className="stair-svg" viewBox="0 0 280 120" role="img" aria-label={mode === "box" ? "Tramo bloqueado por una caja" : "Peldaños con malla caminable"}>
      <line className="stair-ground" x1="8" y1="108" x2="272" y2="108" />
      {mode === "box" ? (
        <polygon className="stair-box" points="28,108 196,108 196,28" />
      ) : (
        <polyline className="stair-mesh" points="28,108 58,108 58,88 88,88 88,68 118,68 118,48 148,48 148,28 196,28" />
      )}
      <circle className="walker" cx={mode === "box" ? 22 : 118} cy={mode === "box" ? 96 : 58} r="7" />
    </svg>
  );
}
