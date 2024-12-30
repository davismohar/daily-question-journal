<script setup lang="ts">
import { useAuth0 } from "@auth0/auth0-vue";
import { ref, onMounted, watch } from "vue";
import { useRouter } from "vue-router";
const router = useRouter();
const auth0 = useAuth0();

const isAuthenticated = auth0.isAuthenticated;
const currentUser = auth0.user;

const postUserRegistration = async () => {
  console.log("currentUser", currentUser.value);
  if (!currentUser.value) return;

  const userRegistrationRequest = {
    subject: currentUser.value.sub,
    email: currentUser.value.email,
    given_name: currentUser.value.given_name,
    name: currentUser.value.name,
    picture: currentUser.value.picture,
    nickname: currentUser.value.nickname,
  };
  console.log(userRegistrationRequest);

  const response = await fetch(import.meta.env.VITE_API_URL + "/api/users", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(userRegistrationRequest),
  });
  return response;
};

watch(currentUser, () => {
  console.log("currentUser changed", currentUser.value);
  postUserRegistration();
});
</script>
<template>
  <div>
    <h1 v-if="!isAuthenticated">Home Page</h1>
    <h1 v-else>Hi, {{ currentUser?.given_name }}</h1>
    <p>Is Authenticated: {{ isAuthenticated }}</p>
    <p>Current User: {{ currentUser }}</p>
  </div>
</template>
