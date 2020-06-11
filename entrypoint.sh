#!/bin/sh

echo "Generating certificate for $(hostname)"
step ca certificate $(hostname) lighthouse.crt lighthouse.key --ca-url="${CA_URL}"

echo "Installing the root certificate"
step ca root lighthouse-ca.crt --ca-url="${CA_URL}"
cp lighthouse-ca.crt /usr/local/share/ca-certificates/
update-ca-certificates

dotnet Lighthouse.dll
