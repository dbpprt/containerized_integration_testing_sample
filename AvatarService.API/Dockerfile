FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["AvatarService.API/AvatarService.API.csproj", "AvatarService.API/"]
RUN dotnet restore "AvatarService.API/AvatarService.API.csproj"
COPY . .
WORKDIR "/src/AvatarService.API"
RUN dotnet build "AvatarService.API.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "AvatarService.API.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "AvatarService.API.dll"]