<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRoute } from "vue-router";
import { useAuth0 } from "@auth0/auth0-vue";
import type { Question } from "../models/Question";

const props = defineProps<{
  questionId: string;
}>();

const route = useRoute();
const auth0 = useAuth0();
const question = ref<Question | null>(null);
const error = ref<string | null>(null);
const answerText = ref("");
const isSubmitting = ref(false);

const formatDate = (date: string) => {
  return new Date(date).toLocaleDateString();
};

const fetchQuestion = async () => {
  try {
    const accessToken = await auth0.getAccessTokenSilently();
    console.log(accessToken);
    const response = await fetch(
      import.meta.env.VITE_API_URL +
        `/api/questions/${props.questionId}/answers`,
      {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      }
    );
    const data = await response.json();
    console.log(data);
    if (!response.ok) {
      throw new Error("Failed to fetch question");
    }
    question.value = data;
  } catch (err) {
    error.value = "Error loading question";
    console.error(err);
  }
};

const submitAnswer = async () => {
  try {
    isSubmitting.value = true;
    const accessToken = await auth0.getAccessTokenSilently();
    console.log(accessToken);
    const response = await fetch(
      import.meta.env.VITE_API_URL +
        `/api/questions/${props.questionId}/answers`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify({
          answer: answerText.value,
        }),
      }
    );

    if (!response.ok) {
      throw new Error("Failed to submit answer");
    }

    answerText.value = "";
    await fetchQuestion();
  } catch (err) {
    error.value = "Error submitting answer";
    console.error(err);
  } finally {
    isSubmitting.value = false;
  }
};

onMounted(() => {
  fetchQuestion();
});
</script>

<template>
  <div v-if="error" class="error">
    {{ error }}
  </div>
  <div v-else-if="question" class="question">
    <h1 class="content">{{ question.question }}</h1>
    <div v-for="answer in question.answers" :key="answer.id" class="answer">
      <p class="date">{{ formatDate(answer.created_at) }}</p>
      <p>{{ answer.answer }}</p>
    </div>
  </div>
  <div v-else class="loading">Loading...</div>

  <div class="answer-form">
    <input
      v-model="answerText"
      class="answer-input"
      type="text"
      placeholder="Type your answer here..."
      :disabled="isSubmitting"
      @keyup.enter="submitAnswer"
    />
    <button
      @click="submitAnswer"
      :disabled="!answerText.trim() || isSubmitting"
      class="submit-button"
    >
      Submit Answer
    </button>
  </div>
</template>

<style scoped>
.question {
  max-width: 800px;
  margin: 0 auto;
  padding: 20px;
}

.answer {
  margin: 10px 0;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
}

.content {
  white-space: pre-wrap;
  margin: 20px 0;
}

.date {
  color: #666;
  font-size: 0.9em;
}

.error {
  color: red;
  text-align: center;
  padding: 20px;
}

.loading {
  text-align: center;
  padding: 20px;
}

.answer-form {
  max-width: 800px;
  margin: 20px auto;
  padding: 20px;
}

.answer-input {
  width: 100%;
  margin-bottom: 10px;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
  resize: vertical;
}

input {
  flex: 1;
  padding: 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 1rem;
}

.submit-button {
  background-color: #4caf50;
  color: white;
  padding: 10px 20px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.submit-button:disabled {
  background-color: #cccccc;
  cursor: not-allowed;
}

.submit-button:hover:not(:disabled) {
  background-color: #45a049;
}
</style>
