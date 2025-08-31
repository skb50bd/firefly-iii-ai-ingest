# Brotal.FireflyIII.Model.Rule

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Title** | **string** |  | 
**RuleGroupId** | **string** | ID of the rule group under which the rule must be stored. Either this field or rule_group_title is mandatory. | 
**Trigger** | **RuleTriggerType** |  | 
**Triggers** | [**List&lt;RuleTrigger&gt;**](RuleTrigger.md) |  | 
**Actions** | [**List&lt;RuleAction&gt;**](RuleAction.md) |  | 
**CreatedAt** | **DateTime** |  | [optional] [readonly] 
**UpdatedAt** | **DateTime** |  | [optional] [readonly] 
**Description** | **string** |  | [optional] 
**RuleGroupTitle** | **string** | Title of the rule group under which the rule must be stored. Either this field or rule_group_id is mandatory. | [optional] 
**Order** | **int** |  | [optional] [readonly] 
**Active** | **bool** | Whether or not the rule is even active. Default is true. | [optional] [default to true]
**Strict** | **bool** | If the rule is set to be strict, ALL triggers must hit in order for the rule to fire. Otherwise, just one is enough. Default value is true. | [optional] 
**StopProcessing** | **bool** | If this value is true and the rule is triggered, other rules  after this one in the group will be skipped. Default value is false. | [optional] [default to false]

[[Back to Model list]](../../README.md#documentation-for-models) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to README]](../../README.md)

