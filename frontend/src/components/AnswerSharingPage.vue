<template>
    <div>
        <h1>Answers Shared With:</h1>
        <ul>
            <li v-for="subject in answerSharingSubjects" :key="subject">{{ subject }}</li>
        </ul>
        <form @submit.prevent="addSubject">
            <input v-model="newSubjectEmail" type="email" placeholder="Enter email" required />
            <button type="submit">Add Subject</button>
        </form>
    </div>
</template>

<script setup lang="ts">
import { useAuth0 } from "@auth0/auth0-vue";
import { ref, onMounted } from "vue";

const answerSharingSubjects = ref<Array<string>>([]);
const newSubjectEmail = ref<string>("");
const auth0 = useAuth0();

const fetchAnswerSharingSubjects = async () => {
    try {
        const token = await auth0.getAccessTokenSilently();
        const response = await fetch('http://localhost:8080/api/shared_answers', {
            headers: {
                Authorization: `Bearer ${token}`
            }
        });
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        const data = await response.json();
        console.log(data);
        answerSharingSubjects.value = data;
    } catch (error) {
        console.error('Error fetching answers:', error);
    }
};

const addSubject = async () => {
    try {
        const token = await auth0.getAccessTokenSilently();
        const response = await fetch('http://localhost:8080/api/shared_answers', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                Authorization: `Bearer ${token}`
            },
            body: JSON.stringify({ subjectEmail: newSubjectEmail.value })
        });
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        const data = await response.json();
        answerSharingSubjects.value.push(newSubjectEmail.value);
        newSubjectEmail.value = "";
    } catch (error) {
        console.error('Error adding subject:', error);
    }
};

onMounted(() => {
    fetchAnswerSharingSubjects();
});
</script>

<style scoped>
/* Add your styles here */
</style>