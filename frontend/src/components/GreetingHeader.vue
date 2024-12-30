<script setup lang="ts">
import { useAuth0 } from "@auth0/auth0-vue";

const { isAuthenticated, user } = useAuth0();
</script>

<template>
  <h1 v-if="isAuthenticated">Hello, {{ user?.name }}!</h1>
  <h1 v-else>Hello</h1>
</template>
