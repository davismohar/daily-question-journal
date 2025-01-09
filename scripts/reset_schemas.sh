export PGPASSWORD=postgres
psql -h localhost -p 5432 -U postgres -d daily_questions -a -f scripts/drop_tables.sql
psql -h localhost -p 5432 -U postgres -d daily_questions -a -f scripts/db_migrations/01_create_schemas.sql
psql -h localhost -p 5432 -U postgres -d daily_questions -a -f scripts/db_migrations/02_add_user_role.sql
psql -h localhost -p 5432 -U postgres -d daily_questions -a -c "\copy questions (month_of_year, day_of_month, question) from 'resources/questions.csv' with (format csv, header true);"
