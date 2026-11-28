
import * as Speech from "expo-speech";

let speechVersion = 0;

let finishCurrentSpeech:
  ((completed: boolean) => void) | null = null;

async function stopCurrentSpeech(): Promise<void> {
  const finish = finishCurrentSpeech;

  await Speech.stop();

  // Ensure a waiting Promise is resolved even if
  // the native onStopped callback is not received.
  finish?.(false);
}

export async function speakAnswer(
  answer: string,
  preferredLanguage?: string | null
): Promise<boolean> {

  if (!answer.trim()) return false;

  const version = ++speechVersion;

  const language =
    preferredLanguage
      ?.trim()
      .toLowerCase()
      .replace(/_/g, "-")
      .split("-")[0] || "en";

  // Stop any previous answer.
  await stopCurrentSpeech();

  // Ignore this request if a newer one arrived.
  if (version !== speechVersion) {
    return false;
  }

  return new Promise<boolean>((resolve, reject) => {

    let settled = false;

    function finish(completed: boolean) {
      if (settled) return;

      settled = true;

      if (finishCurrentSpeech === finish) {
        finishCurrentSpeech = null;
      }

      resolve(completed);
    }

    function fail(error: unknown) {
      if (settled) return;

      settled = true;

      if (finishCurrentSpeech === finish) {
        finishCurrentSpeech = null;
      }

      reject(error);
    }

    finishCurrentSpeech = finish;

    try {
      Speech.speak(answer, {
        language,
        rate: 0.95,

        onStart: () => {
          console.log("[Speech] Started");
        },

        onDone: () => {
          console.log("[Speech] Finished");
          finish(true);
        },

        onStopped: () => {
          console.log("[Speech] Interrupted");
          finish(false);
        },

        onError: (error: Error) => {
          console.error("[Speech] Error:", error);
          fail(error);
        },
      });
    } catch (error) {
      fail(error);
    }
  });
}

export async function stopSpeaking(): Promise<void> {
  speechVersion++;

  await stopCurrentSpeech();
}
