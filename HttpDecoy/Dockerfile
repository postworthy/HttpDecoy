FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 17790
EXPOSE 44312

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY HttpDecoy/HttpDecoy.csproj HttpDecoy/
RUN dotnet restore HttpDecoy/HttpDecoy.csproj
COPY . .
WORKDIR /src/HttpDecoy
RUN dotnet build HttpDecoy.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish HttpDecoy.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "HttpDecoy.dll"]
