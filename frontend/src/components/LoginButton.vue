<script setup lang="ts">
import { useAuth0 } from "@auth0/auth0-vue";

const auth0 = useAuth0();

const handleLogin = () => {
  auth0.loginWithRedirect({
    authorizationParams: {
      redirect_uri: import.meta.env.VITE_API_URL + "/auth/callback",
    },
  });
};
</script>

<template>
  <button class="button__login" @click="handleLogin">Log In</button>
</template>
