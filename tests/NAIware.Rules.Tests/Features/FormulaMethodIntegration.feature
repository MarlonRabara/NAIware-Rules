Feature: Formula Method Integration
	As a developer
	I want default method wrappers to evaluate through the logic processor engine
	So that formulas using IF, ROUND, ABS, MIN, and MAX produce correct results end-to-end

Scenario Outline: Evaluate a formula through the engine
	Given a logic processor for "<expression>"
	When I evaluate the formula as a decimal
	Then the logic processor result should be <expected>

	Examples:
		| expression          | expected |
		| IF(1 = 1, 100, 200) | 100      |
		| IF(1 = 2, 100, 200) | 200      |
		| ROUND(10.567, 2)    | 10.57    |
		| ROUNDUP(10.561, 2)  | 10.57    |
		| ABS(-15)            | 15       |
		| MIN(5, 10)          | 5        |
		| MAX(5, 10)          | 10       |
		| MAX(ABS(-10), 5)    | 10       |
		| MIN(ROUND(10.567, 2), 20) | 10.57 |
		| SUM(1, 2, 3)        | 6        |
		| AVERAGE(2, 4, 6)    | 4        |
		| POWER(2, 3)         | 8        |
		| CEILING(4.1)        | 5        |
		| FLOOR(4.9)          | 4        |
		| POWER(2, 3) + ABS(-1) | 9      |
		| MAX(POWER(2, 3), CEILING(4.1)) | 8 |
