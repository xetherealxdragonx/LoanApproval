# Three-stage build: the React front end, then the .NET publish, then a
# runtime-only image. No Node process exists at runtime - stage 1 exists purely
# to produce static assets that the API serves from wwwroot.

# --- Stage 1: build the React front end --------------------------------------
FROM node:22-alpine AS frontend

# web/vite.config.ts writes its output to ../src/LoanApproval.Api/wwwroot, so the
# directory layout here mirrors the repository for that relative path to resolve.
WORKDIR /source/web

# Manifests first, so the npm layer stays cached until dependencies actually change.
COPY web/package.json web/package-lock.json ./
RUN npm ci

COPY web/ ./
RUN npm run build

# --- Stage 2: build and publish the API --------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/LoanApproval.Api/LoanApproval.Api.csproj", "src/LoanApproval.Api/"]
COPY ["src/LoanApproval.Application/LoanApproval.Application.csproj", "src/LoanApproval.Application/"]
COPY ["src/LoanApproval.Infrastructure/LoanApproval.Infrastructure.csproj", "src/LoanApproval.Infrastructure/"]
COPY ["src/LoanApproval.Domain/LoanApproval.Domain.csproj", "src/LoanApproval.Domain/"]
RUN dotnet restore "src/LoanApproval.Api/LoanApproval.Api.csproj"

COPY . .

# Copied after `COPY . .` so it wins over any stale wwwroot in the build context,
# and before publish because the Web SDK includes wwwroot in publish output -
# that is what makes the published app serve the SPA.
COPY --from=frontend /source/src/LoanApproval.Api/wwwroot ./src/LoanApproval.Api/wwwroot

WORKDIR "/src/src/LoanApproval.Api"
RUN dotnet publish "LoanApproval.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Stage 3: runtime ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LoanApproval.Api.dll"]
