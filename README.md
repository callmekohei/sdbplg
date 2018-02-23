[![MIT-LICENSE](http://img.shields.io/badge/license-MIT-blue.svg?style=flat)](https://github.com/callmekohei/tigaDebugger/blob/master/LICENSE)

[![Gitter](https://img.shields.io/gitter/room/nwjs/nw.js.svg)](https://gitter.im/vim-jp/reading-vimrc)

# sdbplg

[mono/sdb](https://github.com/mono/sdb) plugins

sdbplg gives you compact viewing.
<br>
<br>

## ScreenShot

![alt text](./pic/20180223-2.gif)

## Installing
sdbplg requires mono and mono/sdb installed.

```
$ git clone --depth 1 https://github.com/callmekohei/sdbplg
```

## About Compile
```
$ bash build.bash
```

## Set Path
Put the following string to `.bash_profile`
```
export SDB_PATH=/PATH/TO/sdbplg/bin
```


## Set Shortcut key

Put the following file to `$HOME/.sdb.rc`
```
$ vim .sdb.rc

alias add r       foo run
alias add n       foo stepover
alias add i       foo stepinto
alias add u       foo stepout
alias add c       foo stepcontinue
alias add display foo display
r
```

## Usage
```
// create foo.fsx
$ vim foo.fsx

let foo() =
    let mutable x = 1
    x <- 2
    x <- 3
    x

[<EntryPointAttribute>]
let main _ =
    let y = foo()
    stdout.WriteLine(y)
    0

// compiel file
$ fsharpc -g --optimize- foo.fsx

// open file
$ sdb 'run foo.exe'

// set break point
(sdb) bp add func Foo.main

// start debug
(sdb) r

// step in
(sdb) foo stepin

// step over
(sdb) foo stepover

// step out
(sdb) foo stepout

// continue
(sdb) foo continue

// quit
(sdb) quit
```

### Display command
You can display each modules with your favarite.
```
(sdb) foo display
please choise following
expressions backtrace source assembly output

(sdb) foo display source output

─── Source ──────────────────────────────────────────────
No active stack frame
─── Output/messages ─────────────────────────────────────
no program path given (and no previous program to re-run)
```
