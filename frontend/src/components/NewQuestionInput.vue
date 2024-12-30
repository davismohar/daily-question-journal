<script setup lang="ts">
import { ref } from "vue";

const newQuestion = ref("");
const isSubmitting = ref(false);

const submitQuestion = async () => {
  if (!newQuestion.value.trim()) return;

  isSubmitting.value = true;
  try {
    const response = await fetch("/api/questions", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        question: newQuestion.value,
      }),
    });

    if (!response.ok) {
      throw new Error("Failed to submit question");
    }

    // Clear the input after successful submission
    newQuestion.value = "";

    // Emit an event to notify parent component
    emit("questionSubmitted");
  } catch (error) {
    console.error("Error submitting question:", error);
  } finally {
    isSubmitting.value = false;
  }
};

// Define emits
const emit = defineEmits(["questionSubmitted"]);
</script>

<template>
  <div class="question-input">
    <input
      v-model="newQuestion"
      type="text"
      placeholder="Type your question here..."
      :disabled="isSubmitting"
      @keyup.enter="submitQuestion"
    />
    <button
      @click="submitQuestion"
      :disabled="isSubmitting || !newQuestion.trim()"
    >
      {{ isSubmitting ? "Submitting..." : "Submit Question" }}
    </button>
  </div>
</template>

<style scoped>
.question-input {
  display: flex;
  gap: 1rem;
  margin: 1rem 0;
}

input {
  flex: 1;
  padding: 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 1rem;
}

button {
  padding: 0.5rem 1rem;
  background-color: #4caf50;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 1rem;
}

button:disabled {
  background-color: #cccccc;
  cursor: not-allowed;
}

button:hover:not(:disabled) {
  background-color: #45a049;
}
</style>
