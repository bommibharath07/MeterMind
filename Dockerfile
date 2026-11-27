FROM mcr.microsoft.com/dotnet/sdk:10.0.404 AS build
WORKDIR /source
COPY global.json Directory.Build.props MeterMind.slnx dotnet-tools.json ./
COPY src/ src/
ARG PROJECT=API
RUN dotnet restore src/MeterMind.${PROJECT}/MeterMind.${PROJECT}.csproj --locked-mode
RUN dotnet publish src/MeterMind.${PROJECT}/MeterMind.${PROJECT}.csproj -c Release --no-restore -o /out
RUN printf '#!/bin/sh\nexec dotnet /app/MeterMind.%s.dll "$@"\n' "$PROJECT" > /out/entrypoint.sh && chmod +x /out/entrypoint.sh
FROM mcr.microsoft.com/dotnet/aspnet:10.0.15 AS runtime
WORKDIR /app
COPY --from=build /out/ ./
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["/app/entrypoint.sh"]
