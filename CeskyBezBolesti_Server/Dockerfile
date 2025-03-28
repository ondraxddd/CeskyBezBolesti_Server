#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["CeskyBezBolesti_Server/CeskyBezBolesti_Server.csproj", "CeskyBezBolesti_Server/"]
RUN dotnet restore "CeskyBezBolesti_Server/CeskyBezBolesti_Server.csproj"
COPY . .
WORKDIR "/src/CeskyBezBolesti_Server"
RUN dotnet build "CeskyBezBolesti_Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CeskyBezBolesti_Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ./CeskyBezBolesti_Server/Database /app/Database
ENTRYPOINT ["dotnet", "CeskyBezBolesti_Server.dll"]