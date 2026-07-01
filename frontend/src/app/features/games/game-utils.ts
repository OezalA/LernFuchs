import { VocabularyWord } from '../../core/models';

/** Mischt ein Array (Fisher-Yates), verändert das Original nicht destruktiv nach außen. */
export function shuffle<T>(arr: T[]): T[] {
  const a = [...arr];
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

/** Kleiner Artikel (der/die/das) oder leerer String bei Nicht-Nomen. */
export function articleLabel(w: VocabularyWord): string {
  return w.article && w.article !== 'None' ? w.article.toLowerCase() : '';
}

/** Wort mit Artikel, z. B. "der Baum". */
export function wordWithArticle(w: VocabularyWord): string {
  const a = articleLabel(w);
  return a ? `${a} ${w.word}` : w.word;
}

/** Nur Wörter aus gelesenen Texten (oder eigenständige Wörter ohne Quelltext). */
export function readableWords(words: VocabularyWord[], readIds: Set<number>): VocabularyWord[] {
  return words.filter(w => w.sourcePassageId == null || readIds.has(w.sourcePassageId));
}
