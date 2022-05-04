#! /bin/bash

eval "$(jq -r '@sh "terraformFuncId=\(.funcId)"')"

faURL=https://management.azure.com/$terraformFuncId/host/default/systemkeys/webpubsub_extension?api-version=2016-08-01
key=$(az rest --method put --body "{\"properties\": {\"name\": \"webpubsub_extension\"}}" --url $faURL | jq -r '.properties.value') 

jq -n --arg key "$key" \
  '{"key":$key}'
