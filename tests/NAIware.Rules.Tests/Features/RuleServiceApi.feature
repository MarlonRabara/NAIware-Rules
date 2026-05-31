Feature: Rule Service API
	As a consumer of the NAIware Rule Service
	I want to submit serialized models to the HTTP evaluation endpoint
	So that I can evaluate them against a rules library and receive results

Scenario: Health endpoint reports healthy
	When I request the service health endpoint
	Then the response status should be "OK"

Scenario: MISMO XML is translated and evaluated via the configured translator
	Given a request for the "Mortgage.Model" loan application model
	And the request uses the MISMO translator
	And the request payload is the file "sample-loan-application.xml" as "Xml"
	And the request uses the rules library file "MortgageEligibilityRules.json"
	And the request execution mode is "Lenient"
	When I post the evaluation request
	Then the response status should be "OK"
	And the evaluated context name should be "Loan"
	And the evaluation should have succeeded
	And the total evaluated rules should be 3
	And the matches should contain code "BORR-001"
	And the matches should contain code "AMT-001"
	And the mismatches should contain expression "High Balance Loan"

Scenario: Inline XML payload is evaluated
	Given a request for the "Mortgage.Model" loan application model
	And the request uses the MISMO translator
	And the request payload is the inline content of file "sample-loan-application.xml" as "Xml"
	And the request uses the inline rules library from file "MortgageEligibilityRules.json"
	When I post the evaluation request
	Then the response status should be "OK"
	And the total evaluated rules should be 3
	And the matches should contain code "BORR-001"

Scenario: Bad request when the model assembly is missing
	Given a request referencing a missing model assembly "DoesNotExist.dll"
	And the request payload is the inline content "<MESSAGE />" as "Xml"
	And the request uses the rules library file "MortgageEligibilityRules.json"
	When I post the evaluation request
	Then the response status should be "BadRequest"
