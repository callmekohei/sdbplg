#!/usr/bin/env bash
# ===========================================================================
#  FILE    : build.bash
#  AUTHOR  : callmekohei <callmekohei at gmail.com>
#  License : MIT license
# ===========================================================================


# fit your file path
FSX_PATH=./src/main.fsx


# see also
# Getting Started with Paket > Manual setup
# https://fsprojects.github.io/Paket/getting-started.html#Manual-setup
function download_paket_bootstrapper () {

    if ! type jq >/dev/null 2>&1 ; then
        echo 'Please install jq'
        return -1
        exit
    fi

    curl -i "https://api.github.com/repos/fsprojects/Paket/releases" \
        | jq '.[]' \
        | jq '.[0].assets[].browser_download_url' \
        | grep 'paket.bootstrapper.exe' \
        | xargs wget -P .paket

    mv .paket/paket.bootstrapper.exe .paket/paket.exe
}



function create_exe_file () {
    declare -a local arr=(
        "${FSX_PATH}"
        --target:library
        --nologo
        --simpleresolution
        -r:$(dirname $(which sdb))/../lib/sdb/sdb.exe
        --out:./bin/sdbplg.dll
    )
    fsharpc "${arr[@]}"
}





if [ -e ./bin/ ] ; then
    echo 'do nothing!'
else
    mkdir ./bin/
    if [ "$?" = 0 ] ; then
        create_exe_file
    fi
fi




