# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy solution and project files
COPY *.sln .
COPY src/LinkMeet.API/*.csproj src/LinkMeet.API/
COPY src/LinkMeet.Domain/*.csproj src/LinkMeet.Domain/
COPY src/LinkMeet.Application/*.csproj src/LinkMeet.Application/
COPY src/LinkMeet.Infrastructure/*.csproj src/LinkMeet.Infrastructure/

# Restore dependencies
RUN dotnet restore

# Copy everything else and build
COPY . .
WORKDIR /source/src/LinkMeet.API
RUN dotnet publish -c Release -o /app

# Final Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app .

# Render uses $PORT, ASP.NET uses ASPNETCORE_URLS
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "LinkMeet.API.dll"]
