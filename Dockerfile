# Multi-stage Dockerfile for Enterprise ERP System (.NET 10 LTS / MudBlazor Interactive Server)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for caching layer restore
COPY ["EnterpriseERP.slnx", "./"]
COPY ["src/ERP.Shared/ERP.Shared.csproj", "src/ERP.Shared/"]
COPY ["src/ERP.Domain/ERP.Domain.csproj", "src/ERP.Domain/"]
COPY ["src/ERP.Application/ERP.Application.csproj", "src/ERP.Application/"]
COPY ["src/ERP.Infrastructure/ERP.Infrastructure.csproj", "src/ERP.Infrastructure/"]
COPY ["src/ERP.Web/ERP.Web.csproj", "src/ERP.Web/"]

RUN dotnet restore "src/ERP.Web/ERP.Web.csproj"

# Copy source tree and compile
COPY src/ src/
WORKDIR "/src/src/ERP.Web"
RUN dotnet build "ERP.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ERP.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage using lightweight ASP.NET Core 10 runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Non-root user security best practices
USER $APP_UID

EXPOSE 5000
EXPOSE 5001

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ERP.Web.dll"]

