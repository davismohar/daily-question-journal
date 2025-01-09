module DailyJournal.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open Npgsql.FSharp
open System
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.Extensions.DependencyInjection
open Microsoft.IdentityModel.Tokens
open System.Security.Claims

let corsPolicyName = "corsPolicy"

let corsPolicy (policyBuilder: CorsPolicyBuilder) =
    policyBuilder
        .AllowAnyMethod()
        .WithHeaders("Authorization", "Content-Type", "Accept")
        .WithOrigins("http://localhost:5173", "https://daily-question-journal.vercel.app")
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
      email: string
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
      role: string
      subject: string
      email: string
      given_name: string
      name: string
      picture: string
      nickname: string }

type AddAnswerSharingSubjectRequest = { subjectEmail: string }

let getUserBySubject (connection: Sql.SqlProps) (subject: string) : User option =
    connection
    |> Sql.query
        "SELECT id, role, subject, email, given_name, name, picture, nickname FROM users WHERE subject = @subject"
    |> Sql.parameters [ "subject", Sql.string subject ]
    |> Sql.executeRow (fun read ->
        { id = read.uuid "id"
          role = read.string "role"
          subject = read.string "subject"
          email = read.string "email"
          given_name = read.string "given_name"
          name = read.string "name"
          picture = read.string "picture"
          nickname = read.string "nickname" })
    |> Option.ofObj

let getUserByEmail (connection: Sql.SqlProps) (email: string) : User option =
    connection
    |> Sql.query "SELECT id, role, subject, email, given_name, name, picture, nickname FROM users WHERE email = @email"
    |> Sql.parameters [ "email", Sql.string email ]
    |> Sql.execute (fun read ->
        { id = read.uuid "id"
          role = read.string "role"
          subject = read.string "subject"
          email = read.string "email"
          given_name = read.string "given_name"
          name = read.string "name"
          picture = read.string "picture"
          nickname = read.string "nickname" })
    |> List.tryHead

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

let isAdmin (connection: Sql.SqlProps) (subject: string) : bool =
    match getUserBySubject connection subject with
    | Some user -> user.role = "admin"
    | None -> false

let createQuestion (connection: Sql.SqlProps) (question: QuestionRequest) =
    insertQuestion connection question.question

let handlePostQuestion (connection: Sql.SqlProps) (subject: string) =
    if (isAdmin connection subject) then
        Request.mapJson (fun question ->
            let newQuestion = createQuestion connection question
            Response.ofJson newQuestion)
    else
        Response.withStatusCode 401 >> Response.ofPlainText "Unauthorized"

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

let getAnswersByQuestionIdAndUserId (connection: Sql.SqlProps) (id: Guid) (userId: Guid) : Answer list =
    connection
    |> Sql.query
        """
    SELECT a.id, a.answer, u.email, a.created_at
    FROM answers a 
    LEFT JOIN users u on a.user_id = u.id
    WHERE question_id = @id 
    AND (user_id = @userId) OR (user_id IN (SELECT share_subject FROM answer_sharing_relationships WHERE share_granter = @userId))
    """
    |> Sql.parameters [ "id", Sql.uuid id; "userId", Sql.uuid userId ]
    |> Sql.execute (fun read ->
        { id = read.uuid "id"
          email = read.string "email"
          answer = read.string "answer"
          created_at = read.dateTime "created_at" })

let getAnswersByQuestionIdAndUserIdList (connection: Sql.SqlProps) (id: Guid) (userIds: Guid list) : Answer list =
    connection
    |> Sql.query
        """
    SELECT a.id, a.answer, u.email, a.created_at
    FROM answers a 
    LEFT JOIN users u on a.user_id = u.id
    WHERE 
    question_id = @id 
    AND user_id IN @userIds"""
    |> Sql.parameters [ "id", Sql.uuid id; "userIds", Sql.uuidArray (userIds |> Array.ofList) ]
    |> Sql.execute (fun read ->
        { id = read.uuid "id"
          email = read.string "email"
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



let handleDeleteQuestion (connection: Sql.SqlProps) (id: string) (subject: string) : HttpHandler =
    if not (isAdmin connection subject) then
        Response.withStatusCode 401 >> Response.ofPlainText "Unauthorized"
    else
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
        """
        with inserted_answer as (
            INSERT INTO answers (question_id, user_id, answer) VALUES (@questionId, @userId, @answer) RETURNING *
        )
        SELECT a.id, a.answer, u.email, a.created_at
        FROM inserted_answer a
        LEFT JOIN users u on a.user_id = u.id
        ;
        """
    |> Sql.parameters
        [ "questionId", Sql.uuid questionId
          "answer", Sql.string answer.answer
          "userId", Sql.uuid userId ]
    |> Sql.executeRow (fun read ->
        { id = read.uuid "id"
          email = read.string "email"
          answer = read.string "answer"
          created_at = read.dateTime "created_at" })


let handlePostAnswer (connection: Sql.SqlProps) (questionId: string) (subject: string) : HttpHandler =
    Request.mapJson (fun answer ->
        let questionId = Guid.Parse questionId

        let userId =
            getUserBySubject connection subject |> Option.get |> (fun user -> user.id)

        let newAnswer = insertAnswer connection questionId answer userId
        Response.ofJson newAnswer)

let insertUser (connection: Sql.SqlProps) (user: UserRegistrationRequest) : User =
    // First check if user exists by email
    let existingUser =
        connection
        |> Sql.query "SELECT id, subject, email, given_name, name, picture, nickname FROM users WHERE email = @email"
        |> Sql.parameters [ "email", Sql.string user.email ]
        |> Sql.execute (fun read ->
            { id = read.uuid "id"
              role = read.string "role"
              subject = read.string "subject"
              email = read.string "email"
              given_name = read.string "given_name"
              name = read.string "name"
              picture = read.string "picture"
              nickname = read.string "nickname" })
        |> List.tryHead

    match existingUser with
    | Some user -> user // Return existing user if found
    | None -> // Insert new user if not found
        connection
        |> Sql.query
            "INSERT INTO users (subject, role,  email, given_name, name, picture, nickname) VALUES (@subject, @role, @email, @given_name, @name, @picture, @nickname) RETURNING id, subject, role, email, given_name, name, picture, nickname"
        |> Sql.parameters
            [ "subject", Sql.string user.subject
              "role", Sql.string "user"
              "email", Sql.string user.email
              "given_name", Sql.string user.given_name
              "name", Sql.string user.name
              "picture", Sql.string user.picture
              "nickname", Sql.string user.nickname ]
        |> Sql.executeRow (fun read ->
            { id = read.uuid "id"
              subject = read.string "subject"
              role = "user"
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

let insertAnswerSharingRelationship (connection: Sql.SqlProps) (granterUserId: Guid) (subjectUserId: Guid) =
    connection
    |> Sql.query
        "INSERT INTO answer_sharing_relationships (share_granter, share_subject) VALUES (@granterUserId, @subjectUserId)"
    |> Sql.parameters
        [ "granterUserId", Sql.uuid granterUserId
          "subjectUserId", Sql.uuid subjectUserId ]
    |> Sql.executeNonQuery

let getAnswerSharingRelationships (connection: Sql.SqlProps) (granterUserId: Guid) : User list =
    connection
    |> Sql.query
        """
    SELECT u.*
    FROM answer_sharing_relationships asr
    INNER JOIN users u ON asr.share_subject = u.id
    WHERE share_granter = @granterUserId
    """
    |> Sql.parameters [ "granterUserId", Sql.uuid granterUserId ]
    |> Sql.execute (fun read ->
        { id = read.uuid "id"
          subject = read.string "subject"
          role = "user"
          email = read.string "email"
          given_name = read.string "given_name"
          name = read.string "name"
          picture = read.string "picture"
          nickname = read.string "nickname" })


let handlePutSharedAnswers (connection: Sql.SqlProps) (subject: string) : HttpHandler =
    Request.mapJson (fun request ->
        let subjectEmail = request.subjectEmail
        printfn "SubjectEmail: %A" subjectEmail

        let granterUser = getUserBySubject connection subject |> Option.get
        let subjectUser = getUserByEmail connection subjectEmail

        match subjectUser with
        | Some subjectUser ->
            printfn "Subject: %A, SubjectEmail: %A" granterUser.id subjectUser.id

            insertAnswerSharingRelationship connection granterUser.id subjectUser.id
            |> ignore

            let answerSharingRelationships =
                getAnswerSharingRelationships connection granterUser.id

            let sharedEmails = answerSharingRelationships |> List.map (fun u -> u.email)

            Response.ofJson sharedEmails
        | None -> Response.withStatusCode 404 >> Response.ofPlainText "Subject not found")


let handleGetQuestionWithAnswers (connection: Sql.SqlProps) (questionId: string) (subject: string) : HttpHandler =
    match Guid.TryParse questionId with
    | true, guid ->
        let question = getQuestionById connection guid

        match getUserBySubject connection subject with
        | Some user ->
            let userId = user.id

            // let sharedUserIds =
            //     getAnswerSharingRelationships connection userId |> List.map (fun u -> u.id)

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
        | None -> Response.withStatusCode 404 >> Response.ofPlainText "User not found"
    | false, _ -> Response.withStatusCode 400 >> Response.ofPlainText "Invalid ID format"




let getSharedEmailsByUserEmail connection userEmail =
    // Implement the database query to get the shared emails
    // This is a placeholder implementation
    let query = "SELECT shared_email FROM shared_answers WHERE user_email = @userEmail"

    connection
    |> Sql.query query
    |> Sql.parameters [ "userEmail", Sql.string userEmail ]
    |> Sql.execute (fun read -> read.string "shared_email")

let configureLogging (log: ILoggingBuilder) =
    log.ClearProviders() |> ignore
    log.AddConsole() |> ignore
    log.AddDebug() |> ignore
    log

module AuthConfig =
    let authority = "https://dev-spr842pm040mf5yw.us.auth0.com/"
    let audience = "https/dailyjournal"
    let writeAnswersPolicy = "write:answers"

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
              post "/api/questions" (fun ctx ->
                  Request.ifAuthenticated
                      (fun authCtx ->
                          handlePostQuestion (Sql.connect connectionString) authCtx.User.Identity.Name authCtx)
                      (Response.withStatusCode 401 >> Response.ofPlainText "not authenticated")
                      ctx)
              get "/api/questions/{id}" (fun ctx ->
                  let route = Request.getRoute ctx
                  let id = route.GetString "id"
                  (handleGetQuestion (Sql.connect connectionString) id) ctx)
              delete "/api/questions/{id}" (fun ctx ->
                  let route = Request.getRoute ctx
                  let id = route.GetString "id"

                  Request.ifAuthenticated
                      (fun authCtx ->
                          (handleDeleteQuestion (Sql.connect connectionString) id authCtx.User.Identity.Name) authCtx)
                      (Response.withStatusCode 401 >> Response.ofPlainText "not authenticated")
                      ctx)
              get "/api/questions/{id}/answers" (fun ctx ->
                  let route = Request.getRoute ctx
                  let id = route.GetString "id"

                  Request.ifAuthenticated
                      (fun authCtx ->
                          handleGetQuestionWithAnswers
                              (Sql.connect connectionString)
                              id
                              authCtx.User.Identity.Name
                              authCtx)
                      (Response.withStatusCode 401 >> Response.ofPlainText "not authenticated")
                      ctx)
              post "/api/questions/{id}/answers" (fun ctx ->
                  let route = Request.getRoute ctx
                  let id = route.GetString "id"

                  Request.ifAuthenticated
                      (fun authCtx ->
                          handlePostAnswer (Sql.connect connectionString) id authCtx.User.Identity.Name authCtx)
                      (Response.withStatusCode 401 >> Response.ofPlainText "not authenticated")
                      ctx)

              post "/api/users" (handlePostUser (Sql.connect connectionString))

              get "/api/shared_answers" (fun ctx ->
                  Request.ifAuthenticated
                      (fun authCtx ->
                          let connection = Sql.connect connectionString
                          let authedSubject = authCtx.User.Identity.Name

                          let userId =
                              getUserBySubject connection authedSubject |> Option.get |> (fun user -> user.id)

                          let sharedEmails =
                              getAnswerSharingRelationships connection userId |> List.map (fun u -> u.email)

                          Response.ofJson sharedEmails authCtx)
                      (Response.withStatusCode 401 >> Response.ofPlainText "not authenticated")
                      ctx)

              put "/api/shared_answers" (fun ctx ->
                  let route = Request.getRoute ctx

                  Request.ifAuthenticated
                      (fun authCtx ->
                          handlePutSharedAnswers (Sql.connect connectionString) authCtx.User.Identity.Name authCtx)
                      (Response.withStatusCode 401 >> Response.ofPlainText "not authenticated")
                      ctx) ]
    }

    0
