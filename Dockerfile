FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["spotify-swiss-knife/spotify-swiss-knife.csproj", "spotify-swiss-knife/"]
RUN dotnet restore "spotify-swiss-knife/spotify-swiss-knife.csproj"

COPY spotify-swiss-knife/ spotify-swiss-knife/
WORKDIR /src/spotify-swiss-knife
RUN dotnet publish "spotify-swiss-knife.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["sh", "-c", "if [ -z \"${ConnectionStrings__SpotifyDbContext:-}\" ] && [ -n \"${DB_HOST:-}\" ] && [ -n \"${DB_USER:-}\" ] && [ -n \"${DB_PASSWORD:-}\" ]; then export ConnectionStrings__SpotifyDbContext=\"Host=${DB_HOST};Port=${DB_PORT:-5432};Database=${DB_NAME:-ssk};Username=${DB_USER};Password=${DB_PASSWORD};SSL Mode=${DB_SSL_MODE:-Require};Trust Server Certificate=${DB_TRUST_SERVER_CERTIFICATE:-true}\"; fi; exec dotnet spotify-swiss-knife.dll"]