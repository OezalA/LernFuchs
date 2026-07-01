// Ordnet einem Thema ein passendes Emoji zu (für hübschere Gruppen-Überschriften).
const RULES: { keywords: string[]; icon: string }[] = [
  { keywords: ['tier', 'haustier', 'hund', 'katze', 'pferd', 'biene', 'insekt', 'wüstentier'], icon: '🐾' },
  { keywords: ['freund', 'familie', 'gefühl'], icon: '🤝' },
  { keywords: ['weltraum', 'planet', 'stern', 'galax', 'mond'], icon: '🚀' },
  { keywords: ['ritter', 'burg', 'mittelalter'], icon: '🏰' },
  { keywords: ['ägypt', 'pyramide', 'geschichte'], icon: '🏛️' },
  { keywords: ['sport', 'fußball', 'ball'], icon: '⚽' },
  { keywords: ['erfind', 'technik', 'roboter', 'fahrzeug', 'brücke', 'tunnel'], icon: '💡' },
  { keywords: ['natur', 'wald', 'baum', 'pflanze', 'blume'], icon: '🌳' },
  { keywords: ['jahreszeit', 'wetter', 'winter', 'sommer'], icon: '🌦️' },
  { keywords: ['ozean', 'meer', 'wasser', 'fluss'], icon: '🌊' },
  { keywords: ['dinosaurier', 'dino'], icon: '🦕' },
  { keywords: ['vulkan', 'wüste', 'berg'], icon: '🌋' },
  { keywords: ['musik', 'instrument'], icon: '🎵' },
  { keywords: ['ernährung', 'essen', 'obst', 'gemüse'], icon: '🍎' },
  { keywords: ['körper', 'gesund'], icon: '🫀' },
  { keywords: ['bauwerk', 'bauern', 'hof'], icon: '🏗️' },
  { keywords: ['recycl', 'umwelt'], icon: '♻️' },
  { keywords: ['pirat', 'abenteuer', 'mut', 'schatz'], icon: '🏴‍☠️' },
  { keywords: ['fest', 'brauch'], icon: '🎉' },
  { keywords: ['schule', 'lernen'], icon: '🎒' },
];

export function topicIcon(topic: string | null | undefined): string {
  if (!topic) return '📄';
  const t = topic.toLowerCase();
  for (const rule of RULES) {
    if (rule.keywords.some(k => t.includes(k))) return rule.icon;
  }
  return '📄';
}
