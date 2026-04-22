# Politica de metricas ML basada en F-beta

Este proyecto adopta **F-beta** como metrica principal de desempeno para modelos de clasificacion del modulo IA.

## Metrica oficial

- Formula:

```text
Fbeta = ((1 + beta^2) * Precision * Recall) / ((beta^2 * Precision) + Recall)
```

- Interpretacion de `beta`:
  - `beta > 1`: prioriza `Recall`.
  - `beta < 1`: prioriza `Precision`.
  - `beta = 1`: equivale a `F1`.

## Configuracion recomendada para NOS_VEMOS

Para el caso de seguridad/alerta (donde omitir una deteccion es costoso), se define:

- Metrica principal: `F2`.
- Metricas de control: `Recall`, `Precision`, matriz de confusion.

## Regla de aceptacion de modelo

Un modelo se considera apto para despliegue si cumple simultaneamente:

- `F2 >= 0.82`
- `Recall >= 0.88`
- `Precision >= 0.70`

Si no cumple cualquiera de las tres condiciones, el modelo queda en estado **No Aprobado**.

## Criterio de seguimiento operativo

En monitoreo continuo (produccion o preproduccion):

- Alerta amarilla: `F2 < 0.80` en ventana movil semanal.
- Alerta roja: `F2 < 0.75` en ventana movil semanal o `Recall < 0.82`.

## Periodicidad de evaluacion

- Evaluacion minima: semanal.
- Re-entrenamiento recomendado cuando:
  - exista deriva de datos visible en la matriz de confusion,
  - o se activen alertas durante 2 ventanas consecutivas.

## Registro minimo por corrida

Cada corrida de evaluacion del modelo debe guardar:

- fecha de evaluacion,
- version de modelo,
- dataset usado,
- `F2`, `Recall`, `Precision`,
- matriz de confusion,
- decision final (`Aprobado` / `No Aprobado`).

## Nota

Esta politica evita usar metricas ambiguas como objetivo principal y alinea la decision del modelo con el riesgo operativo del proyecto.
