FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/FieldOps.Domain/FieldOps.Domain.csproj src/FieldOps.Domain/
COPY src/FieldOps.Application/FieldOps.Application.csproj src/FieldOps.Application/
COPY src/FieldOps.Infrastructure/FieldOps.Infrastructure.csproj src/FieldOps.Infrastructure/
COPY src/FieldOps.Api/FieldOps.Api.csproj src/FieldOps.Api/
RUN dotnet restore src/FieldOps.Api/FieldOps.Api.csproj

COPY src/ src/
RUN dotnet publish src/FieldOps.Api/FieldOps.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "FieldOps.Api.dll"]

