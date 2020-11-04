#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Lighthouse.Standalone/Lighthouse.Standalone.csproj", "Lighthouse.Standalone/"]
RUN dotnet restore "Lighthouse.Standalone/Lighthouse.Standalone.csproj"
COPY . .
WORKDIR "/src/Lighthouse.Standalone"
RUN dotnet build "Lighthouse.Standalone.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Lighthouse.Standalone.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lighthouse.Standalone.dll"]