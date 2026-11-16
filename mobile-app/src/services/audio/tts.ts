import * as Speech from "expo-speech";

export function speakAnswer(
  answer: string,
  preferredLanguage?: string | null
) {
  if (!answer.trim()) return;

  const language =
    preferredLanguage?.toLowerCase() ?? "en";

  const locales: Record<string, string> = {
    en: "en-US",
    fr: "fr-FR",
    ar: "ar",
  };

  Speech.stop().then(() => {
    Speech.speak(answer, {
      language: locales[language] ?? "en-US",
      rate: 0.95,
    });
  });
}

export function stopSpeaking() {
  return Speech.stop();
}