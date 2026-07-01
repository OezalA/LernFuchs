// Ordnet ein spezifisches Thema einer Oberkategorie zu (Überschrift + Emoji).
// So werden z. B. "Sport" -> "Hobby & Freizeit" und "Haustiere"/"Tiere" -> "Tiere".

export interface Category {
  name: string;
  icon: string;
}

interface CategoryRule extends Category {
  keywords: string[];
}

// Reihenfolge = Priorität (spezifischere zuerst).
const CATEGORIES: CategoryRule[] = [
  { name: 'Weltall', icon: '🚀', keywords: ['weltall', 'weltraum', 'planet', 'stern', 'galax', 'mond', 'astronaut', 'rakete', 'sonnensystem'] },
  { name: 'Tiere', icon: '🐾', keywords: ['tier', 'haustier', 'hund', 'katze', 'pferd', 'biene', 'insekt', 'vogel', 'fisch', 'wild', 'dino'] },
  { name: 'Geschichte', icon: '🏛️', keywords: ['ritter', 'burg', 'ägypt', 'pharao', 'römer', 'mittelalter', 'steinzeit', 'wikinger', 'pyramide', 'geschichte des', 'antik'] },
  { name: 'Literatur & Geschichten', icon: '📚', keywords: ['literatur', 'märchen', 'fabel', 'sage', 'autor', 'gedicht', 'erzähl', 'roman', 'buch'] },
  { name: 'Abenteuer & Fantasie', icon: '🏴‍☠️', keywords: ['abenteuer', 'pirat', 'drache', 'fantasie', 'schatz', 'zauber', 'held'] },
  { name: 'Hobby & Freizeit', icon: '⚽', keywords: ['hobby', 'freizeit', 'sport', 'fußball', 'ball', 'spiel', 'basteln', 'tanz', 'schwimmen', 'fahrrad', 'turnen'] },
  { name: 'Länder & Kulturen', icon: '🌍', keywords: ['land', 'länder', 'kontinent', 'deutschland', 'europa', 'kultur', 'fest', 'brauch', 'tradition', 'geografie', 'stadt', 'reise'] },
  { name: 'Wissenschaft & Technik', icon: '🔬', keywords: ['erfind', 'technik', 'roboter', 'experiment', 'wissenschaft', 'maschine', 'strom', 'computer', 'brücke', 'tunnel', 'fahrzeug', 'auto'] },
  { name: 'Mensch & Körper', icon: '🫀', keywords: ['körper', 'mensch', 'gesund', 'muskel', 'herz', 'gefühl', 'sinne', 'skelett', 'zahn'] },
  { name: 'Familie & Freunde', icon: '🤝', keywords: ['familie', 'freund', 'freundschaft', 'geschwister', 'zusammen'] },
  { name: 'Essen & Ernährung', icon: '🍎', keywords: ['essen', 'ernährung', 'obst', 'gemüse', 'rezept', 'kochen', 'frühstück', 'nahrung'] },
  { name: 'Schule & Berufe', icon: '🎒', keywords: ['schule', 'beruf', 'lernen', 'klasse', 'lehrer', 'arbeit', 'feuerwehr', 'polizei'] },
  { name: 'Natur & Umwelt', icon: '🌳', keywords: ['natur', 'umwelt', 'pflanze', 'baum', 'blume', 'wetter', 'jahreszeit', 'wald', 'ozean', 'meer', 'wasser', 'recycl', 'vulkan', 'klima', 'wüste'] },
];

const FALLBACK: Category = { name: 'Sonstiges', icon: '📄' };

export function categoryFor(topic: string | null | undefined): Category {
  if (!topic) return FALLBACK;
  const t = topic.toLowerCase();
  for (const c of CATEGORIES) {
    if (c.keywords.some(k => t.includes(k))) return { name: c.name, icon: c.icon };
  }
  return FALLBACK;
}
