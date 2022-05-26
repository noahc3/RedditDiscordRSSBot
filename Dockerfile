FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /src/
COPY . ./
RUN dotnet restore RedditDiscordRSSBot.sln && \
    dotnet publish -c Release -o out


FROM mcr.microsoft.com/dotnet/runtime:3.1

WORKDIR /app/
COPY --from=build /src/out ./
COPY config.json ./

CMD ["dotnet", "RedditDiscordRSSBot.dll"]
