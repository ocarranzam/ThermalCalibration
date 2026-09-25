# HU-01 · Registrar empresa cliente y equipo
# Generado desde docs/specs/functional/03-user-stories.md (v0.6). No editar a mano:
# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.
# Como técnico de calibración quiero registrar la empresa cliente (RUC) y sus equipos para asociar cada sesión de medición a un equipo identificable y mantener su historial.
# Perfil: Técnico, Admin · Escenarios de prueba: —

@HU-01 @SM
Feature: Registro de empresas cliente y equipos

  Background:
    Given que he iniciado sesión con el rol "Technician"
    And existe el tipo de equipo "Congeladora" activo

  Scenario: Registrar una empresa nueva
    When registro la empresa con RUC "20100070970" y razón social "Laboratorios Andinos S.A.C."
    Then la empresa queda registrada y activa
    And aparece en el buscador de empresas por RUC y por razón social

  Scenario: Rechazar un RUC duplicado
    Given existe la empresa con RUC "20100070970"
    When intento registrar otra empresa con RUC "20100070970"
    Then el sistema rechaza el registro con el mensaje "Ya existe una empresa con el RUC 20100070970"
    And me ofrece abrir la empresa existente

  Scenario Outline: Rechazar un RUC con formato inválido
    When intento registrar una empresa con RUC "<ruc>"
    Then el sistema rechaza el registro con el mensaje "<mensaje>"

    Examples:
      | ruc          | mensaje                                        |
      | 2010007097   | El RUC debe tener 11 dígitos                   |
      | 2010007097A  | El RUC solo admite dígitos                     |
      | 30100070970  | El RUC debe empezar con 10, 15, 17 o 20        |
      | 20100070971  | El dígito verificador del RUC no es válido     |

  Scenario: Registrar un equipo asociado a la empresa
    Given existe la empresa con RUC "20100070970"
    When registro un equipo para esa empresa con:
      | campo           | valor        |
      | Tipo de equipo  | Congeladora  |
      | Marca           | Haier        |
      | Modelo          | HBF-205      |
      | Número de serie | SN-88231     |
    Then el equipo queda registrado y asociado a la empresa
    And su historial de sesiones está vacío

  Scenario: Rechazar un número de serie duplicado en la misma empresa
    Given la empresa con RUC "20100070970" tiene un equipo con serie "SN-88231"
    When intento registrar otro equipo de esa empresa con serie "SN-88231"
    Then el sistema rechaza el registro con el mensaje "La empresa ya tiene un equipo con la serie SN-88231"

  Scenario: Permitir la misma serie en otra empresa
    Given la empresa con RUC "20100070970" tiene un equipo con serie "SN-88231"
    And existe la empresa con RUC "20601234561"
    When registro para la empresa "20601234561" un equipo con serie "SN-88231"
    Then el equipo queda registrado

  Scenario: Datos obligatorios del equipo
    When intento registrar un equipo sin número de serie o sin tipo de equipo
    Then el sistema no guarda el equipo e indica los campos obligatorios faltantes
