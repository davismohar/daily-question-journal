<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useAuth0 } from "@auth0/auth0-vue";

interface Question {
  id: number;
  question: string;
}

const route = useRoute();
const question = ref<Question | null>(null);
const questionText = ref("");
const error = ref<string | null>(null);
const router = useRouter();
const auth0 = useAuth0();

const fetchQuestion = async () => {
  try {
    const response = await fetch(
      import.meta.env.VITE_API_URL + `/api/questions/${route.params.id}`
    );
    const data = await response.json();
    console.log(data);
    if (!response.ok) {
      throw new Error("Failed to fetch question");
    }
    question.value = data;
    questionText.value = data.question;
  } catch (err) {
    error.value = "Error loading question";
    console.error(err);
  }
};

const updateQuestion = async () => {
  try {
    let accessToken = await auth0.getAccessTokenSilently();
    await fetch(
      import.meta.env.VITE_API_URL + `/api/questions/${route.params.id}`,
      {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify({
          question: questionText.value,
        }),
      }
    );
  } catch (err) {
    console.error(err);
  }
};

const deleteQuestion = async () => {
  try {
    await fetch(
      import.meta.env.VITE_API_URL + `/api/questions/${route.params.id}`,
      {
        method: "DELETE",
      }
    );
    router.push("/questions");
  } catch (err) {
    console.error(err);
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
    <form @submit.prevent="updateQuestion" class="question-form">
      <input v-model="questionText" type="text" class="question-input" />
      <button type="submit" @click.prevent="updateQuestion">Update</button>
    </form>
  </div>
  <div v-else class="loading">Loading...</div>
  <button class="delete-button" @click="deleteQuestion">Delete</button>
</template>

<style scoped>
.question {
  max-width: 800px;
  margin: 0 auto;
  padding: 20px;
}

.question-input {
  display: flex;
  gap: 1rem;
  width: 100%;
  margin-bottom: 10px;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
  resize: vertical;
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
  min-height: 100px;
  margin-bottom: 10px;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
  resize: vertical;
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

.delete-button {
  background-color: #dc3545;
  color: white;
  padding: 10px 20px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}
</style>
