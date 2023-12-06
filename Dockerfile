#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["TotsChallenge.csproj", "."]
COPY ["TotsChallenge.xml", "."]
RUN dotnet restore "./TotsChallenge.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "TotsChallenge.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TotsChallenge.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TotsChallenge.dll"]