FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/BffGateway.Application/BffGateway.Application.csproj", "src/BffGateway.Application/"]
COPY ["src/BffGateway.Infrastructure/BffGateway.Infrastructure.csproj", "src/BffGateway.Infrastructure/"]
COPY ["src/BffGateway.WebApi/BffGateway.WebApi.csproj", "src/BffGateway.WebApi/"]
RUN dotnet restore "/src/src/BffGateway.WebApi/BffGateway.WebApi.csproj"
COPY . .
WORKDIR "/src/src/BffGateway.WebApi"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BffGateway.WebApi.dll"]


