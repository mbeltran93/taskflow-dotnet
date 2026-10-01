# --- Build stage ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy csproj files first and restore, so Docker can cache the (slow) restore layer
# independently of source code changes.
COPY TaskFlow.slnx ./
COPY src/TaskFlow.Domain/TaskFlow.Domain.csproj src/TaskFlow.Domain/
COPY src/TaskFlow.Application/TaskFlow.Application.csproj src/TaskFlow.Application/
COPY src/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj src/TaskFlow.Infrastructure/
COPY src/TaskFlow.Api/TaskFlow.Api.csproj src/TaskFlow.Api/
COPY tests/TaskFlow.UnitTests/TaskFlow.UnitTests.csproj tests/TaskFlow.UnitTests/
COPY tests/TaskFlow.IntegrationTests/TaskFlow.IntegrationTests.csproj tests/TaskFlow.IntegrationTests/

RUN dotnet restore src/TaskFlow.Api/TaskFlow.Api.csproj

COPY . .
RUN dotnet publish src/TaskFlow.Api/TaskFlow.Api.csproj -c Release -o /app --no-restore

# --- Runtime stage ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# krb5-libs provides libgssapi_krb5.so.2, which Npgsql probes for GSSAPI auth support; without
# it the app still works, but logs a noisy "Cannot load library" warning on every startup.
RUN apk add --no-cache krb5-libs \
    && addgroup -S taskflow && adduser -S taskflow -G taskflow
COPY --from=build /app .
RUN chown -R taskflow:taskflow /app
USER taskflow

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TaskFlow.Api.dll"]
