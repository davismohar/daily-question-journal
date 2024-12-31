<script setup lang="ts">
import { useAuth0 } from "@auth0/auth0-vue";

const auth0 = useAuth0();

const handleLogout = () => {
  auth0.logout({ logoutParams: { returnTo: import.meta.env.VITE_BASE_URL } });
};
</script>

<template>
  <button @click="handleLogout">Log Out</button>
</template>
