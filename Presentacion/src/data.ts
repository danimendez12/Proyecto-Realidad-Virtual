import type { DestinationId, FloorId, MenuId } from "./types";

export const SCREENS = [
  {
    id: "portada",
    num: "01",
    label: "Portada",
    note: "Abre con la casa, no con la lista de paquetes. La escena que se juega es SampleScene.",
  },
  {
    id: "casa",
    num: "02",
    label: "La casa",
    note: "Las fotos son de la escena. La fachada incluye la escalera. Los dos interiores son la misma sala con distinto acabado; el corte de plantas sigue en Y = 4 m.",
  },
  {
    id: "controles",
    num: "03",
    label: "Controles",
    note: "X abre el menú: está en la cara del control izquierdo, junto a la Y. El gatillo confirma el panel y marca la cinta. El grip agarra puerta, ventana e interruptor. Y, las tres rayas, A, B y la palanca no tienen script propio.",
  },
  {
    id: "moverse",
    num: "04",
    label: "Moverse",
    note: "El menú queda fijo a 1.5 m y 0.5 m bajo la vista. Los saltos son Segundo piso, Sala y Exterior. La caja de la escalera se cambia por la malla.",
  },
  {
    id: "tocar",
    num: "05",
    label: "Tocar",
    note: "Grip en la manija. Si sueltas la puerta bajo 3°, se cierra. La hoja recorre su ancho menos 7.7 cm. El interruptor de la planta baja apunta hacia abajo.",
  },
  {
    id: "decidir",
    num: "06",
    label: "Decidir",
    note: "La noche tarda 4 segundos. Los postes se quedan encendidos y el relleno exterior se apaga. La cinta solo mide lo que tiene collider.",
  },
  {
    id: "cierre",
    num: "07",
    label: "Cierre",
    note: "Para una pregunta, salta con 1–7. Esta presentación no abre el visor ni un build de Unity.",
  },
] as const;

export const FLOORS: Record<
  FloorId,
  { name: string; place: string; body: string; facts: string[] }
> = {
  baja: {
    name: "Planta baja",
    place: "Sala",
    body: "Todo lo que está por debajo de Y = 4 m. Los interruptores cercanos de la misma planta se mueven juntos, y las luces del techo miran hacia abajo para no subir de piso.",
    facts: [
      "Corte de plantas en Y = 4 m",
      "Dos interruptores a menos de 0.6 m actúan como uno",
      "Spots de techo a 150°, cono interior de 90°, apuntando abajo",
    ],
  },
  alta: {
    name: "Planta alta",
    place: "SalaUp",
    body: "Por encima del corte. Cada interruptor se queda con las luces de su propia planta, las más cercanas en horizontal.",
    facts: [
      "SalaUp vive arriba del corte",
      "No recibe la luz de los spots de la planta baja",
      "El mismo menú de mano sirve en los dos pisos",
    ],
  },
  exterior: {
    name: "Exterior",
    place: "Afuera de El proyecto",
    body: "Aquí están el sol, el cielo y la niebla. De noche se apaga el relleno y se quedan los postes y las lámparas.",
    facts: [
      "El relleno exterior baja a cero de noche",
      "Focos, lámparas y postes siguen encendidos",
      "Casas adicionales está en la escena y queda fuera de este recorrido",
    ],
  },
};

export const VIEWS: Record<FloorId, { src: string; alt: string; caption: string }> = {
  baja: {
    src: "/fotos/interior-calido.jpg",
    alt: "Sala con cielo de madera, sofá claro, mesa y cocina al fondo",
    caption: "Sala · cielo de madera",
  },
  alta: {
    src: "/fotos/interior-claro.jpg",
    alt: "La misma sala con piso de baldosa, cielo liso y otra luz",
    caption: "Sala · otro acabado",
  },
  exterior: {
    src: "/fotos/fachada.jpg",
    alt: "Fachada de El proyecto, con la escalera exterior y el estacionamiento",
    caption: "Fachada y escalera",
  },
};

export const DESTINATIONS: { id: DestinationId; num: string; name: string; where: string }[] = [
  { id: "piso", num: "1", name: "Segundo piso", where: "Sobre Sala, en la planta alta" },
  { id: "sala", num: "2", name: "Sala", where: "Planta baja de El proyecto" },
  { id: "exterior", num: "3", name: "Exterior", where: "Afuera de la casa" },
];

export const MENU_LABEL: Record<MenuId, string> = {
  main: "Principal",
  teleport: "Teletransporte",
  lights: "Luces",
  walls: "Paredes",
  measure: "Medida",
};

export const MATERIALS: { id: string; label: string; asset: string; color: string }[] = [
  { id: "cielo", label: "Azul cielo", asset: "Azul_cielo", color: "#8CC7E8" },
  { id: "marino", label: "Azul marino", asset: "Azul_marino", color: "#243B63" },
  { id: "perla", label: "Gris perla", asset: "Gris_perla", color: "#D9DEE2" },
  { id: "nogal", label: "Madera nogal", asset: "Madera_nogal", color: "#795548" },
  { id: "marmol", label: "Mármol crema", asset: "Mármol_crema", color: "#E8DDC7" },
  { id: "carbon", label: "Negro carbón", asset: "Negro_carbón", color: "#25282B" },
  { id: "beige", label: "Beige", asset: "Pared_Beige", color: "#E8C39E" },
  { id: "blanco", label: "Blanco", asset: "Pared_Blanco", color: "#FFFFFF" },
  { id: "rosa", label: "Rosa empolvado", asset: "Rosa_empolvado", color: "#D79AA7" },
  { id: "bosque", label: "Verde bosque", asset: "Verde_bosque", color: "#315747" },
];

export const ORIGINAL_WALL = {
  label: "Original",
  color: "#D9D3C7",
};

export const QUESTIONS: { q: string; screen: number; detail: string }[] = [
  { q: "¿Dónde se corta la casa?", screen: 1, detail: "En Y = 4 m, entre Sala y SalaUp." },
  { q: "¿Qué botón abre el menú?", screen: 2, detail: "La X del control izquierdo." },
  { q: "¿Con qué se agarra la puerta?", screen: 2, detail: "Con el grip, cerca de la manija." },
  { q: "¿Cómo aparece el menú?", screen: 3, detail: "A 1.5 m y medio metro bajo la vista." },
  { q: "¿A dónde se salta?", screen: 3, detail: "Segundo piso, Sala y Exterior." },
  { q: "¿Por qué se pisan las escaleras?", screen: 3, detail: "La caja del tramo se cambia por la malla." },
  { q: "¿La puerta se queda abierta?", screen: 4, detail: "Bajo 3° se cierra sola al soltar." },
  { q: "¿Cuánto corre la ventana?", screen: 4, detail: "El ancho de la hoja menos 7.7 cm." },
  { q: "¿Cuánto tarda la noche?", screen: 5, detail: "4 segundos, con SmoothStep." },
  { q: "¿Qué acabados hay?", screen: 5, detail: "Los diez materiales del menú de paredes." },
];

export const STACK = [
  "Unity 6000.5.8f1",
  "URP 17.5",
  "XR Interaction Toolkit 3.5.1",
  "OpenXR 1.17",
  "Meta OpenXR 2.5",
  "Android XR OpenXR 1.3",
  "Android mín. SDK 34",
];
