#!/bin/bash

# Start PostgreSQL container
docker run --name local-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_DB=daily_questions \
  -p 5432:5432 \
  -d postgres:17
