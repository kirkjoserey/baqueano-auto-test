# ⚡ BaqueanoAutoTest

> **Suite de QA Automatizado** para el portal **Baqueano** (Java EE)  
> Pruebas automatizadas de UI en tres viewports: PC Desktop · Tablet · Celular

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![Selenium](https://img.shields.io/badge/Selenium-4.27-43B02A?logo=selenium&logoColor=white)
![ChromeDriver](https://img.shields.io/badge/ChromeDriver-136-4285F4?logo=googlechrome&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-8.0-003B57?logo=sqlite&logoColor=white)
![License](https://img.shields.io/badge/license-MIT-blue)

---

## 📋 Índice

- [¿Qué es?](#-qué-es)
- [Características](#-características)
- [Prerequisitos](#-prerequisitos)
- [Instalación y ejecución](#-instalación-y-ejecución)
- [Configuración](#-configuración)
- [Flujo de ejecución](#-flujo-de-ejecución)
- [Testing Responsive](#-testing-responsive)
- [Módulos de test](#-módulos-de-test)
- [Base de datos SQLite](#-base-de-datos-sqlite)
- [Capturas de pantalla](#-capturas-de-pantalla)
- [Reporte HTML](#-reporte-html)
- [Clasificación de errores](#-clasificación-de-errores)
- [Estructura del proyecto](#-estructura-del-proyecto)
- [Agregar nuevos tests](#-agregar-nuevos-tests)
- [Solución de problemas](#-solución-de-problemas)

---

## 🤖 ¿Qué es?

**BaqueanoAutoTest** es un programa de testing automatizado que se conecta al sistema web Baqueano y realiza pruebas funcionales sobre sus módulos sin intervención humana. Cada vez que se ejecuta:

1. **Limpia** todos los resultados anteriores (base de datos, capturas y reporte)
2. **Ejecuta** todos los tests en tres fases: Desktop → Tablet → Celular
3. **Captura** una pantalla de cada test (sea PASS o FAIL)
4. **Genera** un reporte HTML interactivo con filtros por dispositivo

---

## ✨ Características

| Feature | Descripción |
|---|---|
| 🤖 **Totalmente autónomo** | Un solo comando ejecuta todos los tests sin intervención |
| 📱 **3 viewports** | Misma suite en PC (máximo), Tablet (768 px) y Celular (390 px) |
| 📊 **Reporte HTML visual** | Grilla 3×3, zoom en capturas, lightbox, filtros por dispositivo |
| 🗄 **Historial SQLite** | Todos los resultados quedan registrados en base de datos local |
| 📸 **Capturas automáticas** | PNG por cada test, sea PASS o FAIL |
| 🔒 **Protección admin** | El usuario `admin` nunca puede ser modificado ni eliminado |
| 🍞 **Toast guard** | Detecta y espera notificaciones Sonner antes de cada acción |
| 🏷️ **Clasificación de errores** | Distingue errores de herramienta, de aserción o de programación |

---

## 📦 Prerequisitos

| Componente | Versión | Descripción |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0 o superior | Runtime y compilador de C# |
| [Google Chrome](https://www.google.com/chrome/) | 114 o superior | Navegador para las pruebas |
| ChromeDriver | Misma versión que Chrome | Se instala automáticamente vía NuGet |
| Backend Baqueano | Corriendo en localhost | Accesible en `http://localhost:8080/baqueano` |

> **ChromeDriver** se administra automáticamente vía el paquete NuGet `Selenium.WebDriver.ChromeDriver` — no requiere instalación manual.

---

## 🚀 Instalación y ejecución

```bash
# 1. Clonar el repositorio
git clone https://github.com/kirkjoserey/baqueano-auto-test.git
cd baqueano-auto-test

# 2. Restaurar dependencias
dotnet restore

# 3. Asegurarse de que el backend Baqueano esté corriendo en http://localhost:8080/baqueano

# 4. Ejecutar la suite completa
dotnet run
```

Al finalizar, se genera automáticamente el archivo:
```
bin/Debug/net8.0/TestReport_YYYYMMDD_HHmmss.html
```

Abrirlo en cualquier navegador para ver los resultados.

> ⚠️ **Al iniciar**, el programa borra automáticamente todos los resultados anteriores de la base de datos, las capturas de pantalla y el reporte HTML previo. Cada ejecución parte desde cero.

### Modo headless (sin ventana Chrome)

Útil para servidores CI/CD donde no hay pantalla disponible:

```json
// appsettings.json
"Headless": true
```

---

## ⚙️ Configuración

Toda la configuración se encuentra en `appsettings.json`:

```json
{
  "TestSettings": {
    "BaseUrl":              "http://localhost:8080/baqueano",
    "Headless":             false,
    "ImplicitWaitSeconds":  10,
    "ScreenshotFolder":     "Screenshots",
    "TotalPerfilesTests":   40,
    "TotalUsuariosTests":   12,
    "TotalParametrosTests": 25,
    "ContactosStateTests":  8,
    "ContactosDeleteCount": 5
  },
  "TabletSettings": {
    "Enabled":    true,
    "DeviceName": "",
    "Width":      768,
    "Height":     1024,
    "TotalPerfilesTests":   40,
    "TotalUsuariosTests":   12,
    "TotalParametrosTests": 25,
    "ContactosStateTests":  8,
    "ContactosDeleteCount": 5
  },
  "MobileSettings": {
    "Enabled":    true,
    "DeviceName": "",
    "Width":      390,
    "Height":     844,
    "TotalPerfilesTests":   40,
    "TotalUsuariosTests":   12,
    "TotalParametrosTests": 25,
    "ContactosStateTests":  8,
    "ContactosDeleteCount": 5
  },
  "Credentials": {
    "Username": "admin",
    "Password": "admin123"
  },
  "ConnectionStrings": {
    "SQLite": "Data Source=Data\\baqueano_tests.db"
  }
}
```

### Parámetros principales

| Clave | Tipo | Descripción |
|---|---|---|
| `BaseUrl` | string | URL raíz del sistema bajo prueba |
| `Headless` | bool | `true` = Chrome sin ventana (CI/CD) · `false` = con ventana (desarrollo) |
| `ImplicitWaitSeconds` | int | Segundos de espera máxima por elemento o acción |
| `TotalPerfilesTests` | int | Tests de Perfiles (se distribuye 40% ALTA / 40% MOD / 20% DEL) |
| `TotalUsuariosTests` | int | Tests de Usuarios (NAV + ALTA + LOGIN + MOD + DEL) |
| `TotalParametrosTests` | int | Tests de Parámetros (misma distribución que Perfiles) |
| `ContactosStateTests` | int | Cantidad de contactos a los que se cambia el estado |
| `ContactosDeleteCount` | int | Cantidad de contactos a eliminar |
| `TabletSettings:Enabled` | bool | Activar o desactivar la fase Tablet |
| `TabletSettings:DeviceName` | string | Nombre de dispositivo Chrome DevTools (ej: `"iPad Mini"`). Vacío = usa Width/Height |

Para **deshabilitar** una fase sin borrar su configuración:
```json
"TabletSettings": { "Enabled": false }
```

---

## 🔄 Flujo de ejecución

```
🧹 Limpiar DB    →   🖥️ Fase       →   📟 Fase      →   📱 Fase      →   📊 Reporte
   + Screenshots      Desktop           Tablet            Celular           HTML
```

### Secuencia dentro de cada fase

```
🔐 LoginTest  →  👤 PerfilesTest  →  👥 UsuariosTest  →  🔧 ParametrosTest  →  ✉️ ContactosTest
```

Cada módulo de test implementa la interfaz `ITest` y recibe el driver de Selenium. Los tests se registran en `Program.cs` en el orden de ejecución deseado.

---

## 📱 Testing Responsive

La misma suite de tests se ejecuta tres veces, cada vez con un viewport distinto para verificar que la aplicación funcione correctamente en todos los tipos de dispositivo.

```
🖥️  Desktop             📟  Tablet             📱  Celular
──────────────          ──────────────          ──────────────
Chrome maximizado        768 × 1024 px           390 × 844 px
Sin prefijo              Prefijo: TAB-            Prefijo: MOB-
```

| Fase | Viewport | User-Agent | Prefijo test | Badge reporte |
|---|---|---|---|---|
| 🖥️ Desktop | Pantalla completa | Chrome desktop | *(sin prefijo)* | 🖥️ Desktop |
| 📟 Tablet | 768 px | Android Tablet | `TAB-` | 📟 Tablet |
| 📱 Celular | 390 px | Android Pixel 7 | `MOB-` | 📱 Celular |

> Cuando una fase encuentra que un elemento de la UI no es accesible (por ejemplo, el menú lateral colapsado en mobile), el test registra **FAIL** en esa fase específica sin afectar las demás. Eso es información válida sobre problemas de responsive.

---

## 🧪 Módulos de test

### 🔐 Login — 3 tests fijos

| ID | Escenario | Resultado esperado |
|---|---|---|
| `TC-LOGIN-01` | Credenciales válidas (admin/admin123) | ✅ Acceso al sistema — formulario desaparece |
| `TC-LOGIN-02` | Contraseña incorrecta | ✅ Login rechazado — formulario permanece |
| `TC-LOGIN-03` | Usuario vacío | ✅ Login rechazado — validación en frontend |

Al finalizar los 3 tests, el módulo **vuelve a autenticarse como admin** para que los módulos siguientes puedan operar con sesión activa.

---

### 👤 Perfiles — configurable (default: 40)

Distribución automática a partir de `TotalPerfilesTests`:

| Bloque | Porcentaje | Descripción |
|---|---|---|
| NAV | 2 fijos | Navegación vía sidebar y vía URL directa |
| ALTA | 40% | Crea perfiles con nombre, descripción y estado Activo/Inactivo |
| MODIFICAR | 40% | Edita los perfiles creados cambiando sus datos |
| ELIMINAR | 20% | Borra los perfiles creados y verifica que desaparezcan |

---

### 👥 Usuarios — configurable (default: 12)

| Bloque | Tests | Descripción |
|---|---|---|
| NAV | 2 fijos | Sidebar y URL directa |
| ALTA | 40% del total | Crea usuarios con nombre, apellido, email, perfil (ADMIN/CONSULTA/GESTOR) y estado activo |
| LOGIN | 30% de los creados | Abre una **ventana nueva**, prueba el login de cada usuario creado y cierra la ventana |
| MODIFICAR | 40% del total | Edita datos del usuario (excepto el usuario *admin*) |
| ELIMINAR | 20% del total | Elimina usuarios (nunca elimina *admin*) |

> 🛡️ El usuario **admin** está protegido por doble capa: la página no permite editarlo ni eliminarlo, y los tests lo filtran antes de intentarlo.

---

### 🔧 Parámetros — configurable (default: 25)

Misma distribución que Perfiles (40% ALTA / 40% MOD / 20% DEL).  
Cada parámetro se crea con **clave**, **valor**, **descripción** y **estado** (Activo/Inactivo). Los tests verifican que el registro aparezca en la tabla tras cada operación.

---

### ✉️ Contactos — estados + eliminación

Opera sobre los registros existentes en la tabla.

**Bloque 1 — Cambio de estado**  
Para cada contacto (hasta `ContactosStateTests` = 8), abre el modal de detalle y cambia el estado rotando en ciclo:

```
NUEVO  →  LEIDO  →  RESPONDIDO  →  ARCHIVADO  →  NUEVO…
```

**Bloque 2 — Eliminación**  
Elimina hasta `ContactosDeleteCount` = 5 registros (siempre el primero disponible) y verifica que el conteo de la tabla disminuya correctamente.

> Si no hay contactos en la tabla, ambos bloques se omiten y se registra una advertencia. Los tests nunca fallan por falta de datos.

---

## 🗄 Base de datos SQLite

Los resultados se guardan en `Data\baqueano_tests.db` (relativo al ejecutable). **Se crea automáticamente** si no existe — no es necesario incluirla en el repositorio.

### Tabla: TestResults

| Columna | Tipo | Descripción |
|---|---|---|
| `Id` | INTEGER PK | Autoincremental |
| `TestName` | TEXT | ID del test (ej: `MOB-TC-LOGIN-01`) |
| `Category` | TEXT | Categoría (ej: `Login`, `Usuarios-Alta`) |
| `Passed` | INTEGER | 1 = PASS, 0 = FAIL |
| `Message` | TEXT | Mensaje del resultado o descripción del error |
| `ScreenshotPath` | TEXT | Ruta absoluta a la captura de pantalla |
| `ExecutedAt` | TEXT | Timestamp de ejecución |

---

## 📸 Capturas de pantalla

Cada test guarda una captura al finalizar (sea PASS o FAIL). Se almacenan en `Screenshots\` relativo al ejecutable.

| Aspecto | Detalle |
|---|---|
| Formato | PNG |
| Nombre de archivo | `{TestName}_{yyyyMMdd_HHmmss_fff}.png` |
| Ejemplo | `TC-LOGIN-01_20260521_143022_123.png` |
| Limpieza | Al iniciar cada ejecución se borran todas las capturas anteriores |
| Momento | Se toma *siempre* en el bloque `finally`, incluso si el test lanza excepción |

---

## 📊 Reporte HTML

Al finalizar todas las fases se genera un único archivo `TestReport_YYYYMMDD_HHmmss.html`. Es un archivo **autónomo** (sin dependencias externas) que se puede abrir en cualquier navegador.

### Pestañas por dispositivo

```
[ 📋 Todos ]  [ 🖥️ PC/Desktop ]  [ 📟 Tablet ]  [ 📱 Celular ]
   ✅128/❌22     ✅48/❌2            ✅43/❌7         ✅37/❌13
      87%             96%                 86%              74%
```

Cada pestaña muestra conteo PASS/FAIL y porcentaje en tiempo real al filtrar.

### Color del porcentaje

| Color | Significado |
|---|---|
| 🟢 Verde | 100% de tests en PASS |
| 🟠 Naranja | Entre 50% y 99% en PASS — hay fallas menores |
| 🔴 Rojo | Menos del 50% en PASS — hay fallas críticas |

### Otras características

- Grilla 3×3 con paginación numérica
- Zoom en capturas al pasar el mouse (scale 1.6×)
- Lightbox al hacer clic en la imagen (Escape para cerrar)
- Badge por dispositivo en cada tarjeta (🖥️ Desktop / 📟 Tablet / 📱 Celular)

> Abrir el reporte desde la carpeta `bin\Debug\net8.0\` para que las imágenes carguen correctamente.

---

## 🚨 Clasificación de errores

Cuando un test falla, el mensaje incluye un prefijo que indica la causa raíz:

| Prefijo | Causa | Ejemplos |
|---|---|---|
| `[HERRAMIENTA]` | Problema de Selenium al interactuar con la UI | Elemento no encontrado, elemento bloqueado, timeout de espera, ventana cerrada inesperadamente |
| `[ASERCION]` | El test completó pero el resultado no es el esperado | La página no cargó, el registro no aparece en la tabla, el conteo no bajó tras eliminar |
| `[PROGRAMACION]` | Error en la lógica interna del test | NullReferenceException, InvalidOperationException, lógica de código incorrecta |

> **`[HERRAMIENTA]`** suele indicar que la UI cambió (selector desactualizado) o lentitud de red.  
> **`[ASERCION]`** indica que la funcionalidad del sistema falló.  
> **`[PROGRAMACION]`** indica un bug en el propio código de testing.

---

## 🗂 Estructura del proyecto

```
baqueano-auto-test/
│
├── Program.cs                        # Registro de DI y configuración del host
├── Worker.cs                         # Punto de entrada del servicio (BackgroundService)
├── appsettings.json                  # Toda la configuración del sistema
│
├── Infrastructure/                   # Servicios de soporte
│   ├── ITest.cs                      # Interfaz que deben implementar todos los tests
│   ├── TestResult.cs                 # Modelo de datos de un resultado
│   ├── TestRunner.cs                 # Orquestador: Desktop → Tablet → Mobile
│   ├── DatabaseService.cs            # Lectura/escritura en SQLite
│   ├── ScreenshotService.cs          # Captura y gestión de imágenes PNG
│   ├── HtmlReportService.cs          # Generación del reporte HTML final
│   ├── ErrorClassifier.cs            # Clasifica errores [HERRAMIENTA/ASERCION/PROGRAMACION]
│   ├── MobileDriverFactory.cs        # Crea ChromeDriver con emulación de viewport
│   └── MobileConfigurationProxy.cs  # Redirige config por sección (Tablet/Mobile)
│
├── Pages/                            # Page Object Model (POM): una clase por pantalla
│   ├── LoginPage.cs
│   ├── PerfilesPage.cs
│   ├── UsuariosPage.cs
│   ├── ParametrosPage.cs
│   └── ContactosPage.cs
│
└── Tests/                            # Módulos de prueba: uno por sección del sistema
    ├── LoginTest.cs
    ├── PerfilesTest.cs
    ├── UsuariosTest.cs
    ├── ParametrosTest.cs
    └── ContactosTest.cs
```

### Patrón Page Object Model (POM)

Cada pantalla del sistema tiene su propia clase en `Pages/` que encapsula los selectores y las acciones. Los tests (`Tests/`) usan esas clases sin conocer los detalles del HTML. Esto hace que un cambio en la UI solo requiera modificar el Page Object, no los tests.

---

## ➕ Agregar nuevos tests

1. **Crear el Page Object** en `Pages/MiPaginaPage.cs` con los selectores y acciones de la nueva pantalla.

2. **Crear el módulo de test** en `Tests/MiTest.cs` implementando `ITest`:
   ```csharp
   public class MiTest : ITest
   {
       public async Task<List<TestResult>> RunAsync(IWebDriver driver) { ... }
   }
   ```

3. **Registrar** en `Program.cs`:
   ```csharp
   services.AddTransient<ITest, MiTest>();
   ```

4. **Agregar conteos** en `appsettings.json` en las tres secciones (TestSettings / TabletSettings / MobileSettings):
   ```json
   "TotalMiModuloTests": 10
   ```

El nuevo módulo se ejecutará automáticamente en las tres fases (Desktop, Tablet, Celular) sin ningún cambio adicional en el `TestRunner`.

---

## 🛠 Solución de problemas

| Problema | Causa probable | Solución |
|---|---|---|
| ChromeDriver no inicia | Versión de Chrome ≠ versión del ChromeDriver | Ejecutar `dotnet restore` para actualizar el paquete ChromeDriver |
| `WebDriverTimeoutException` en login | El backend tarda en cargar o el SPA demora en redirigir | Aumentar `ImplicitWaitSeconds` a 15 o 20 en `appsettings.json` |
| Tests de mobile/tablet fallan todos | El DeviceName configurado no existe en esta versión de Chrome | Dejar `DeviceName: ""` para que use `Width`/`Height` automáticamente |
| Toasts bloquean los clicks | Notificaciones Sonner flotando sobre los botones | Ya está manejado por `WaitForToastsToDisappear()`. Si persiste, aumentar el timeout de toast (actualmente 6s) |
| Modal de Contactos no se detecta | React no genera `role="dialog"` estándar | El sistema detecta el modal por los botones de estado (NUEVO/LEIDO/etc.). Si cambian de texto, actualizar `StateButtonsXPath` en `ContactosPage.cs` |
| Reporte HTML sin imágenes | Capturas y HTML en distintas carpetas | Abrir el reporte desde `bin\Debug\net8.0\`, no moverlo a otra ubicación |
| Error al compilar: NETSDK1022 | Archivos duplicados en el `.csproj` | Eliminar la línea `<Content Include="appsettings.json">` del `.csproj` |

---

## 📄 Documentación completa

Ver el archivo [`DOCUMENTACION_QA.html`](./DOCUMENTACION_QA.html) incluido en el repositorio para la documentación técnica completa con diagramas, tablas de configuración y guía de solución de problemas.

---

## 📜 Licencia

MIT © [kirkjoserey](https://github.com/kirkjoserey)
