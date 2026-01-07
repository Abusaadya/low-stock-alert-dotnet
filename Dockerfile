# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["SallaAlertApp.Api/SallaAlertApp.Api.csproj", "SallaAlertApp.Api/"]
RUN dotnet restore "SallaAlertApp.Api/SallaAlertApp.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/SallaAlertApp.Api"
RUN dotnet build "SallaAlertApp.Api.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "SallaAlertApp.Api.csproj" -c Release -o /app/publish

# Final Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN apt-get update && apt-get install -y libgssapi-krb5-2

WORKDIR /app
COPY --from=publish /app/publish .

# Configure Port (Railway passes PORT env var)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SallaAlertApp.Api.dll"]
