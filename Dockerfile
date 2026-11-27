# Local development Dockerfile — multi-stage build
# Production deployment config (IAM, Secrets Manager, ECR promo, signing) is out of scope

# R4: Base images pinned by digest (linux/amd64). Evidence: devops-out/r4-dotnet-base-digest-2026-10-05.md
# sdk 10.0-noble = 10.0.401 / runtime 10.0.12 (≥10.0.9 PASS)
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble@sha256:0eeb52c76e35a5431ca707ad2bc75e38006a05393045d8532ae44c15d9474523 AS build
WORKDIR /src

COPY Dealoware.sln ./
COPY src/Dealoware.Api/Dealoware.Api.csproj src/Dealoware.Api/
COPY src/Dealoware.Domain/Dealoware.Domain.csproj src/Dealoware.Domain/
COPY src/Dealoware.Application/Dealoware.Application.csproj src/Dealoware.Application/
COPY src/Dealoware.Infrastructure/Dealoware.Infrastructure.csproj src/Dealoware.Infrastructure/
RUN dotnet restore src/Dealoware.Api/Dealoware.Api.csproj

COPY src/ src/
# The commit this image is built from (docker build --build-arg GIT_SHA=<full commit hash>).
# It is built into the app (ReleaseVersion): both health answers give its first 7
# characters. A build without it answers "dev". Declared after the restore, so a new
# commit does not throw away the restore layer.
ARG GIT_SHA=dev
RUN echo "$GIT_SHA" | grep -Eq '^(dev|[0-9a-f]{7,40})$' \
 || { echo "GIT_SHA must be dev or a lower-case hex commit hash" >&2; exit 1; }
RUN dotnet publish src/Dealoware.Api/Dealoware.Api.csproj -c Release -o /app/publish --no-restore "-p:DealowareGitSha=$GIT_SHA"

# aspnet 10.0-noble = runtime 10.0.12 (≥10.0.9 PASS)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble@sha256:0fa044f682cb7d93a5a90401a00c626c66f7b00b86922be9441f869eae039f80 AS runtime
ARG GIT_SHA=dev
LABEL org.opencontainers.image.revision=$GIT_SHA
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .

# AWS RDS global CA bundle for Postgres TLS VerifyFull.
# Source: https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem
# This is public trust material, not a secret. Mode 0444 = read-only.
COPY --chmod=0444 certs/rds-global-bundle.pem /app/certs/rds-global-bundle.pem
RUN echo "fe45bbebf92ad3e27a583bbb2ddd1553c521ed4d49af5514dc0a40372ea5395c  /app/certs/rds-global-bundle.pem" | sha256sum -c - \
 && grep -q "BEGIN CERTIFICATE" /app/certs/rds-global-bundle.pem

ENTRYPOINT ["dotnet", "Dealoware.Api.dll"]
