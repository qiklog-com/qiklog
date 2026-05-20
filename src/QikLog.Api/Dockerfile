# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["src/QikLog.Core/QikLog.Core.csproj", "src/QikLog.Core/"]
COPY ["src/QikLog.Api/QikLog.Api.csproj", "src/QikLog.Api/"]
RUN dotnet restore "src/QikLog.Api/QikLog.Api.csproj"
COPY src/ src/
WORKDIR /src/src/QikLog.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5080
ENV ASPNETCORE_URLS=http://+:5080
ENTRYPOINT ["dotnet", "QikLog.Api.dll"]
