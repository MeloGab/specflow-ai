FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY global.json Directory.Build.props SpecFlowAI.slnx ./
COPY src/SpecFlowAI.Domain/SpecFlowAI.Domain.csproj src/SpecFlowAI.Domain/
COPY src/SpecFlowAI.Application/SpecFlowAI.Application.csproj src/SpecFlowAI.Application/
COPY src/SpecFlowAI.Infrastructure/SpecFlowAI.Infrastructure.csproj src/SpecFlowAI.Infrastructure/
COPY src/SpecFlowAI.Api/SpecFlowAI.Api.csproj src/SpecFlowAI.Api/
RUN dotnet restore src/SpecFlowAI.Api/SpecFlowAI.Api.csproj

COPY src/ src/
RUN dotnet publish src/SpecFlowAI.Api/SpecFlowAI.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "SpecFlowAI.Api.dll"]
