# ─── Збірка ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "Contact.API.csproj"
RUN dotnet publish "Contact.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ─── Запуск ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# libfontconfig1 потрібен QuestPDF (SkiaSharp) для генерації PDF на Linux.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "Contact.API.dll"]
