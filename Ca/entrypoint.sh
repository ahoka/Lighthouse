#!/bin/sh

mkdir -p $(dirname "${PWDPATH}")
echo "${CA_PASSWORD}" > "${PWDPATH}"

if [ ! -f "${CONFIGPATH}" ]
then
    step ca init --name=InternalCA --dns=ca --address=:4443 --provisioner=root@localhost --password-file="${PWDPATH}"
fi

step-ca --password-file "${PWDPATH}" "${CONFIGPATH}"
