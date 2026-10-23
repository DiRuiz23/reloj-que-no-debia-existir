# Pruebas realizadas y problemas pendientes

## Pruebas realizadas (todas en Blender 5.2.2 LTS)
**Geometria y UV (Fase 5)**
- 117 mallas unicas auditadas: 0 normales invertidas, 0 caras degeneradas, 0 vertices sueltos, 0 UV fuera de [0,1]; cada cara usa una sola celda de la paleta.
- 145 objetos del kit: escala 1 y rotacion 0 sin padre; sin transformaciones delta; 0 aristas abiertas en las piezas modeladas con cajas.
- Modulos: suelos 2 x 2 m, paredes 2 x 3 m, rieles/vigas/zocalos/bordes de 2 m, dentro de tolerancia.
- Pivotes revisados por tipo de pieza (base, espalda, bisagra, eje, techo).
**Escena demo**
- 272 celdas de suelo sin huecos ni solapes; 0 penetraciones props-paredes tras reubicar 12 piezas; interior, exterior y tejado verificados con capturas de las camaras.
- Correcciones: arbusto verde (-0,20 m) y nopal (-0,075 m) flotaban; `FACADE_Station_4x3` estaba en modo edicion.
**Exportacion (Fase 6)**
- 109 FBX raiz exportados y comprobados en disco (ninguno falta).
- Reimportacion de 5 muestras (locomotora, puerta, reloj de anomalia, pared con puerta, baldosa): dimensiones identicas, jerarquia completa, UV, materiales y propiedades personalizadas conservadas.
- FBX de la demo reimportado: 665 objetos, 110 mallas unicas, 7 empties, 34 x 34 m.
- `INVENTARIO_objetos.csv` generado con 145 filas.

## No probado
- **No se importo en Unity**: el script `ElRelojPostprocessor.cs` no se ha compilado ni ejecutado, y no se ha comprobado visualmente la orientacion de ejes en Unity. Pruebalo con un proyecto vacio primero.
- Los renders PNG finales no se abrieron; las revisiones visuales se hicieron con capturas de la ventana de Blender.

## Problemas pendientes y limitaciones
1. **Peso de geometria**: la demo visible tiene ~324 mil triangulos; el kit suma ~131 mil. Para Unity conviene fusionar o instanciar con GPU el suelo de tierra (192 piezas). Fachada de la estacion ~6.500 triangulos y librero ~6.800.
2. **Caras ocultas**: al modelar con cajas solapadas quedan caras interiores. No se eliminaron para no perder detalle ni crear aristas abiertas.
3. **Vertices coincidentes** en 26 mallas donde dos cajas se tocan; no se fusionan para evitar aristas no-manifold.
4. **Suelo de la demo**: el patron de tierra se nota repetido a distancia; se resuelve con mas variantes de baldosa.
5. **Curva y desvio de via**: existen en el kit (`RAIL_Curve_R8_30deg`, `SWITCH_Stand_Lever`) pero la demo usa una via recta.
6. **Puertas del almacen y utileria del anden** colocadas por estimacion; puede requerir ajuste fino.
7. **Remates de tejado**: los dos frontones de la demo son `ROOF_GableEnd_Adobe_4m` escalado (2,0 x 1,0 x 2,1) solo en la escena demo; el FBX de la pieza no esta escalado.
8. **Materiales heredados** `LEGACY_MAT_*` y texturas `LEGACY_TEX_*` (20 texturas de 64 px) siguen en el `.blend`, sin uso ni exportacion.
9. **Vidrio**: `MAT_Glass_Clear` es transparente; en Unity hay que usar la transparencia del script (ver guia).
10. **Jugador y animaciones**: no incluidos, segun el encargo.
