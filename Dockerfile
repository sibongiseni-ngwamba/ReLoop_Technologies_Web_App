FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ReLoop_Technologies_Web_App.csproj", "./"]
RUN dotnet restore "ReLoop_Technologies_Web_App.csproj"

COPY . .
RUN dotnet publish "ReLoop_Technologies_Web_App.csproj" \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ReLoop_Technologies_Web_App.dll"]
