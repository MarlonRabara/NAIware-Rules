Feature: Extended Method Wrappers
	As a developer
	I want extended numeric, text, and date formula functions
	So that formulas can perform aggregation, string manipulation, and date math

# ----------------------------------------------------------------------------
# Numeric functions
# ----------------------------------------------------------------------------

Scenario Outline: SUM adds its numeric arguments
	Given the extended "SUM" wrapper
	When I invoke it with arguments "<args>"
	Then the numeric wrapper result should be <expected>

	Examples:
		| args        | expected |
		| 1^2^3       | 6        |
		| 42          | 42       |
		| 10^-4       | 6        |
		| -5^-5       | -10      |
		| 1.5^2.5     | 4        |
		| 0^0^0       | 0        |

Scenario Outline: AVERAGE computes the arithmetic mean
	Given the extended "AVERAGE" wrapper
	When I invoke it with arguments "<args>"
	Then the numeric wrapper result should be <expected>

	Examples:
		| args        | expected |
		| 2^4^6       | 4        |
		| 10          | 10       |
		| 1^2         | 1.5      |
		| -2^2        | 0        |
		| 5^10^15^20  | 12.5     |

Scenario Outline: POWER raises a base to an exponent
	Given the extended "POWER" wrapper
	When I invoke it with arguments "<args>"
	Then the numeric wrapper result should be <expected>

	Examples:
		| args   | expected |
		| 2^3    | 8        |
		| 5^0    | 1        |
		| 9^0.5  | 3        |
		| 2^-1   | 0.5      |
		| -2^3   | -8       |
		| 10^2   | 100      |

Scenario Outline: CEILING rounds up to the next integer
	Given the extended "CEILING" wrapper
	When I invoke it with arguments "<args>"
	Then the numeric wrapper result should be <expected>

	Examples:
		| args  | expected |
		| 4.1   | 5        |
		| 4.0   | 4        |
		| -4.1  | -4       |
		| 0     | 0        |
		| 7.999 | 8        |

Scenario Outline: FLOOR rounds down to the previous integer
	Given the extended "FLOOR" wrapper
	When I invoke it with arguments "<args>"
	Then the numeric wrapper result should be <expected>

	Examples:
		| args  | expected |
		| 4.9   | 4        |
		| 4.0   | 4        |
		| -4.1  | -5       |
		| 0     | 0        |
		| 7.001 | 7        |

# ----------------------------------------------------------------------------
# Text functions
# ----------------------------------------------------------------------------

Scenario Outline: CONCAT joins its arguments
	Given the extended "CONCAT" wrapper
	When I invoke it with arguments "<args>"
	Then the text wrapper result should be "<expected>"

	Examples:
		| args            | expected     |
		| Hello^ ^World   | Hello World  |
		| a^b^c           | abc          |
		| foo             | foo          |
		| 1^2^3           | 123          |
		| left^^right     | leftright    |

Scenario: CONCAT mixes numeric and text values
	Given the extended "CONCAT" wrapper
	When I invoke it with arguments "Total: ^42"
	Then the text wrapper result should be "Total: 42"

Scenario: TRIM removes leading and trailing whitespace
	Given the extended "TRIM" wrapper
	When I invoke it with arguments "   padded   "
	Then the text wrapper result should be "padded"

Scenario: TRIM leaves inner whitespace intact
	Given the extended "TRIM" wrapper
	When I invoke it with arguments "  a b c  "
	Then the text wrapper result should be "a b c"

Scenario: TRIM of an all-whitespace string yields empty
	Given the extended "TRIM" wrapper
	When I invoke it with arguments "     "
	Then the text wrapper result should be empty

Scenario Outline: REPLACE swaps a positional span
	Given the extended "REPLACE" wrapper
	When I invoke it with arguments "<args>"
	Then the text wrapper result should be "<expected>"

	Examples:
		| args            | expected |
		| abcdef^2^3^XY   | aXYef    |
		| abcdef^1^6^Z    | Z        |
		| abcdef^1^0^X    | Xabcdef  |
		| abcdef^7^3^X    | abcdefX  |
		| abcdef^2^99^X   | aX       |

Scenario Outline: SUBSTITUTE replaces matched text
	Given the extended "SUBSTITUTE" wrapper
	When I invoke it with arguments "<args>"
	Then the text wrapper result should be "<expected>"

	Examples:
		| args        | expected |
		| a-b-c^-^+   | a+b+c    |
		| aaa^a^b     | bbb      |
		| abc^x^y     | abc      |
		| hello^l^L   | heLLo    |
		| a-b-c^-^    | abc      |

Scenario: SUBSTITUTE with empty match returns the original text
	Given the extended "SUBSTITUTE" wrapper
	When I invoke it with arguments "abc^^x"
	Then the text wrapper result should be "abc"

Scenario: SUBSTITUTE is case-sensitive
	Given the extended "SUBSTITUTE" wrapper
	When I invoke it with arguments "aAaA^a^z"
	Then the text wrapper result should be "zAzA"

Scenario Outline: LEFT returns leading characters
	Given the extended "LEFT" wrapper
	When I invoke it with arguments "<args>"
	Then the text wrapper result should be "<expected>"

	Examples:
		| args        | expected |
		| abcdef^3    | abc      |
		| abcdef^10   | abcdef   |
		| abcdef^1    | a        |

Scenario Outline: LEFT clamps non-positive counts to empty
	Given the extended "LEFT" wrapper
	When I invoke it with arguments "<args>"
	Then the text wrapper result should be empty

	Examples:
		| args        |
		| abcdef^0    |
		| abcdef^-1   |

Scenario Outline: RIGHT returns trailing characters
	Given the extended "RIGHT" wrapper
	When I invoke it with arguments "<args>"
	Then the text wrapper result should be "<expected>"

	Examples:
		| args        | expected |
		| abcdef^3    | def      |
		| abcdef^10   | abcdef   |
		| abcdef^1    | f        |

Scenario Outline: RIGHT clamps non-positive counts to empty
	Given the extended "RIGHT" wrapper
	When I invoke it with arguments "<args>"
	Then the text wrapper result should be empty

	Examples:
		| args        |
		| abcdef^0    |
		| abcdef^-1   |

Scenario Outline: MID returns a substring from a 1-based position
	Given the extended "MID" wrapper
	When I invoke it with arguments "<args>"
	Then the text wrapper result should be "<expected>"

	Examples:
		| args          | expected |
		| abcdef^2^3    | bcd      |
		| abcdef^1^2    | ab       |
		| abcdef^4^10   | def      |

Scenario Outline: MID returns empty for out-of-range positions or non-positive length
	Given the extended "MID" wrapper
	When I invoke it with arguments "<args>"
	Then the text wrapper result should be empty

	Examples:
		| args          |
		| abcdef^7^2    |
		| abcdef^3^0    |

Scenario Outline: UPPER converts text to upper case
	Given the extended "UPPER" wrapper
	When I invoke it with arguments "<args>"
	Then the text wrapper result should be "<expected>"

	Examples:
		| args      | expected  |
		| abc       | ABC       |
		| AbC       | ABC       |
		| abc123    | ABC123    |
		| ALREADY   | ALREADY   |

Scenario Outline: LOWER converts text to lower case
	Given the extended "LOWER" wrapper
	When I invoke it with arguments "<args>"
	Then the text wrapper result should be "<expected>"

	Examples:
		| args      | expected  |
		| ABC       | abc       |
		| AbC       | abc       |
		| ABC123    | abc123    |
		| already   | already   |

Scenario Outline: PROPER converts text to title case
	Given the extended "PROPER" wrapper
	When I invoke it with arguments "<args>"
	Then the text wrapper result should be "<expected>"

	Examples:
		| args            | expected      |
		| hello WORLD     | Hello World   |
		| john smith      | John Smith    |
		| jANE doE        | Jane Doe      |
		| one two three   | One Two Three |

# ----------------------------------------------------------------------------
# Date / time functions
# ----------------------------------------------------------------------------

Scenario Outline: DATEDIFF returns whole-unit differences
	Given the extended "DATEDIFF" wrapper
	When I invoke it with arguments "<args>"
	Then the numeric wrapper result should be <expected>

	Examples:
		| args                          | expected |
		| day^2024-01-01^2024-01-31     | 30       |
		| month^2024-01-15^2024-03-15   | 2        |
		| year^2020-06-01^2024-06-01    | 4        |
		| day^2024-01-31^2024-01-01     | -30      |
		| day^2024-02-01^2024-02-01     | 0        |
		| year^2020-06-01^2024-05-31    | 3        |
		| month^2024-01-31^2024-02-15   | 0        |

Scenario Outline: DATEDIFF supports unit aliases
	Given the extended "DATEDIFF" wrapper
	When I invoke it with arguments "<args>"
	Then the numeric wrapper result should be <expected>

	Examples:
		| args                                        | expected |
		| yyyy^2020-01-01^2024-01-01                  | 4        |
		| yy^2020-01-01^2024-01-01                    | 4        |
		| mm^2024-01-01^2024-04-01                    | 3        |
		| m^2024-01-01^2024-04-01                     | 3        |
		| dd^2024-01-01^2024-01-10                    | 9        |
		| d^2024-01-01^2024-01-10                     | 9        |
		| hh^2024-01-01 00:00^2024-01-01 05:00        | 5        |
		| h^2024-01-01 00:00^2024-01-01 05:00         | 5        |
		| mi^2024-01-01 00:00^2024-01-01 00:30        | 30       |
		| n^2024-01-01 00:00^2024-01-01 00:30         | 30       |
		| ss^2024-01-01 00:00:00^2024-01-01 00:00:45  | 45       |
		| s^2024-01-01 00:00:00^2024-01-01 00:00:45   | 45       |

Scenario: NOW returns the current date and time
	Given the extended "NOW" wrapper
	When I invoke it with no arguments
	Then the wrapper result should be a date on or after today

Scenario: TODAY returns the current date at midnight
	Given the extended "TODAY" wrapper
	When I invoke it with no arguments
	Then the wrapper result should be a date at midnight

# ----------------------------------------------------------------------------
# Validation failures
# ----------------------------------------------------------------------------

Scenario Outline: Wrappers reject an incorrect argument count
	Given the extended "<wrapper>" wrapper
	When I invoke it with arguments "<args>"
	Then the extended wrapper execution should fail validation

	Examples:
		| wrapper    | args           |
		| POWER      | 2              |
		| POWER      | 2^3^4          |
		| CEILING    | 1^2            |
		| FLOOR      | 1^2            |
		| TRIM       | a^b            |
		| REPLACE    | abc^1^2        |
		| SUBSTITUTE | a^b            |
		| LEFT       | abc            |
		| RIGHT      | abc            |
		| MID        | abc^1          |
		| UPPER      | a^b            |
		| LOWER      | a^b            |
		| PROPER     | a^b            |
		| DATEDIFF   | day^2024-01-01 |

Scenario Outline: DATEDIFF rejects an unknown unit
	Given the extended "DATEDIFF" wrapper
	When I invoke it with arguments "<args>"
	Then the extended wrapper execution should fail validation

	Examples:
		| args                          |
		| decade^2020-01-01^2024-01-01  |
		| week^2024-01-01^2024-01-31    |
		| quarter^2024-01-01^2024-04-01 |

Scenario Outline: Wrappers requiring at least one argument reject an empty call
	Given the extended "<wrapper>" wrapper
	When I invoke it with no arguments
	Then the extended wrapper execution should fail validation

	Examples:
		| wrapper |
		| SUM     |
		| AVERAGE |
		| CONCAT  |

Scenario Outline: NOW and TODAY reject any arguments
	Given the extended "<wrapper>" wrapper
	When I invoke it with arguments "<args>"
	Then the extended wrapper execution should fail validation

	Examples:
		| wrapper | args |
		| NOW     | 1    |
		| TODAY   | 1    |
