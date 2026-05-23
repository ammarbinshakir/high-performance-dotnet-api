FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY HighPerformanceDotNetApi.slnx ./
COPY src/HighPerformanceDotNetApi.Domain/HighPerformanceDotNetApi.Domain.csproj src/HighPerformanceDotNetApi.Domain/
COPY src/HighPerformanceDotNetApi.Application/HighPerformanceDotNetApi.Application.csproj src/HighPerformanceDotNetApi.Application/
COPY src/HighPerformanceDotNetApi.Infrastructure/HighPerformanceDotNetApi.Infrastructure.csproj src/HighPerformanceDotNetApi.Infrastructure/
COPY src/HighPerformanceDotNetApi.Api/HighPerformanceDotNetApi.Api.csproj src/HighPerformanceDotNetApi.Api/
RUN dotnet restore src/HighPerformanceDotNetApi.Api/HighPerformanceDotNetApi.Api.csproj

COPY . .
RUN dotnet publish src/HighPerformanceDotNetApi.Api/HighPerformanceDotNetApi.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HighPerformanceDotNetApi.Api.dll"]
