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
// Jede Kategorie kennt deutsche UND englische Stichwörter, damit auch
// englische Themen (Fremdsprache) korrekt einsortiert werden.
const CATEGORIES: CategoryRule[] = [
  { name: 'Weltall', icon: '🚀', keywords: ['weltall', 'weltraum', 'planet', 'stern', 'galax', 'mond', 'astronaut', 'rakete', 'sonnensystem', 'space', 'star', 'moon', 'rocket', 'espacio', 'planeta', 'estrella', 'luna'] },
  { name: 'Tiere', icon: '🐾', keywords: ['tier', 'haustier', 'hund', 'katze', 'pferd', 'biene', 'insekt', 'vogel', 'fisch', 'wild', 'dino', 'animal', 'pet', 'dog', 'cat', 'mascota', 'perro', 'gato'] },
  { name: 'Geschichte', icon: '🏛️', keywords: ['ritter', 'burg', 'ägypt', 'pharao', 'römer', 'mittelalter', 'steinzeit', 'wikinger', 'pyramide', 'geschichte des', 'antik', 'knight', 'roman', 'viking', 'history', 'historia'] },
  { name: 'Literatur & Geschichten', icon: '📚', keywords: ['literatur', 'märchen', 'fabel', 'sage', 'autor', 'gedicht', 'erzähl', 'roman', 'buch', 'story', 'fairy', 'tale', 'book', 'poem', 'cuento', 'libro'] },
  { name: 'Abenteuer & Fantasie', icon: '🏴‍☠️', keywords: ['abenteuer', 'pirat', 'drache', 'fantasie', 'schatz', 'zauber', 'held', 'adventure', 'pirate', 'dragon', 'treasure', 'magic', 'hero', 'aventura', 'tesoro', 'héroe'] },
  { name: 'Hobby & Freizeit', icon: '⚽', keywords: ['hobby', 'freizeit', 'sport', 'fußball', 'ball', 'spiel', 'basteln', 'tanz', 'schwimmen', 'fahrrad', 'turnen', 'toy', 'game', 'football', 'soccer', 'play', 'dance', 'deporte', 'juguete', 'pasatiempo', 'jugar', 'fútbol'] },
  { name: 'Länder & Kulturen', icon: '🌍', keywords: ['land', 'länder', 'kontinent', 'deutschland', 'europa', 'kultur', 'fest', 'brauch', 'tradition', 'geografie', 'stadt', 'reise', 'country', 'town', 'city', 'holiday', 'travel', 'festival', 'world', 'ciudad', 'país', 'mundo', 'fiesta', 'viaje'] },
  { name: 'Wissenschaft & Technik', icon: '🔬', keywords: ['erfind', 'technik', 'roboter', 'experiment', 'wissenschaft', 'maschine', 'strom', 'computer', 'brücke', 'tunnel', 'fahrzeug', 'auto', 'technology', 'robot', 'machine', 'science', 'ciencia', 'tecnología', 'máquina'] },
  { name: 'Mensch & Körper', icon: '🫀', keywords: ['körper', 'mensch', 'gesund', 'muskel', 'herz', 'gefühl', 'sinne', 'skelett', 'zahn', 'body', 'feeling', 'health', 'sense', 'cuerpo', 'sentimiento', 'salud', 'sentido'] },
  { name: 'Familie & Freunde', icon: '🤝', keywords: ['familie', 'freund', 'freundschaft', 'geschwister', 'zusammen', 'family', 'friend', 'familia', 'amigo'] },
  { name: 'Essen & Ernährung', icon: '🍎', keywords: ['essen', 'ernährung', 'obst', 'gemüse', 'rezept', 'kochen', 'frühstück', 'nahrung', 'food', 'drink', 'fruit', 'vegetable', 'eat', 'breakfast', 'meal', 'comida', 'fruta', 'verdura', 'comer', 'desayuno'] },
  { name: 'Schule & Berufe', icon: '🎒', keywords: ['schule', 'beruf', 'lernen', 'klasse', 'lehrer', 'arbeit', 'feuerwehr', 'polizei', 'school', 'job', 'teacher', 'work', 'escuela', 'colegio', 'profesor', 'trabajo'] },
  { name: 'Natur & Umwelt', icon: '🌳', keywords: ['natur', 'umwelt', 'pflanze', 'baum', 'blume', 'wetter', 'jahreszeit', 'wald', 'ozean', 'meer', 'wasser', 'recycl', 'vulkan', 'klima', 'wüste', 'nature', 'weather', 'plant', 'tree', 'flower', 'sea', 'ocean', 'water', 'season', 'naturaleza', 'tiempo', 'planta', 'flor', 'árbol', 'agua', 'clima'] },
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
