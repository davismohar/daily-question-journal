<script setup lang="ts">
import HomePage from "../components/UnauthenticatedHomePage.vue";
import QuestionWithAnswers from "../components/QuestionWithAnswers.vue";
import { useAuth0 } from "@auth0/auth0-vue";
import { ref, onMounted } from "vue";
import type { Question } from "../models/Question";
import Greetings from "../components/GreetingHeader.vue";
const auth0 = useAuth0();
const isAuthenticated = auth0.isAuthenticated;
const questionId = ref<string | null>(null);
const currentQuestion = ref<Question | null>(null);

const currentDate = new Date();
const currentMonth = ref<number>(currentDate.getMonth() + 1);
const currentDay = ref<number>(currentDate.getDate());

const getCurrentQuestion = async () => {
  const response = await fetch(
    import.meta.env.VITE_API_URL +
      `/api/questions?month=${currentMonth.value}&day=${currentDay.value}`
  );
  const data = await response.json();
  questionId.value = data.id;
};

onMounted(() => {
  getCurrentQuestion();
});
</script>

<template>
  <main>
    <Greetings />
    <HomePage v-if="!isAuthenticated || !questionId" />
    <QuestionWithAnswers v-else-if="questionId" :questionId="questionId" />
    <div v-else>Loading...</div>
  </main>
</template>
