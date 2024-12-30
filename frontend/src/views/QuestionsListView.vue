<script setup lang="ts">
import QuestionsList from "../components/QuestionsList.vue";
import NewQuestionInput from "../components/NewQuestionInput.vue";
import { useTemplateRef } from "vue";

const questionsListRef = useTemplateRef("questionsListRef");

const handleQuestionSubmitted = () => {
  questionsListRef.value?.fetchQuestions();
};
</script>

<template>
  <main>
    <QuestionsList ref="questionsListRef" />
    <NewQuestionInput @questionSubmitted="handleQuestionSubmitted" />
  </main>
</template>
