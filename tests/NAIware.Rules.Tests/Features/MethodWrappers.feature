Feature: Default Method Wrappers
	As a developer
	I want default formula method wrappers
	So that formulas can use standard functions like IF, ROUND, MIN, MAX, and ABS

Scenario: IF returns the true branch
	Given the "IF" wrapper
	When I execute the wrapper with arguments
		| value |
		| True  |
		| 100   |
		| 200   |
	Then the wrapper result should be 100

Scenario: IF returns the false branch
	Given the "IF" wrapper
	When I execute the wrapper with arguments
		| value |
		| False |
		| 100   |
		| 200   |
	Then the wrapper result should be 200

Scenario: INT converts a fractional value
	Given the "INT" wrapper
	When I execute the wrapper with arguments
		| value |
		| 10.7  |
	Then the wrapper result should be 11

Scenario: ROUND rounds to two decimals
	Given the "ROUND" wrapper
	When I execute the wrapper with arguments
		| value  |
		| 10.567 |
		| 2      |
	Then the wrapper result should be 10.57

Scenario: ROUNDUP rounds up to two decimals
	Given the "ROUNDUP" wrapper
	When I execute the wrapper with arguments
		| value  |
		| 10.561 |
		| 2      |
	Then the wrapper result should be 10.57

Scenario: MIN returns the smaller value
	Given the "MIN" wrapper
	When I execute the wrapper with arguments
		| value |
		| 5     |
		| 10    |
	Then the wrapper result should be 5

Scenario: MAX returns the larger value
	Given the "MAX" wrapper
	When I execute the wrapper with arguments
		| value |
		| 5     |
		| 10    |
	Then the wrapper result should be 10

Scenario: ABS returns the absolute value
	Given the "ABS" wrapper
	When I execute the wrapper with arguments
		| value |
		| -15   |
	Then the wrapper result should be 15

Scenario: IF rejects an incorrect argument count
	Given the "IF" wrapper
	When I execute the wrapper with arguments
		| value |
		| True  |
		| 100   |
	Then the wrapper execution should fail validation

# ----------------------------------------------------------------------------
# Additional edge-case coverage
# ----------------------------------------------------------------------------

Scenario Outline: IF selects the correct branch
	Given the "IF" wrapper
	When I execute the wrapper with delimited arguments "<args>"
	Then the wrapper result should be <expected>

	Examples:
		| args            | expected |
		| True^100^200    | 100      |
		| False^100^200   | 200      |
		| True^-1^-2      | -1       |
		| False^0^5       | 5        |

Scenario Outline: INT converts numeric values
	Given the "INT" wrapper
	When I execute the wrapper with delimited arguments "<args>"
	Then the wrapper result should be <expected>

	Examples:
		| args   | expected |
		| 10.7   | 11       |
		| 10.4   | 10       |
		| -10.7  | -11      |
		| 0      | 0        |
		| 5      | 5        |

Scenario Outline: ROUND rounds to the requested precision
	Given the "ROUND" wrapper
	When I execute the wrapper with delimited arguments "<args>"
	Then the wrapper result should be <expected>

	Examples:
		| args        | expected |
		| 10.567^2    | 10.57    |
		| 10.564^2    | 10.56    |
		| 10.5^0      | 10       |
		| -10.567^2   | -10.57   |
		| 123.456^1   | 123.5    |

Scenario Outline: ROUNDUP always rounds away from zero
	Given the "ROUNDUP" wrapper
	When I execute the wrapper with delimited arguments "<args>"
	Then the wrapper result should be <expected>

	Examples:
		| args        | expected |
		| 10.561^2    | 10.57    |
		| 10.001^2    | 10.01    |
		| 10.5^0      | 11       |
		| 2.001^0     | 3        |

Scenario Outline: MIN returns the smaller of two values
	Given the "MIN" wrapper
	When I execute the wrapper with delimited arguments "<args>"
	Then the wrapper result should be <expected>

	Examples:
		| args     | expected |
		| 5^10     | 5        |
		| 10^5     | 5        |
		| -5^-10   | -10      |
		| 0^0      | 0        |
		| 3.5^3.4  | 3.4      |

Scenario Outline: MAX returns the larger of two values
	Given the "MAX" wrapper
	When I execute the wrapper with delimited arguments "<args>"
	Then the wrapper result should be <expected>

	Examples:
		| args     | expected |
		| 5^10     | 10       |
		| 10^5     | 10       |
		| -5^-10   | -5       |
		| 0^0      | 0        |
		| 3.5^3.4  | 3.5      |

Scenario Outline: ABS returns the magnitude of a value
	Given the "ABS" wrapper
	When I execute the wrapper with delimited arguments "<args>"
	Then the wrapper result should be <expected>

	Examples:
		| args   | expected |
		| -15    | 15       |
		| 15     | 15       |
		| 0      | 0        |
		| -2.5   | 2.5      |

Scenario Outline: Default wrappers reject an incorrect argument count
	Given the "<wrapper>" wrapper
	When I execute the wrapper with delimited arguments "<args>"
	Then the wrapper execution should fail validation

	Examples:
		| wrapper | args     |
		| IF      | True^1   |
		| INT     | 1^2      |
		| ROUND   | 10.5     |
		| ROUNDUP | 10.5     |
		| MIN     | 5        |
		| MAX     | 5        |
		| ABS     | 5^6      |

Scenario Outline: Default wrappers reject an empty argument list
	Given the "<wrapper>" wrapper
	When I execute the wrapper with no arguments
	Then the wrapper execution should fail validation

	Examples:
		| wrapper |
		| IF      |
		| INT     |
		| ROUND   |
		| ROUNDUP |
		| MIN     |
		| MAX     |
		| ABS     |

