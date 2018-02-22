# ===========================================================================
#  FILE    : build.bash
#  AUTHOR  : callmekohei <callmekohei at gmail.com>
#  License : MIT license
# ===========================================================================

# Create bin folder
if [ -e ./bin ] ; then
    rm -rf ./bin
fi

mkdir ./bin


# Create dll file
declare -a arr=(
    fsharpc
    --nologo
    -a
    -r:$(dirname $(which sdb))/../lib/sdb/sdb.exe
    -r:./packages/FSharp.Control.Reactive/lib/net45/FSharp.Control.Reactive.dll
    -r:./packages/System.Reactive.Core/lib/net46/System.Reactive.Core.dll
    -r:./packages/System.Reactive.Linq/lib/net46/System.Reactive.Linq.dll
    -r:./packages/System.Reactive.Interfaces/lib/net45/System.Reactive.Interfaces.dll
    -r:./packages/System.Reactive.PlatformServices/lib/net46/System.Reactive.PlatformServices.dll
    ./sdbplg.fsx
    --out:./bin/sdbplg.dll
)

${arr[@]}
cp ./packages/FSharp.Control.Reactive/lib/net45/FSharp.Control.Reactive.dll ./bin
cp ./packages/System.Reactive.Core/lib/net46/System.Reactive.Core.dll ./bin
cp ./packages/System.Reactive.Linq/lib/net46/System.Reactive.Linq.dll ./bin
cp ./packages/System.Reactive.Interfaces/lib/net45/System.Reactive.Interfaces.dll ./bin
cp ./packages/System.Reactive.PlatformServices/lib/net46/System.Reactive.PlatformServices.dll ./bin



