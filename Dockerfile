FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Orizon.Distribuidora.sln", "./"]
COPY ["src/Orizon.Distribuidora.Domain/Orizon.Distribuidora.Domain.csproj", "src/Orizon.Distribuidora.Domain/"]
COPY ["src/Orizon.Distribuidora.Application/Orizon.Distribuidora.Application.csproj", "src/Orizon.Distribuidora.Application/"]
COPY ["src/Orizon.Distribuidora.Infrastructure/Orizon.Distribuidora.Infrastructure.csproj", "src/Orizon.Distribuidora.Infrastructure/"]
COPY ["src/Orizon.Distribuidora.Web/Orizon.Distribuidora.Web.csproj", "src/Orizon.Distribuidora.Web/"]
COPY ["tests/Orizon.Distribuidora.Domain.Tests/Orizon.Distribuidora.Domain.Tests.csproj", "tests/Orizon.Distribuidora.Domain.Tests/"]
COPY ["tests/Orizon.Distribuidora.Application.Tests/Orizon.Distribuidora.Application.Tests.csproj", "tests/Orizon.Distribuidora.Application.Tests/"]

RUN dotnet restore "Orizon.Distribuidora.sln"

COPY . .
RUN dotnet publish "src/Orizon.Distribuidora.Web/Orizon.Distribuidora.Web.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 8080

USER app
ENTRYPOINT ["sh", "-c", "dotnet Orizon.Distribuidora.Web.dll --urls http://0.0.0.0:${PORT:-8080}"]
