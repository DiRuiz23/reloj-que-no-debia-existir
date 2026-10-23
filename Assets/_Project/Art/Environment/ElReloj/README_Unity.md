# El Reloj que No Debia Existir - Entorno 3D (Estacion ferroviaria, Mexico 1910)

Kit modular + escena de demostracion creados en Blender 5.2.2 LTS. Todo es geometria 3D real; no hay fondos 2D, sprites ni planos como sustitutos.

## Contenido de esta carpeta (`export/`)
| Ruta | Que es |
|---|---|
| `FBX/01_TERRAIN` ... `FBX/06_VEGETATION` | 109 FBX, uno por pieza raiz del kit (con sus hijos: ruedas, agujas, cristal...). Pivote en el origen. |
| `FBX/08_DEMO_SCENE/DEMO_Estacion_1910.fbx` | Estacion completa ensamblada (665 objetos, 110 mallas unicas, 34 x 34 m, centrada cerca del origen). |
| `Textures/TEX_Palette_16x16.png` | Paleta unica de 100 colores (16 x 16 px). Todo el kit usa esta textura. |
| `Unity/ElReloj/Editor/ElRelojPostprocessor.cs` | Script de editor: filtrado Point, ajustes de importacion, materiales, colliders y capa Interactable. |
| `INVENTARIO_objetos.csv` | 145 objetos del kit con dimensiones (m), triangulos, collider, interactivo y FBX. |
| `PRUEBAS_Y_PENDIENTES.md` | Pruebas realizadas y problemas pendientes. |

El archivo editable es `../ElReloj_Estacion1910.blend`.

## Convenciones
- Unidades: metros (1 unidad de Unity = 1 m). Cuadricula de 1 m; suelos de 2 x 2 m; paredes de 2 x 3 m, 0,30 m de grosor; puertas de 1,0 x 2,1 m; via de 1,435 m.
- Pivotes: suelo y props en el centro de la base; piezas de pared en la cara trasera; puertas, brazos y ruedas en su bisagra o eje; lamparas colgantes en el techo.
- Frente de las piezas: en Blender el frente de paredes, fachadas, muebles y carteles mira a -Y. La orientacion resultante en Unity no se ha comprobado; si una pieza queda girada, ajusta el eje en el prefab.
- Material unico `MAT_Palette` (UV de cada cara apunta a una celda de color) y `MAT_Glass_Clear` para cristales. Estilo pixel art: sin texturas procedurales.
- Objetos interactivos (35) son GameObjects separados con la propiedad `unity_interactable`. Hijos animables (agujas, pendulo, brazo del semaforo, ruedas) giran sobre su eje local Y.
- Las propiedades `unity_collider` (`box`, `capsule`, `sphere`, `mesh_convex`, `none`) se exportan en el FBX.

## Importacion en Unity (2022.3 LTS o superior; URP recomendado)
1. Crea la carpeta `Assets/ElReloj/` y copia dentro: `FBX/`, `Textures/` y `Unity/ElReloj/Editor/` (queda `Assets/ElReloj/Editor/ElRelojPostprocessor.cs`).
2. Espera a que Unity compile. La textura `TEX_Palette_16x16.png` se importara con filtrado **Point**, sin mipmaps ni compresion.
3. Menu **ElReloj > 1 - Crear materiales**. Crea `MAT_Palette` y `MAT_Glass_Clear` (transparente) en `Assets/ElReloj/Materials`.
4. Reimporta los modelos (clic derecho en `Assets/ElReloj/FBX` > Reimport) para que se asignen esos materiales.
5. Opcional: crea la capa **Interactable** (Project Settings > Tags and Layers) y reimporta; las piezas interactivas la recibiran.
6. Arrastra `DEMO_Estacion_1910.fbx` a una escena vacia. Menu **ElReloj > 2 - Alternar tejado de la demo** oculta o muestra el tejado para ver el interior.
7. Ilumina con una luz direccional calida (color aprox. #FFDBA8) y ambiente sepia. Las lamparas usan colores emisivos de la paleta (`lamp_glow`, `clock_glow`); anade Bloom si quieres brillo.
8. Prefabs: arrastra cada FBX de `FBX/` a la escena y guardalo como prefab. Las piezas `PREFAB_*` (locomotora, tender, vagon, pilas de carga) ya traen su jerarquia.

Notas:
- Si tu proyecto no es URP, el script usa el shader Standard.
- Si Unity muestra rotacion -90 en los modelos, comprueba que "Bake Axis Conversion" este activo en el importador.
- Los colliders se generan al importar a partir de `unity_collider`; los de tipo `none` no llevan collider (rieles, vegetacion pequena, decoracion).

## Como reconstruir la demo en Blender
Colecciones: `00_REFERENCE` (camaras y luz), `01_TERRAIN` ... `06_VEGETATION`, `07_MATERIALS` (barra 3D de muestras de paleta), `08_DEMO_SCENE` (instancias que comparten malla con el kit).
Para ver el interior ejecuta en Blender: `exec(bpy.data.texts["demo_cutaway"].as_string()); cutaway("interior")` (modos `full`, `no_roof`, `interior`, `open`) o apaga el ojo de `Roof` y `Walls_*` en el Outliner.
Camaras: `CAM_Ortho_Presentation`, `CAM_Perspective_Check` y `CAM_Interior_Check`.
