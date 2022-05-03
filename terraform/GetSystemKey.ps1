#This script will be called from Terraform to update the webpubsub_extension key
$jsonpayload = [Console]::In.ReadLine()
$json = ConvertFrom-Json $jsonpayload

# Access JSON values 
$terraformFuncId = $json.funcId

$jsonIn = '{"properties": {"name": "webpubsub_extension"}}' | ConvertTo-Json 
$faURL = "https://management.azure.com/" + $terraformFuncId + "/host/default/systemkeys/webpubsub_extension?api-version=2016-08-01"

#az login
try {
    $out = az rest --method put --body $jsonIn --url $faURL | ConvertFrom-Json
    $newKey = $out.properties.value

    $outputJson = @{
        "key" = $newKey
    } | ConvertTo-Json

    #Outputting like this will write the json object back to Terraform, note it needs to be like a string dictionary
    Write-Output $outputJson
}
catch {
    #You can output errors to Terraform too, if you dont do this its sometimes tricky to troubleshoot issues in your script when terraform runs it
    Write-Error $_
    exit 1
}
 