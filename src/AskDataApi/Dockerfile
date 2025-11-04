# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1) copy project file and restore
COPY ./AskDataApi.csproj ./
RUN dotnet restore

# 2) copy the rest and publish
COPY . .
RUN dotnet publish -c Release -o /out

# --- Run stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out ./

# Zeabur gives a dynamic PORT; bind HTTP to it
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
EXPOSE 8080

ENTRYPOINT ["dotnet","AskDataApi.dll"]
