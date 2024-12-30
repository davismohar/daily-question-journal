import type { Answer } from "./Answer";

interface Question {
  id: string;
  question: string;
  day: number;
  month: number;
  answers: Answer[];
}

export type { Question };
