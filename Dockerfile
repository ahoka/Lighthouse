FROM smallstep/step-cli:0.14.4 AS step-cli

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Lighthouse/Lighthouse.csproj", "Lighthouse/"]
RUN dotnet restore "Lighthouse/Lighthouse.csproj"
COPY . .
WORKDIR "/src/Lighthouse"
RUN dotnet build "Lighthouse.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Lighthouse.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=step-cli /usr/local/bin/step /usr/local/bin/
COPY --from=publish /app/publish .
COPY entrypoint.sh /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
