diagram "JWT vs OAuth 2.0"
  accent "JWT" "OAuth 2.0"
  subtitle "Understanding the difference."
  title middle

panel
  caption "A token format used for authentication.\nSelf-contained and stateless."
  user   U "User"                                 @0,0
  server A "Auth Server\n(Issues JWT)"            @2,0 tint=lavender
  server R "API /\nResource Server\n(Validates JWT)" @2.6,2 tint=mint
  U -> A "Login"
  A -> U "Returns JWT"
  U -> R "Sends JWT in\nAuthorization Header"     route=vh
  A -> R "Validates JWT\n(Signature, Claims, Expiry)" route=vh nudge=10,-20
  R -> U "Returns Response"                       route=vh off=-26 lpos=below
  note "JWT is a token format.\nIt is self-contained (carries claims) and used\nfor stateless authentication between parties."

panel
  caption "A framework for delegated authorization.\nIssues access tokens to access resources."
  user    U "User"                     @0,0
  lock    S "Authorization\nServer"    @1.5,0 tint=sky
  browser C ""                         @3,0
  app     P "Client\n(Application)"    @0.8,2 tint=peach
  server  R "API /\nResource Server\n(Validates Token)" @3,2 tint=mint
  U -> S "Login /\nAuthorize"
  S -> C "User\nConsent"
  S -> P "Issue\nAccess Token"         route=vh
  P -> R "Access API\nwith Access Token"
  R -> P "Returns\nProtected Resource" lpos=below
  C -> R "Validate Token /\nIntrospect (if needed)" #6
  note "OAuth 2.0 is an authorization framework.\nIt allows third-party access to resources\nwithout sharing credentials."

card terminal
  column "JWT (JSON Web Token)"
    - Token format for securely transmitting claims
    - Self-contained (contains user info / claims)
    - Used for authentication
    - No server-side session required
    - Common in stateless APIs
    comment "Example JWT Structure (3 parts)"
    token xxxxx:orange "Header" | yyyyyy:green "Payload\n(Claims)" | zzzzzz:blue "Signature"
  column "OAuth 2.0 (Authorization Framework)"
    - Framework for delegated authorization
    - Issue access tokens to access resources
    - Used for authorization (not authentication)
    - Supports multiple grant types
    - Common for third-party integrations
    comment "Example OAuth 2.0 Flow (Simplified)"
    1. User authorizes the application
    2. Authorization server issues an access token
    3. Application accesses the resource
    4. Resource server validates the token
