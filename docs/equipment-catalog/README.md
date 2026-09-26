# Catálogo de equipos (fichas de fabricantes)

Fichas técnicas y catálogos **públicos** de los fabricantes de los equipos que mide el sistema. Sirven para identificar el modelo de un equipo, para comparar un ensayo con la especificación del fabricante y, en el futuro, para los **perfiles de eficacia por modelo** ([implementation-plan.md](../implementation-plan.md)).

| Campo | Valor |
|---|---|
| Versión | 0.1 |
| Fecha de descarga | 2026-09-25 |
| Usado por | [DATA-1-analisis.md](../data/DATA-1-analisis.md) (identificación de la cámara Memmert) |

> Los documentos son propiedad de sus fabricantes (© Memmert GmbH + Co. KG). Se guardan como copia de referencia de materiales de descarga pública, sin modificar. Ante cualquier duda, prevalece la versión vigente en el sitio del fabricante.

## 1. Memmert

Carpeta [memmert/](memmert/).

| Archivo | Contenido | Origen | SHA-256 (16 primeros) |
|---|---|---|---|
| [Memmert-Climate-chambers-brochure-D13643.pdf](memmert/Memmert-Climate-chambers-brochure-D13643.pdf) | Catálogo de cámaras climáticas (HPP, HCP, ICH, CTC/TTC) con datos técnicos | [Fisher Scientific (distribuidor)](https://assets.fishersci.com/TFS-Assets/CCG/EU/Memmert/Product-Information/BR-Climate-Chambers-english-D13643_01.pdf) | `46C679D574105D52` |
| [Memmert-Environmental-test-chambers-CTC256.pdf](memmert/Memmert-Environmental-test-chambers-CTC256.pdf) | Ficha técnica CTC256 (*Environmental test chamber*) | [Memmert USA](https://www.memmertusa.com/Content/files/Manuals/DataSheets/Memmert-Environmental-test-chambers-CTC256.pdf) | `C406830BB2D2C10D` |
| [Memmert-CTC-TTC-operating-manual-D10804.pdf](memmert/Memmert-CTC-TTC-operating-manual-D10804.pdf) | Manual de operación CTC 256 / TTC 256 | [Memmert USA](https://www.memmertusa.com/Content/files/Manuals/BA-CTC-TTC-EN-D10804.pdf) | `BD2189E62A6DC251` |
| [Memmert-Constant-climate-chamber-HPP110.pdf](memmert/Memmert-Constant-climate-chamber-HPP110.pdf) | Ficha técnica HPP110 (*Constant climate chamber*, Peltier) | [Memmert USA](https://www.memmertusa.com/Content/files/Manuals/DataSheets/Memmert-Constant-climate-chamber-HPP110.pdf) | `32CF4901E65E4114` |
| [Memmert-Constant-climate-chamber-HPP260.pdf](memmert/Memmert-Constant-climate-chamber-HPP260.pdf) | Ficha técnica HPP260 | [Memmert USA](https://www.memmertusa.com/Content/files/Manuals/DataSheets/Memmert-Constant-climate-chamber-HPP260.pdf) | `B0B92201CADCFD4C` |
| [Memmert-Constant-climate-chamber-HPP750.pdf](memmert/Memmert-Constant-climate-chamber-HPP750.pdf) | Ficha técnica HPP750 | [Memmert USA](https://www.memmertusa.com/Content/files/Manuals/DataSheets/Memmert-Constant-climate-chamber-HPP750.pdf) | `59AA3CB5DA260697` |
| [Memmert-Climate-chamber-ICH260L.pdf](memmert/Memmert-Climate-chamber-ICH260L.pdf) | Ficha técnica ICH260L (*Climate chamber*, estabilidad ICH Q1A) | [Memmert USA](https://www.memmertusa.com/Content/files/Manuals/DataSheets/Memmert-Climate-chamber-ICH260L.pdf) | `253C23AD8C579752` |

### 1.1 Especificaciones de temperatura por familia

| Familia | Nombre del fabricante | Volúmenes | Rango | Técnica | Variación en el tiempo | Homogeneidad | Fuente |
|---|---|---|---|---|---|---|---|
| **CTC / TTC** | *Environmental test chambers* (CTC: *climatic*; TTC: *temperature*) | 256 L | −42 a +190 °C; CTC con humedad de +10 a +95 °C | Compresor doble | **±0,2 … 0,5 K** (DIN 12880, según la consigna) | **±0,5 … 2 K** (según la consigna) | Catálogo D13643 |
| HPP | *Constant climate chamber* | 108, 256, 749 L | 0 a +70 °C | Peltier | No publicada en ficha | No publicada en ficha | Fichas HPP |
| ICH | *Climate chamber* (estabilidad) | 108, 256, 749 L | −10 a +60 °C | Compresor | No publicada en ficha | "Homogeneidad sin igual" (sin valor) | Catálogo, ficha ICH260L |

Estas especificaciones se usaron para identificar el modelo inferido de DATA-1 (**TTC256**) y evaluar que la cámara está en condición óptima.

## 2. Añadir un fabricante o un modelo

1. Descargar **solo documentos públicos** del sitio del fabricante o de un distribuidor oficial.
2. Guardarlos sin modificar en `docs/equipment-catalog/<fabricante>/`, con un nombre descriptivo.
3. Añadir una fila por archivo (contenido, URL de origen y huella SHA-256) y las especificaciones de temperatura por familia o modelo.
4. Si un documento prohíbe expresamente su redistribución, no guardar la copia: solo el enlace, como en [docs/standards/](../standards/README.md).
