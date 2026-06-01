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

Scenario: A valid draft expression passes validation
	Given a validation request for the "Mortgage.Model" loan application model
	And the draft expression is "Terms.RequestedLoanAmount <= 500000"
	And the draft result code is "AMT-001" and message "Within limit"
	When I post the validation request
	Then the response status should be "OK"
	And the draft should be valid
	And the validation should report 0 errors

Scenario: A draft expression with an unknown property is rejected
	Given a validation request for the "Mortgage.Model" loan application model
	And the draft expression is "Terms.NotARealProperty > 10"
	When I post the validation request
	Then the response status should be "OK"
	And the draft should be invalid
	And the validation issues should contain "NotARealProperty"

Scenario: A draft expression with unbalanced parentheses is rejected
	Given a validation request for the "Mortgage.Model" loan application model
	And the draft expression is "(Terms.RequestedLoanAmount <= 500000"
	When I post the validation request
	Then the response status should be "OK"
	And the draft should be invalid

Scenario: An entire rules library is validated
	Given a library validation request using the inline rules library from file "MortgageEligibilityRules.json"
	When I post the library validation request
	Then the response status should be "OK"
	And the draft should be valid
