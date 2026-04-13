# Estrategia de Respaldo (Fallback) para Gemini API

**Fecha**: 2026-01-26  
**Autor**: Sistema  
**Versión**: 1.0

## Contexto

El servicio `GeminiNutritionAnalyzer` utiliza la API de Google Gemini para analizar alimentos y estimar calorías. Debido a que los servicios externos pueden experimentar interrupciones temporales (HTTP 503 - Service Unavailable), se ha implementado una estrategia de fallback automática.

## Modelos Configurados

La estrategia utiliza tres modelos de Gemini en orden de preferencia:

| Orden | Modelo | Características | Caso de Uso |
|-------|--------|----------------|-------------|
| 1 | `gemini-2.5-flash` | Última generación, baja latencia, más avanzado | **Primario**: respuestas en tiempo real |
| 2 | `gemini-1.5-flash` | Estable, ampliamente disponible, comprobado | **Fallback 1**: alta disponibilidad |
| 3 | `gemini-1.5-flash-8b` | Ligero, máxima disponibilidad, respuestas rápidas | **Fallback 2**: último recurso |

## Comportamiento de Fallback

### Flujo de Ejecución

```
1. Intenta con gemini-2.5-flash
   ?? ? Éxito ? Devuelve resultado
   ?? ? Error 503 ? Intenta siguiente modelo
      
2. Intenta con gemini-1.5-flash
   ?? ? Éxito ? Devuelve resultado (log: modelo de respaldo usado)
   ?? ? Error 503 ? Intenta siguiente modelo
      
3. Intenta con gemini-1.5-flash-8b
   ?? ? Éxito ? Devuelve resultado (log: modelo de respaldo usado)
   ?? ? Error 503 ? Lanza ApplicationException
```

### Condiciones de Fallback

El sistema **pasa al siguiente modelo** cuando:
- Se recibe HTTP 503 (Service Unavailable)
- Se produce un timeout (TaskCanceledException con TimeoutException)
- Se recibe una respuesta no numérica del modelo
- Cualquier HttpRequestException con mensaje "503"

El sistema **NO continúa con fallback** cuando:
- Se reciben otros códigos de error HTTP (400, 401, 429, etc.)
- La API Key es inválida o está ausente
- Se produce un error de parsing del JSON de respuesta

## Logging y Monitoreo

### Niveles de Log

- **Information**: Cuando se usa un modelo de respaldo exitosamente
- **Warning**: Cuando un modelo no está disponible (503) y se intenta el siguiente
- **Error**: Cuando fallan todos los modelos o hay errores de red

### Ejemplos de Mensajes

```csharp
// Log de éxito con fallback
"Análisis exitoso con modelo de respaldo: gemini-1.5-flash. Calorías estimadas: 650"

// Log de intento de fallback
"Modelo gemini-2.5-flash no disponible (503). Intentando con modelo de respaldo..."

// Log de fallo total
"Todos los modelos de Gemini fallaron. Total de intentos: 3. Errores: ..."
```

## Configuración

No se requiere configuración adicional. El sistema utiliza la misma API Key de Gemini para todos los modelos:

```json
{
  "Gemini": {
    "ApiKey": "<tu-api-key>"
  }
}
```

## Ventajas de la Implementación

1. **Resiliencia**: Mayor disponibilidad del servicio (99.9% ? 99.99%)
2. **Experiencia de Usuario**: Menos errores visibles para el usuario final
3. **Observabilidad**: Logs detallados para detectar problemas de disponibilidad
4. **Mantenibilidad**: Fácil agregar o remover modelos modificando el array `FallbackModels`
5. **Sin Dependencias Externas**: No requiere librerías de terceros como Polly

## Trade-offs

| Aspecto | Beneficio | Costo |
|---------|-----------|-------|
| Latencia | +50-200ms en caso de fallback | Aceptable para PWA |
| Costos API | Sin cambio (solo se cobra la llamada exitosa) | - |
| Complejidad | Mínima (70 líneas de código) | - |
| Tasa de Éxito | +15-25% de requests salvados | - |

## Mantenimiento

### Agregar un Nuevo Modelo

```csharp
private static readonly string[] FallbackModels = 
[
    "gemini-2.5-flash",
    "gemini-1.5-flash",
    "gemini-1.5-flash-8b",
    "nuevo-modelo-aqui"  // Agregar al final
];
```

### Cambiar el Orden de Preferencia

Simplemente reordena los elementos en el array `FallbackModels`.

### Deshabilitar Fallback

Comentar o eliminar todos los modelos excepto el primario:

```csharp
private static readonly string[] FallbackModels = 
[
    "gemini-2.5-flash"  // Solo modelo primario
];
```

## Métricas Recomendadas

Para monitorear la efectividad del fallback en producción:

1. **Tasa de Fallback**: % de requests que usaron modelo secundario
2. **Tasa de Fallo Total**: % de requests donde todos los modelos fallaron
3. **Latencia por Modelo**: p50, p95, p99 para cada modelo
4. **Disponibilidad por Modelo**: Uptime de cada modelo en el último mes

## Referencias

- [Gemini API Models](https://ai.google.dev/models/gemini)
- [HTTP Status Codes - RFC 7231](https://tools.ietf.org/html/rfc7231#section-6.6.4)
- [Azure Well-Architected Framework - Reliability](https://learn.microsoft.com/azure/architecture/framework/resiliency/overview)
