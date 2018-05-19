# ===========================================================================
#  FILE    : build.bash
#  AUTHOR  : callmekohei <callmekohei at gmail.com>
#  License : MIT license
# ===========================================================================

#!/bin/bash

# fit your file path
FSX_PATH=./src/main.fsx
Lib_PATH=./.paket/load/net471/main.group.fsx


# see also
# Getting Started with Paket > Manual setup
# https://fsprojects.github.io/Paket/getting-started.html#Manual-setup
function download_paket_bootstrapper(){

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

install_lib() (

    local foo="
        generate_load_scripts: true
        source https://www.nuget.org/api/v2
        nuget FSharp.Control.Reactive
    "

    if ! type paket >/dev/null 2>&1 ; then
        download_paket_bootstrapper
        mono ./.paket/paket.exe init
        echo "${foo}" > ./paket.dependencies
        mono ./.paket/paket.exe install
    else
        if [ ! -f ./packages/ ] ; then
            paket init
            echo "${foo}" > ./paket.dependencies
            paket install
        fi
    fi
)


create_exe_file() (
    declare -a local arr=(
        "${FSX_PATH}"
        --target:library
        --nologo
        --simpleresolution
        -r:$(dirname $(which sdb))/../lib/sdb/sdb.exe
        --out:./bin/sdbplg.dll
    )
    fsharpc "${arr[@]}"
)


arrange_text() {
    local line
    while read -r line
    do
        echo "${line}" \
        | sed -e 's/#r //g' \
              -e 's/"//g'   \
        | grep --color=never -e "^\." \
        | sed -e 's|^.*packages|\./packages|g'
    done
}


copy_dll_to_bin_folder() {
    local line
    while read -r line
    do
        cp "${line}" ./bin/
    done
}

if [ -e ./bin/ ] ; then
    echo 'do nothing!'
else
    mkdir ./bin/
    install_lib
    if [ "$?" = 0 ] ; then
        create_exe_file
        cat "${Lib_PATH}" | arrange_text | copy_dll_to_bin_folder
    fi
fi




