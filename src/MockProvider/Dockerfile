FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5101

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/MockProvider/MockProvider.csproj", "src/MockProvider/"]
RUN dotnet restore "/src/src/MockProvider/MockProvider.csproj"
COPY . .
WORKDIR "/src/src/MockProvider"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MockProvider.dll"]


