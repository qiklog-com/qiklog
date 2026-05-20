# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["src/QikLog.Core/QikLog.Core.csproj", "src/QikLog.Core/"]
COPY ["src/QikLog.Web/QikLog.Web.csproj", "src/QikLog.Web/"]
RUN dotnet restore "src/QikLog.Web/QikLog.Web.csproj"
COPY src/ src/
WORKDIR /src/src/QikLog.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5081
ENV ASPNETCORE_URLS=http://+:5081
ENTRYPOINT ["dotnet", "QikLog.Web.dll"]
