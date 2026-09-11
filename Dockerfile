# Local development Dockerfile — multi-stage build
# Production deployment config (IAM, Secrets Manager, ECR promo, signing) is out of scope

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Dealoware.sln ./
COPY src/Dealoware.Api/Dealoware.Api.csproj src/Dealoware.Api/
COPY src/Dealoware.Domain/Dealoware.Domain.csproj src/Dealoware.Domain/
COPY src/Dealoware.Application/Dealoware.Application.csproj src/Dealoware.Application/
COPY src/Dealoware.Infrastructure/Dealoware.Infrastructure.csproj src/Dealoware.Infrastructure/
RUN dotnet restore Dealoware.sln

COPY src/ src/
RUN dotnet publish src/Dealoware.Api/Dealoware.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Dealoware.Api.dll"]
