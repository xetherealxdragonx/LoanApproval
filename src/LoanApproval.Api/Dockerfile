FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/LoanApproval.Api/LoanApproval.Api.csproj", "src/LoanApproval.Api/"]
COPY ["src/LoanApproval.Application/LoanApproval.Application.csproj", "src/LoanApproval.Application/"]
COPY ["src/LoanApproval.Infrastructure/LoanApproval.Infrastructure.csproj", "src/LoanApproval.Infrastructure/"]
COPY ["src/LoanApproval.Domain/LoanApproval.Domain.csproj", "src/LoanApproval.Domain/"]
RUN dotnet restore "src/LoanApproval.Api/LoanApproval.Api.csproj"

COPY . .
WORKDIR "/src/src/LoanApproval.Api"
RUN dotnet publish "LoanApproval.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LoanApproval.Api.dll"]
