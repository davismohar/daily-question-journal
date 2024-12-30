import "./assets/main.css";

import { createApp } from "vue";
import App from "./App.vue";
import router from "./router";
import { createAuth0 } from "@auth0/auth0-vue";

const app = createApp(App);

app.use(router).use(
  createAuth0({
    domain: "dev-spr842pm040mf5yw.us.auth0.com",
    clientId: "XagUMrspFoeqyLODCejxdy2aWU39DfeB",
    authorizationParams: {
      redirect_uri: window.location.origin + "/auth/callback",
      audience: "https/dailyjournal",
    },
  })
);

app.mount("#app");
