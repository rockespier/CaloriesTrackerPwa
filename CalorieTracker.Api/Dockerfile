# Etapa 1: Construcción (SDK pesado)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar archivos de solución y proyectos para restaurar dependencias
COPY ["CalorieTracker.Api/CalorieTracker.Api.csproj", "CalorieTracker.Api/"]
COPY ["CalorieTracker.Application/CalorieTracker.Application.csproj", "CalorieTracker.Application/"]
COPY ["CalorieTracker.Domain/CalorieTracker.Domain.csproj", "CalorieTracker.Domain/"]
COPY ["CalorieTracker.Infrastructure/CalorieTracker.Infrastructure.csproj", "CalorieTracker.Infrastructure/"]
RUN dotnet restore "CalorieTracker.Api/CalorieTracker.Api.csproj"

# Copiar el resto del código y compilar
COPY . .
WORKDIR "/src/CalorieTracker.Api"
RUN dotnet build "CalorieTracker.Api.csproj" -c Release -o /app/build

# Etapa 2: Publicación
FROM build AS publish
RUN dotnet publish "CalorieTracker.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 3: Producción (Runtime ligero)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CalorieTracker.Api.dll"]