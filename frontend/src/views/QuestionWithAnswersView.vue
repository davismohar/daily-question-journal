<script setup lang="ts">
import QuestionWithAnswers from "../components/QuestionWithAnswers.vue";
import { useRoute } from "vue-router";

const route = useRoute();
const questionId = route.params.id as string;
</script>

<template>
  <main>
    <QuestionWithAnswers :questionId="questionId" />
  </main>
</template>
