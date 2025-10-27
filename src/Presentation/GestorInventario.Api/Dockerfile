FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --create-home --shell /usr/sbin/nologin appuser

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Presentation/GestorInventario.Api/GestorInventario.Api.csproj", "src/Presentation/GestorInventario.Api/"]
COPY ["src/Application/GestorInventario.Application/GestorInventario.Application.csproj", "src/Application/GestorInventario.Application/"]
COPY ["src/Core/GestorInventario.Domain/GestorInventario.Domain.csproj", "src/Core/GestorInventario.Domain/"]
COPY ["src/Infrastructure/GestorInventario.Infrastructure/GestorInventario.Infrastructure.csproj", "src/Infrastructure/GestorInventario.Infrastructure/"]
RUN dotnet restore "src/Presentation/GestorInventario.Api/GestorInventario.Api.csproj"
COPY . .
WORKDIR "/src/src/Presentation/GestorInventario.Api"
RUN dotnet publish "GestorInventario.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN chown -R appuser:appuser /app
USER appuser
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 CMD curl --fail http://127.0.0.1:8080/ || exit 1
ENTRYPOINT ["dotnet", "GestorInventario.Api.dll"]
