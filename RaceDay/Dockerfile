FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["RaceDay/RaceDay.csproj", "RaceDay/"]
RUN dotnet restore "RaceDay/RaceDay.csproj"
COPY . .
WORKDIR "/src/RaceDay"
RUN dotnet publish "RaceDay.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.dll"]
