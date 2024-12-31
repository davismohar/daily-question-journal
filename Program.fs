module DailyJournal.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open Npgsql.FSharp
open System
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.Extensions.DependencyInjection
open Microsoft.IdentityModel.Tokens
open System.Security.Claims


let corsPolicyName = "MyCorsPolicy"

let corsPolicy (policyBuilder: CorsPolicyBuilder) =
    policyBuilder
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithOrigins(
            "http://localhost:5173",
            "https://davismohar.github.io",
            "https://daily-question-journal.vercel.app"
        )
    |> ignore

let corsOptions (options: CorsOptions) =
    options.AddPolicy(corsPolicyName, corsPolicy)

let connectionString = System.Environment.GetEnvironmentVariable("DATABASE_URL")

type Question =
    { id: Guid
      question: string
      day: int
      month: int }

type Answer =
    { id: Guid
      answer: string
      created_at: DateTime }

type QuestionWithAnswers =
    { id: Guid
      question: string
      day: int
      month: int
      answers: Answer list }

type UserRegistrationRequest =
    { subject: string
      email: string
      given_name: string
      name: string
      picture: string
      nickname: string }

type User =
    { id: Guid
      subject: string
      email: string
      given_name: string
      name: string
      picture: string
      nickname: string }

let getUserIdBySubject (connection: Sql.SqlProps) (subject: string) : Guid =
    connection
    |> Sql.query "SELECT id FROM users WHERE subject = @subject"
    |> Sql.parameters [ "subject", Sql.string subject ]
    |> Sql.executeRow (fun read -> read.uuid "id")

let getQuestionByMonthAndDay (connection: Sql.SqlProps) (month: int) (day: int) : Question option =
    connection
    |> Sql.query
        "SELECT id, question, month_of_year, day_of_month FROM questions WHERE month_of_year = @month AND day_of_month = @day"
    |> Sql.parameters [ "month", Sql.int month; "day", Sql.int day ]
    |> Sql.executeRow (fun read ->
        { id = read.uuid "id"
          question = read.string "question"
          day = read.int "day_of_month"
          month = read.int "month_of_year" })
    |> Option.ofObj

let getAllQuestions (connection: Sql.SqlProps) : Question list =
    connection
    |> Sql.query "SELECT id, question, month_of_year, day_of_month FROM questions"
    |> Sql.execute (fun read ->
        { id = read.uuid "id"
          question = read.string "question"
          day = read.int "day_of_month"
          month = read.int "month_of_year" })

let insertQuestion (connection: Sql.SqlProps) (question: string) =
    connection
    |> Sql.query "INSERT INTO questions (question) VALUES (@question) RETURNING id, question"
    |> Sql.parameters [ "question", Sql.string question ]
    |> Sql.executeRow (fun read ->
        { id = read.uuid "id"
          question = read.string "question"
          day = read.int "day"
          month = read.int "month" })

type QuestionRequest = { question: string }

type AnswerRequest = { answer: string }

let createQuestion (connection: Sql.SqlProps) (question: QuestionRequest) =
    insertQuestion connection question.question

let handlePostQuestion (connection: Sql.SqlProps) =
    Request.mapJson (fun question ->
        let newQuestion = createQuestion connection question
        Response.ofJson newQuestion)

let getQuestionById (connection: Sql.SqlProps) (id: Guid) : Question option =
    connection
    |> Sql.query "SELECT id, question, day_of_month, month_of_year FROM questions WHERE id = @id"
    |> Sql.parameters [ "id", Sql.uuid id ]
    |> Sql.executeRow (fun read ->
        { id = read.uuid "id"
          question = read.string "question"
          day = read.int "day_of_month"
          month = read.int "month_of_year" })
    |> Option.ofObj

let getAnswersByQuestionId (connection: Sql.SqlProps) (id: Guid) : Answer list =
    connection
    |> Sql.query "SELECT id, answer, created_at FROM answers WHERE question_id = @id"
    |> Sql.parameters [ "id", Sql.uuid id ]
    |> Sql.execute (fun read ->
        { id = read.uuid "id"
          answer = read.string "answer"
          created_at = read.dateTime "created_at" })

let getAnswersByQuestionIdAndUserId (connection: Sql.SqlProps) (id: Guid) (userId: Guid) : Answer list =
    connection
    |> Sql.query "SELECT id, answer, created_at FROM answers WHERE question_id = @id AND user_id = @userId"
    |> Sql.parameters [ "id", Sql.uuid id; "userId", Sql.uuid userId ]
    |> Sql.execute (fun read ->
        { id = read.uuid "id"
          answer = read.string "answer"
          created_at = read.dateTime "created_at" })

let handleGetQuestion (connection: Sql.SqlProps) (id: string) : HttpHandler =
    match Guid.TryParse id with
    | true, guid ->
        match getQuestionById connection guid with
        | Some question -> Response.ofJson question
        | None -> Response.withStatusCode 404 >> Response.ofPlainText "Question not found"
    | false, _ -> Response.withStatusCode 400 >> Response.ofPlainText "Invalid ID format"

let deleteQuestionById (connection: Sql.SqlProps) (id: Guid) : bool =
    connection
    |> Sql.query
        "
    DELETE FROM answers WHERE question_id = @id;
    DELETE FROM questions WHERE id = @id;
    "
    |> Sql.parameters [ "id", Sql.uuid id ]
    |> Sql.executeNonQuery
    |> fun rowsAffected -> rowsAffected > 0

let handleGetQuestionWithAnswers (connection: Sql.SqlProps) (questionId: string) (subject: string) : HttpHandler =
    match Guid.TryParse questionId with
    | true, guid ->
        let question = getQuestionById connection guid
        let userId = getUserIdBySubject connection subject
        let answers = getAnswersByQuestionIdAndUserId connection guid userId

        match question with
        | Some q ->
            Response.ofJson
                { id = q.id
                  question = q.question
                  day = q.day
                  month = q.month
                  answers = answers }
        | None -> Response.withStatusCode 404 >> Response.ofPlainText "Question not found"
    | false, _ -> Response.withStatusCode 400 >> Response.ofPlainText "Invalid ID format"

let handleDeleteQuestion (connection: Sql.SqlProps) (id: string) : HttpHandler =
    match Guid.TryParse id with
    | true, guid ->
        if deleteQuestionById connection guid then
            Response.withStatusCode 204 >> Response.ofEmpty
        else
            Response.withStatusCode 404 >> Response.ofPlainText "Question not found"
    | false, _ -> Response.withStatusCode 400 >> Response.ofPlainText "Invalid ID format"

let insertAnswer (connection: Sql.SqlProps) (questionId: Guid) (answer: AnswerRequest) (userId: Guid) : Answer =
    connection
    |> Sql.query
        "INSERT INTO answers (question_id, answer, user_id) VALUES (@questionId, @answer, @userId) RETURNING id, answer, created_at"
    |> Sql.parameters
        [ "questionId", Sql.uuid questionId
          "answer", Sql.string answer.answer
          "userId", Sql.uuid userId ]
    |> Sql.executeRow (fun read ->
        { id = read.uuid "id"
          answer = read.string "answer"
          created_at = read.dateTime "created_at" })



let handlePostAnswer (connection: Sql.SqlProps) (questionId: string) (subject: string) : HttpHandler =
    Request.mapJson (fun answer ->
        let questionId = Guid.Parse questionId
        let userId = getUserIdBySubject connection subject
        let newAnswer = insertAnswer connection questionId answer userId
        Response.ofJson newAnswer)

let insertUser (connection: Sql.SqlProps) (user: UserRegistrationRequest) : User =
    // First check if user exists by email
    let existingUser =
        connection
        |> Sql.query "SELECT id, subject, email, given_name, name, picture, nickname FROM users WHERE email = @email"
        |> Sql.parameters [ "email", Sql.string user.email ]
        |> Sql.executeRow (fun read ->
            { id = read.uuid "id"
              subject = read.string "subject"
              email = read.string "email"
              given_name = read.string "given_name"
              name = read.string "name"
              picture = read.string "picture"
              nickname = read.string "nickname" })
        |> Option.ofObj

    match existingUser with
    | Some user -> user // Return existing user if found
    | None -> // Insert new user if not found
        connection
        |> Sql.query
            "INSERT INTO users (subject, email, given_name, name, picture, nickname) VALUES (@subject, @email, @given_name, @name, @picture, @nickname) RETURNING id, subject, email, given_name, name, picture, nickname"
        |> Sql.parameters
            [ "subject", Sql.string user.subject
              "email", Sql.string user.email
              "given_name", Sql.string user.given_name
              "name", Sql.string user.name
              "picture", Sql.string user.picture
              "nickname", Sql.string user.nickname ]
        |> Sql.executeRow (fun read ->
            { id = read.uuid "id"
              subject = read.string "subject"
              email = read.string "email"
              given_name = read.string "given_name"
              name = read.string "name"
              picture = read.string "picture"
              nickname = read.string "nickname" })

let validateUserRegistrationRequest (user: UserRegistrationRequest) : bool =
    user.subject <> ""
    && user.email <> ""
    && user.subject <> null
    && user.email <> null

let handlePostUser (connection: Sql.SqlProps) : HttpHandler =
    Request.mapJson (fun user ->
        printfn "User registration request: %A" user

        if validateUserRegistrationRequest user then
            let newUser = insertUser connection user
            Response.ofJson newUser
        else
            Response.withStatusCode 400
            >> Response.ofPlainText "Invalid user registration request")


let configureLogging (log: ILoggingBuilder) =
    log.ClearProviders() |> ignore
    log.AddConsole() |> ignore
    log.AddDebug() |> ignore
    log

module AuthConfig =
    let authority = "https://dev-spr842pm040mf5yw.us.auth0.com/"
    let audience = "https/dailyjournal"

    let writeAnswersPolicy = "write:answers"

// module Auth =
//     let hasScope (scope : string) (next : HttpHandler) : HttpHandler =
//         Request.ifAuthenticatedWithScope AuthConfig.authority scope next ErrorPages.forbidden

// ------------
// Register services
// ------------
let authService (svc: IServiceCollection) =
    let createTokenValidationParameters () =
        let tvp = new TokenValidationParameters()
        tvp.NameClaimType <- ClaimTypes.NameIdentifier
        tvp

    svc
        .AddAuthentication(fun options ->
            options.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
            options.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(fun options ->
            options.Authority <- AuthConfig.authority
            options.Audience <- AuthConfig.audience
            options.TokenValidationParameters <- createTokenValidationParameters ())
    |> ignore

    svc

[<EntryPoint>]
let main args =
    webHost args {
        add_service authService
        logging configureLogging
        use_cors corsPolicyName corsOptions
        use_authentication

        endpoints
            [ get "/api/ping" (Response.ofPlainText "pong")
              get "/api/questions" (fun ctx ->
                  let route = Request.getRoute ctx
                  let month = route.Query.TryGetInt "month"
                  let day = route.Query.TryGetInt "day"
                  printfn "Month: %A, Day: %A" month day

                  match month, day with
                  | Some month, Some day ->
                      Response.ofJson (getQuestionByMonthAndDay (Sql.connect connectionString) month day) ctx
                  | _ -> Response.ofJson (getAllQuestions (Sql.connect connectionString)) ctx)
              post "/api/questions" (handlePostQuestion (Sql.connect connectionString))
              get "/api/questions/{id}" (fun ctx ->
                  let route = Request.getRoute ctx
                  let id = route.GetString "id"
                  (handleGetQuestion (Sql.connect connectionString) id) ctx)
              delete "/api/questions/{id}" (fun ctx ->
                  let route = Request.getRoute ctx
                  let id = route.GetString "id"
                  (handleDeleteQuestion (Sql.connect connectionString) id) ctx)
              get "/api/questions/{id}/answers" (fun ctx ->
                  let route = Request.getRoute ctx
                  let id = route.GetString "id"

                  Request.ifAuthenticated
                      (fun ctx ->
                          handleGetQuestionWithAnswers (Sql.connect connectionString) id ctx.User.Identity.Name ctx)
                      (Response.withStatusCode 401 >> Response.ofPlainText "not authenticated")
                      ctx)
              post "/api/questions/{id}/answers" (fun ctx ->
                  let route = Request.getRoute ctx
                  let id = route.GetString "id"

                  Request.ifAuthenticated
                      (fun ctx -> handlePostAnswer (Sql.connect connectionString) id ctx.User.Identity.Name ctx)
                      (Response.withStatusCode 401 >> Response.ofPlainText "not authenticated")
                      ctx)
              post "/api/users" (handlePostUser (Sql.connect connectionString)) ]
    }

    0
