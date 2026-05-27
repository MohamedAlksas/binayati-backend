FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish -c Release -f net8.0 -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000
CMD /bin/bash -c 'dotnet BinayatiBackend.dll 2>&1; echo "DOTNET_EXIT_CODE=$?"; echo "Container will sleep for 60s..."; sleep 60'
