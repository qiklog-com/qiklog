# QikLog on Azure (planned). Render with: make diagrams
diagram "QikLog on Azure"
  subtitle "From your app's ILogger to your browser, live."
  view iso
  theme ink

# Outside Azure: where logs come from, who watches, how code ships
laptop  app  "Your .NET app\nSerilog · ILogger · CLI"  @-0.7,0
user    you  "You"                                     @7.2,0
ci      gh   "GitHub Actions\nOIDC deploy"             @0,2.6
lock    zit  "Zitadel\nsign-in (OIDC)"                 @7.2,2.6

# Azure
group azure "Azure"                        @2,0 to 5,2.2 pad=0.85 tint=sky slab
group env   "Container Apps"   @2,0 to 5,0 pad=0.6 tint=teal loff=1.1

container api "qiklog-api\nIngest API + SignalR hub"  @2,0 tint=teal badge="scales to zero"
container web "qiklog-web\nBlazor dashboard"           @5,0 tint=teal badge="always on"
registry  acr "Container Registry"                     @2,2.2 tint=sky
db        pg  "PostgreSQL\nFlexible Server"            @3.5,2.2 tint=sky
vault     kv  "Key Vault\nmanaged identity"            @5,2.2 tint=sky

# The data path (numbered, animated)
app -> api "HTTPS POST /v1/logs"      flow at=0.68
api -> pg  "store"                    flow
api -> web "live tail · SignalR"      flow
web -> you "app.qiklog.com"           flow at=0.62

# Supporting paths
web -> pg                    nonum dashed
web -> zit "sign in"         nonum dashed
gh  -> acr "push images"     nonum dashed
acr -> env "pull"            nonum dashed
kv  -> env "secrets"         nonum dashed
