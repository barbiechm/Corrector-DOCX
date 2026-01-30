# 1. Usar la imagen de .NET SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar el archivo de proyecto y restaurar dependencias
COPY ["COMPARARNOTAS.csproj", "./"]
RUN dotnet restore "COMPARARNOTAS.csproj"

# Copiar el resto del código y compilar
COPY . .
RUN dotnet publish "COMPARARNOTAS.csproj" -c Release -o /app/publish

# 2. Usar la imagen de .NET Runtime para ejecutar (más ligera)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Configurar el puerto que Render espera (Render usa la variable PORT o 8080 por defecto)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "COMPARARNOTAS.dll"]