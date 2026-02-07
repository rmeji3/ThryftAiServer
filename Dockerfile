# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["ThryftAiServer.csproj", "./"]
RUN dotnet restore "ThryftAiServer.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/."
RUN dotnet build "ThryftAiServer.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "ThryftAiServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose the port the app runs on
EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "ThryftAiServer.dll"]
