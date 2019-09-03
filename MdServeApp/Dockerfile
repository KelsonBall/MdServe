FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["MdServeApp.csproj", ""]
RUN dotnet restore "./MdServeApp.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "MdServeApp.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "MdServeApp.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MdServeApp.dll"]