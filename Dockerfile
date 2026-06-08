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
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh && mkdir -p /app/uploads
VOLUME ["/app/uploads"]

ENTRYPOINT ["/app/entrypoint.sh"]