<script setup lang="ts">
import { ref, onMounted } from "vue";
import type { Question } from "../models/Question";
const currentDate = new Date();

const currentMonth = currentDate.getMonth() + 1;
const currentDay = currentDate.getDate();
const currentQuestion = ref<Question | null>(null);
const getCurrentQuestion = async () => {
  const response = await fetch(
    import.meta.env.VITE_API_URL +
      `/api/questions?month=${currentMonth}&day=${currentDay}`
  );
  const data = await response.json();
  currentQuestion.value = data;
};

onMounted(() => {
  getCurrentQuestion();
});
</script>
<template>
  <div>
    <div v-if="currentQuestion">
      <h1>{{ currentQuestion.question }}</h1>
    </div>
  </div>
</template>
