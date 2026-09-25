# Set de datos de prueba

Escenarios reproducibles para probar la captura, las alertas, la pérdida de sensores, los huecos de comunicación, el cierre y la exportación **sin sensores ni adquisidor**.

- Especificación completa: [docs/specs/functional/06-test-data.md](../docs/specs/functional/06-test-data.md)
- Regenerar (Node.js ≥ 18, sin dependencias): `node test-data/generate-test-data.mjs`
- Cada carpeta `scenarios/TD-nn-*/` contiene `scenario.json` (configuración, eventos y resultados esperados), `readings.csv` (lecturas transmitidas y esperadas) y `transcript.log` (transcripción serial de referencia).

Los archivos de `scenarios/` se generan con semilla fija: no los edites a mano.
