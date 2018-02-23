// ===========================================================================
//  FILE    : main.fsx
//  AUTHOR  : callmekohei <callmekohei at gmail.com>
//  License : MIT license
// ===========================================================================

namespace Mono.Debugger.Client.Commands

#load "./sdbCommands.fsx"
open sdbCommands.SDBCommands

open System
open System.IO
open System.Text
open System.Diagnostics

#r "/usr/local/lib/sdb/sdb.exe"
#r "/usr/local/lib/sdb/Mono.Debugging.dll"
#r "/usr/local/lib/sdb/Mono.Debugging.Soft.dll"
open Mono.Debugger.Client
open Mono.Debugging.Client

#r @"../packages/System.Reactive.Core/lib/net46/System.Reactive.Core.dll"
#r @"../packages/System.Reactive.Linq/lib/net46/System.Reactive.Linq.dll"
#r @"../packages/System.Reactive.Interfaces/lib/net45/System.Reactive.Interfaces.dll"
#r @"../packages/System.Reactive.PlatformServices/lib/net46/System.Reactive.PlatformServices.dll"
#r @"../packages/FSharp.Control.Reactive/lib/net45/FSharp.Control.Reactive.dll"
open FSharp.Control.Reactive


type Generator()  =
    let m_Event = new Event<_>()

    do
        m_Event.Publish
        |> Observable.throttle ( System.TimeSpan.FromMilliseconds(1000.) )
        |> Observable.add ( fun ( flg:ref<bool> ) -> flg := false )
        |> ignore

    member this.PrintOut( str  : ref<string>
                        , flg  : ref<bool>
                        , pLen : ref<int>
                        , f    : System.IO.MemoryStream * int -> string
                        , ms   : System.IO.MemoryStream
                        ) =

        let endMarks = ["exited";"Hit breakpoint at";"suspended"]

        if endMarks |> List.exists( fun endMark -> (str.Value).Contains(endMark) ) then
            flg := false
        else
            str := f(ms, 50)
            if pLen.Value <> (str.Value).Length then
                m_Event.Trigger( flg )
            pLen := (str.Value).Length


module Foo =

    let ggg = Generator()


    let gatherOutputImpl(ms:MemoryStream, time_ms:int) =

        let sr = new System.IO.StreamReader(ms)
        let mutable tmp = int64 0
        let mutable flg = true

        while flg do

            System.Threading.Thread.Sleep time_ms

            if tmp = int64 0 then
                tmp <- ms.Position
            else
                if tmp = ms.Position then
                    flg <- false
                else
                    tmp <- ms.Position

        ms.Position <- int64 0
        sr.ReadToEnd()


    let gatherOutput f args = async {

        try

            // Switch from StandardOut to MemoryStream
            use ms = new MemoryStream()
            use sw = new StreamWriter(ms)
            use tw = TextWriter.Synchronized(sw)
            sw.AutoFlush <- true
            Console.SetOut(tw)

            do! f args

            // read from MemoryStream
            let s = ref ""
            let flg = ref true
            let prevLength = ref 0
            while flg.Value do
                ggg.PrintOut( s, flg, prevLength, gatherOutputImpl, ms )

            // Switch from MemoryStream to StandardOut
            let std = new StreamWriter(Console.OpenStandardOutput())
            std.AutoFlush <- true
            Console.SetOut(std)

            return s.Value

        with e -> return e.Message
    }


    // command foo

    let func args s = async {

        System.Console.Clear()

        let width = System.Console.WindowWidth
        let line00 = Color.DarkBlue + "─── " + Color.DarkYellow + "Expressions "     + Color.DarkBlue  + String.replicate (width - 4 - 12) "─"
        let line01 = Color.DarkBlue + "─── " + Color.DarkYellow + "BackTrace "       + Color.DarkBlue  + String.replicate (width - 4 - 10) "─"
        let line02 = Color.DarkBlue + "─── " + Color.DarkYellow + "Source "          + Color.DarkBlue  + String.replicate (width - 4 -  9) "─"
        let line03 = Color.DarkBlue + "─── " + Color.DarkYellow + "Output/messages " + Color.DarkBlue  + String.replicate (width - 4 - 16) "─"

        Log.Info(line00)
        localVariables() |> Async.RunSynchronously
        watches()        |> Async.RunSynchronously

        Log.Info(line01)
        backTrace()         |> Async.RunSynchronously

        Log.Info(line02)
        Source(args)

        Log.Info(line03)
        Log.Info(s)

        // enable to echoback
        Process.Start("stty","echo") |> ignore
        System.Threading.Thread.Sleep 5
        ()

    }

    type MyRun() =
        inherit Command()
        override __.Names         = [|"run"|]
        override __.Summary       = ""
        override __.Syntax        = ""
        override __.Help          = ""
        override __.Process(args) = func args (gatherOutput run args |> Async.RunSynchronously) |> Async.RunSynchronously

    type MyStepOver() =
        inherit Command()
        override __.Names         = [|"stepover"|]
        override __.Summary       = ""
        override __.Syntax        = ""
        override __.Help          = ""
        override __.Process(args) = func args (gatherOutput stepOver () |> Async.RunSynchronously) |> Async.RunSynchronously

    type MyStepInto() =
        inherit Command()
        override __.Names         = [|"stepinto"|]
        override __.Summary       = ""
        override __.Syntax        = ""
        override __.Help          = ""
        override __.Process(args) = func args (gatherOutput stepInto () |> Async.RunSynchronously) |> Async.RunSynchronously

    type MyStepOut() =
        inherit Command()
        override __.Names         = [|"stepout"|]
        override __.Summary       = ""
        override __.Syntax        = ""
        override __.Help          = ""
        override __.Process(args) = func args (gatherOutput stepOut () |> Async.RunSynchronously) |> Async.RunSynchronously

    type MyContinue() =
        inherit Command()
        override __.Names         = [|"continue"|]
        override __.Summary       = ""
        override __.Syntax        = ""
        override __.Help          = ""
        override __.Process(args) = func args (gatherOutput Continue () |> Async.RunSynchronously) |> Async.RunSynchronously

    [<Sealed; Command>]
    type MyCommand() =
        inherit MultiCommand()
        do base.AddCommand<MyRun>()
        do base.AddCommand<MyStepOver>()
        do base.AddCommand<MyStepInto>()
        do base.AddCommand<MyStepOut>()
        do base.AddCommand<MyContinue>()
        override this.Names   = [|"foo"|]
        override this.Summary = ""
        override this.Syntax  = ""
        override this.Help    = ""


    // command bar

    let func_bar args s = async {

        System.Console.Clear()

        let width = System.Console.WindowWidth
        let line00 = Color.DarkBlue + "─── " + Color.DarkYellow + "Expressions "     + Color.DarkBlue  + String.replicate (width - 4 - 12) "─"
        let line01 = Color.DarkBlue + "─── " + Color.DarkYellow + "BackTrace "       + Color.DarkBlue  + String.replicate (width - 4 - 10) "─"
        let line02 = Color.DarkBlue + "─── " + Color.DarkYellow + "Assembly "        + Color.DarkBlue  + String.replicate (width - 4 -  9) "─"
        let line03 = Color.DarkBlue + "─── " + Color.DarkYellow + "Output/messages " + Color.DarkBlue  + String.replicate (width - 4 - 16) "─"

        Log.Info(line00)
        localVariables() |> Async.RunSynchronously
        watches()        |> Async.RunSynchronously

        Log.Info(line01)
        backTrace()         |> Async.RunSynchronously

        Log.Info(line02)
        Assembly()       |> Async.RunSynchronously

        Log.Info(line03)
        Log.Info(s)

        // enable to echoback
        Process.Start("stty","echo") |> ignore
        System.Threading.Thread.Sleep 5
        ()

    }

    type MyRunBar() =
        inherit Command()
        override __.Names         = [|"run"|]
        override __.Summary       = ""
        override __.Syntax        = ""
        override __.Help          = ""
        override __.Process(args) = func_bar args (gatherOutput run args |> Async.RunSynchronously) |> Async.RunSynchronously

    type MyStepOverBar() =
        inherit Command()
        override __.Names         = [|"stepover"|]
        override __.Summary       = ""
        override __.Syntax        = ""
        override __.Help          = ""
        override __.Process(args) = func_bar args (gatherOutput stepOver () |> Async.RunSynchronously) |> Async.RunSynchronously

    type MyStepIntoBar() =
        inherit Command()
        override __.Names         = [|"stepinto"|]
        override __.Summary       = ""
        override __.Syntax        = ""
        override __.Help          = ""
        override __.Process(args) = func_bar args (gatherOutput stepInto () |> Async.RunSynchronously) |> Async.RunSynchronously

    type MyStepOutBar() =
        inherit Command()
        override __.Names         = [|"stepout"|]
        override __.Summary       = ""
        override __.Syntax        = ""
        override __.Help          = ""
        override __.Process(args) = func_bar args (gatherOutput stepOut () |> Async.RunSynchronously) |> Async.RunSynchronously

    type MyContinueBar() =
        inherit Command()
        override __.Names         = [|"continue"|]
        override __.Summary       = ""
        override __.Syntax        = ""
        override __.Help          = ""
        override __.Process(args) = func_bar args (gatherOutput Continue () |> Async.RunSynchronously) |> Async.RunSynchronously

    [<Sealed; Command>]
    type MyCommandBar() =
        inherit MultiCommand()
        do base.AddCommand<MyRunBar>()
        do base.AddCommand<MyStepOverBar>()
        do base.AddCommand<MyStepIntoBar>()
        do base.AddCommand<MyStepOutBar>()
        do base.AddCommand<MyContinueBar>()
        override this.Names   = [|"bar"|]
        override this.Summary = ""
        override this.Syntax  = ""
        override this.Help    = ""
