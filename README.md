# El Reloj que No Debía Existir
RPG de aventura 2.5D con mecánica de manipulación del tiempo (Unity + C#).

- Unity: 6000.3.9f1
- Equipo: Diego, Monserrat
- Plataforma: PC (Windows)

## Cómo abrir el proyecto
1. Instalar Git y Git LFS (`git lfs install`)
2. Clonar el repo y abrirlo con Unity Hub con la versión indicada
3. Abrir la escena Assets/_Project/Scenes/Boot.unity

## Flujo de trabajo
Ramas: main (estable) <- develop <- feature/tNN-nombre
Commits: feat(T1): ..., fix: ..., art: ..., docs: ...
Un solo integrante edita una escena a la vez.

## Controles

| Acción | Teclado | Gamepad |
|---|---|---|
| Moverse | A / D o ← / → | Stick izquierdo o cruceta |
| Saltar | Espacio | Botón A / ✕ |
| Interactuar | E | Botón X / ◻ |
| Parada Temporal | Q | RB / R1 |
| Pausa | Esc | Start |
| Táctil | Pendiente (I-37) | — |

Las acciones están definidas en `Assets/_Project/Data/Input/PlayerControls.inputactions`
(mapa `Gameplay`). La clase C# generada está en el namespace `Reloj.Controls`.

## Cómo compilar

Los perfiles de build están en `Assets/Settings/Build Profiles`
(`Windows`, `Web` y `Android`). Cada uno incluye las escenas
`Boot` (0) y `Test_Sandbox` (1). La carpeta `Builds/` no se sube al repositorio.

### Requisitos
- Unity **6000.3.9f1** con los módulos: Windows Build Support (Mono),
  Android Build Support (OpenJDK, Android SDK y NDK) y Web Build Support.
- Python (solo para probar el build Web en local).

### Windows (PC)
1. `File > Build Profiles` → perfil **Windows** → **Switch Profile**.
2. **Build** en la carpeta `Builds/Windows`.
3. Ejecutar `Builds/Windows/Reloj.exe`.

### Web (WebGL)
1. `File > Build Profiles` → perfil **Web** → **Switch Profile**.
2. **Build** en la carpeta `Builds/Web` (compresión desactivada para pruebas locales).
3. Servir la carpeta:
```bash
   cd Builds/Web
   python -m http.server 8000
```
4. Abrir `http://localhost:8000` (no abrir `index.html` directamente).

### Android (APK)
1. En el teléfono: activar *Opciones de desarrollador* y *Depuración USB*.
2. `File > Build Profiles` → perfil **Android** → **Switch Profile**.
3. **Build** en `Builds/Android` (o **Build And Run** con el teléfono conectado).
4. Instalar con `adb install -r Builds/Android/Reloj.apk` si no se usó Build And Run.