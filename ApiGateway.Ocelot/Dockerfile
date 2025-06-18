FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ApiGateway.Ocelot/ApiGateway.Ocelot.csproj", "ApiGateway.Ocelot/"]
RUN dotnet restore "ApiGateway.Ocelot/ApiGateway.Ocelot.csproj"
COPY . .
WORKDIR "/src/ApiGateway.Ocelot"
RUN dotnet build "ApiGateway.Ocelot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ApiGateway.Ocelot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ApiGateway.Ocelot.dll"]