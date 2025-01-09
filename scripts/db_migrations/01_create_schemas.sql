CREATE TABLE
IF NOT EXISTS
users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subject TEXT NOT NULL,
    email TEXT NOT NULL,
    given_name TEXT NOT NULL,
    name TEXT NOT NULL,
    picture TEXT NOT NULL,
    nickname TEXT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
create unique index if not exists idx_users_subject on users (subject);
create unique index if not exists idx_users_email on users (email);


CREATE TABLE 
IF NOT EXISTS 
questions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    question TEXT NOT NULL,
    month_of_year INTEGER NOT NULL,
    day_of_month INTEGER NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
create unique index if not exists idx_questions_month_of_year_day_of_month on questions (month_of_year, day_of_month);

CREATE TABLE 
IF NOT EXISTS
answers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    question_id UUID NOT NULL,
    answer TEXT NOT NULL,
    user_id UUID NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    FOREIGN KEY (question_id) REFERENCES questions(id),
    FOREIGN KEY (user_id) REFERENCES users(id)
);
create index if not exists idx_answers_user_id_question_id on answers (user_id, question_id);

CREATE TABLE
IF NOT EXISTS
answer_sharing_relationships (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  share_subject UUID NOT NULL,
  share_granter UUID NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
  FOREIGN KEY (share_subject) references users(id),
  FOREIGN KEY (share_granter) references users(id)
);
create unique index if not exists idx_answer_sharing_relationships_hare_granter on answer_sharing_relationships (share_granter);

