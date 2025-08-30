# Brotal.FireflyIII.Model.BillStore

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Name** | **string** |  | 
**AmountMin** | **string** |  | 
**AmountMax** | **string** |  | 
**Date** | **DateTime** |  | 
**RepeatFreq** | **BillRepeatFrequency** |  | 
**CurrencyId** | **string** | Use either currency_id or currency_code | [optional] 
**CurrencyCode** | **string** | Use either currency_id or currency_code | [optional] 
**EndDate** | **DateTime** | The date after which this bill is no longer valid or applicable | [optional] 
**ExtensionDate** | **DateTime** | The date before which the bill must be renewed (or cancelled) | [optional] 
**Skip** | **int** | How often the bill must be skipped. 1 means a bi-monthly bill. | [optional] 
**Active** | **bool** | If the bill is active. | [optional] 
**Notes** | **string** |  | [optional] 
**ObjectGroupId** | **string** | The group ID of the group this object is part of. NULL if no group. | [optional] 
**ObjectGroupTitle** | **string** | The name of the group. NULL if no group. | [optional] 

[[Back to Model list]](../../README.md#documentation-for-models) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to README]](../../README.md)

