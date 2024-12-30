<script setup lang="ts">
import { ref, onMounted } from "vue";

// Define interface for Question type
interface Question {
  id: string;
  question: string;
  isHovered: boolean;
  day: number;
  month: number;
}

// Create reactive reference for questions
const questions = ref<Question[]>([]);

const fetchQuestions = async () => {
  try {
    const response = await fetch(
      import.meta.env.VITE_API_URL + "/api/questions"
    );
    const data = await response.json();
    questions.value = data;
    console.log(data);
  } catch (error) {
    console.error("Error fetching questions:", error);
  }
};

const deleteQuestion = async (id: number) => {
  try {
    const response = await fetch(
      import.meta.env.VITE_API_URL + `/api/questions/${id}`,
      {
        method: "DELETE",
      }
    );

    if (!response.ok) {
      throw new Error("Failed to delete question");
    }

    // Refresh the questions list after successful deletion
    await fetchQuestions();
  } catch (error) {
    console.error("Error deleting question:", error);
  }
};

defineExpose({
  fetchQuestions,
});

// Fetch questions when component mounts
onMounted(async () => {
  await fetchQuestions();
});
</script>

<template>
  <div>
    <h1>Questions</h1>
    <div v-if="questions.length">
      <div
        v-for="question in questions"
        :key="question.id"
        class="question"
        :class="{ 'is-hovered': question.isHovered }"
      >
        <router-link
          :to="`/questions/${question.id}/answers`"
          class="question-link"
        >
          <h2 class="question-content">
            {{ question.month }}/{{ question.day }}
            {{ question.question }}

            <router-link :to="`/questions/${question.id}/edit`">
              <button class="edit-button">Edit</button>
            </router-link>
          </h2>
        </router-link>
      </div>
    </div>

    <div v-else>
      <p>Loading questions...</p>
    </div>
  </div>
</template>

<style scoped>
.question-link {
  border: 1px solid #ddd;
  margin: 1rem 0;
  padding: 1rem;
  border-radius: 4px;
  text-decoration: none;
  color: inherit;
  display: block;
}

.question-link:hover {
  background-color: #5b5b5b;
}
.question-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.edit-button {
  margin-left: auto;
  margin-right: 0;
  padding: 0.5rem 1rem;
  background-color: #007bff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.edit-button:hover {
  background-color: #0056b3;
}

h2 {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin: 0;
}

.question-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
