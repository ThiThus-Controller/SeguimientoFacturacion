# Convenciones de desarrollo

## Idioma y nombres

- El lenguaje del dominio y la interfaz es español.
- Clases, métodos y propiedades usan `PascalCase`.
- Variables y parámetros usan `camelCase`.
- Interfaces comienzan con `I`.
- Los nombres deben expresar una responsabilidad concreta.
- No usar nombres genéricos como `Helper`, `Util`, `Class1`, `Nuevo` o
  `Prueba` en código productivo.
- Los acrónimos forman parte del nombre: `Dto`, `Id`, `Utc`, `Fe`.

## Organización

- Una clase pública principal por archivo.
- El nombre del archivo coincide con el tipo principal.
- Los namespaces siguen la ruta del proyecto.
- Domain no referencia Application, Infrastructure ni Web.
- Las interfaces que necesita Application se declaran en Application.
- Infrastructure implementa contratos y configura adaptadores.
- Web solo coordina solicitudes y presentación.

## Dominio

- Las entidades protegen invariantes en constructores y métodos.
- Los setters públicos están prohibidos en agregados.
- Los importes financieros usan `decimal`, nunca `float` ni `double`.
- Las fechas de negocio sin hora usan `DateOnly`.
- Las fechas de auditoría usan `DateTimeOffset` en UTC.
- Los identificadores externos que pueden contener ceros se almacenan
  como texto.
- Las reglas reutilizables se representan como servicios, políticas o
  value objects del dominio.

## Application

- Los controladores consumen casos de uso o servicios de Application.
- Las entradas y salidas cruzan límites mediante DTO.
- FluentValidation valida formato y requisitos de solicitudes.
- Las reglas esenciales se vuelven a validar en Domain.
- Los métodos de E/S son asíncronos y reciben `CancellationToken`.
- No exponer `IQueryable`, entidades EF ni excepciones de proveedor.

## Infrastructure

- EF Core se configura exclusivamente mediante Fluent API.
- Las migraciones se revisan antes de ejecutarse.
- No modificar una migración ya aplicada.
- Índices y restricciones deben tener nombres explícitos.
- Toda operación definitiva de importación es transaccional.
- ClosedXML se limita a Infrastructure.
- Las consultas de lectura usan `AsNoTracking` cuando corresponda.

## Web

- Los controladores deben ser delgados.
- Cada acción sensible requiere `[Authorize]` o una política equivalente.
- Nunca confiar únicamente en botones ocultos o validación JavaScript.
- Los mensajes al usuario no deben revelar secretos, SQL ni trazas.
- Los ViewModels son independientes de las entidades del dominio.

## Seguridad

- No escribir secretos en código, Git, logs, documentación o capturas.
- No registrar contraseñas, claves AES ni cadenas de conexión completas.
- `usuarios.dat` nunca se versiona.
- Las contraseñas se derivan; nunca se cifran de forma reversible.
- La clave AES debe tener 256 bits y provenir de un almacén seguro.
- La autorización se verifica en el servidor.
- Los permisos tienen códigos estables y no se renombran sin migración.

## SQL y datos

- Usar nombres de esquemas explícitos.
- Los scripts diagnósticos deben ser de solo lectura.
- Los scripts de mantenimiento deben incluir simulación o transacción.
- Antes de una migración, crear y comprobar una copia de seguridad.
- Evitar comparaciones monetarias aproximadas.
- No eliminar registros históricos para resolver una inconsistencia.
- No incluir datos reales de pacientes en pruebas o repositorio.

## Pruebas

- Domain prueba invariantes y cálculos puros.
- Application prueba orquestación, validación y resultados.
- Infrastructure prueba mapeos, repositorios, archivos y EF Core.
- Web prueba políticas, controladores y servicios HTTP.
- Cada corrección de defecto debe incorporar una prueba de regresión.
- El nombre del test sigue `Metodo_Escenario_ResultadoEsperado`.
- No ubicar archivos `*Tests.cs` dentro de proyectos productivos.

## Commits

Se utiliza Conventional Commits:

```text
feat(importacion): procesar lote de pagos
fix(seguridad): reactivar cuentas inactivas
test(sql): certificar integralmente la fase uno
docs(project): documentar cierre de fase uno
refactor(domain): centralizar calculo de saldo
chore(deps): actualizar paquetes sin vulnerabilidades
```

Cada commit debe ser pequeño, coherente y compilar. No mezclar cambios
funcionales independientes.

## Ramas

- `main`: versión integrada y estable.
- `feature/*`: funcionalidad nueva.
- `fix/*`: corrección aislada.
- `refactor/*`: cambio estructural preservando comportamiento.

Antes de fusionar:

1. Árbol de trabajo limpio.
2. Compilación exitosa.
3. Todas las pruebas correctas.
4. Sin paquetes vulnerables conocidos.
5. Migraciones revisadas.
6. Documentación actualizada.
7. Sin archivos sensibles ni binarios accidentales.

## Documentación XML

Los tipos y miembros públicos relevantes incluyen comentarios XML que
explican propósito, parámetros, resultado y excepciones importantes. No
se documenta lo obvio ni se usan comentarios para justificar código
confuso.
