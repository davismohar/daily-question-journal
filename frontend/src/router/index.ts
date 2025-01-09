import { createRouter, createWebHistory } from "vue-router";
import QuestionsListView from "../views/QuestionsListView.vue";
import QuestionWithAnswersView from "../views/QuestionWithAnswersView.vue";
import QuestionEditView from "../views/QuestionEditView.vue";
import AuthCallbackView from "../views/AuthCallbackView.vue";
import HomeView from "../views/HomeView.vue";
import AnswerSharingView from "../views/AnswerSharingView.vue";
const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: "/",
      name: "home",
      component: HomeView,
    },
    {
      path: "/questions",
      name: "questions",
      component: QuestionsListView,
    },
    {
      path: "/questions/:id/answers",
      name: "question-with-answers",
      component: QuestionWithAnswersView,
    },
    {
      path: "/questions/:id/edit",
      name: "question-edit",
      component: QuestionEditView,
    },
    {
      path: "/auth/callback",
      name: "auth-callback",
      component: AuthCallbackView,
    },
    {
      path: "/answer_sharing",
      name: "answer-sharing",
      component: AnswerSharingView,
    },
    {
      path: "/about",
      name: "about",
      // route level code-splitting
      // this generates a separate chunk (About.[hash].js) for this route
      // which is lazy-loaded when the route is visited.
      component: () => import("../views/AboutView.vue"),
    },
  ],
});

export default router;
