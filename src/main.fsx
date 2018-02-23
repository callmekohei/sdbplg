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
open System.Collections.Concurrent

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


    let gatherOutput f args =

        try
            // Switch from StandardOut to MemoryStream
            use ms = new MemoryStream()
            use sw = new StreamWriter(ms)
            use tw = TextWriter.Synchronized(sw)
            sw.AutoFlush <- true
            Console.SetOut(tw)

            f args

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

            s.Value

        with e -> e.Message


    type DisplayItems = EXP | BT | SRC | OP | ASM

    let cq = new ConcurrentQueue<DisplayItems>()

    let expressions_ (width) =
        Log.Info( Color.DarkBlue + "─── " + Color.DarkYellow + "Expressions "     + Color.DarkBlue  + String.replicate (width - 4 - 12) "─" )
        localVariables() |> Async.RunSynchronously
        watches()        |> Async.RunSynchronously

    let backTrace_ (width) =
        Log.Info( Color.DarkBlue + "─── " + Color.DarkYellow + "BackTrace "       + Color.DarkBlue  + String.replicate (width - 4 - 10) "─" )
        backTrace() |> Async.RunSynchronously

    let source_ (width, args) =
        Log.Info( Color.DarkBlue + "─── " + Color.DarkYellow + "Source "          + Color.DarkBlue  + String.replicate (width - 4 -  9) "─" )
        Source(args)

    let assembly_ (width) =
        Log.Info( Color.DarkBlue + "─── " + Color.DarkYellow + "Assembly "        + Color.DarkBlue  + String.replicate (width - 4 -  9) "─" )
        Assembly() |> Async.RunSynchronously

    let output_ (width, s) =
        Log.Info( Color.DarkBlue + "─── " + Color.DarkYellow + "Output/messages " + Color.DarkBlue  + String.replicate (width - 4 - 16) "─" )
        Log.Info(s)

    let func args s =
        System.Console.Clear()
        let width = System.Console.WindowWidth

        if cq.Count = 0 then
            [EXP;BT;SRC;OP] |> List.iter( fun di -> cq.Enqueue(di) )

        cq.ToArray()
        |> Array.iter( fun x ->
                    match x with
                    | EXP -> expressions_(width)
                    | BT  -> backTrace_(width)
                    | SRC -> source_(width, s)
                    | ASM -> assembly_(width)
                    | OP  -> output_(width, s) )

        // enable to echoback
        Process.Start("stty","echo")    |> ignore
        System.Threading.Thread.Sleep 5 |> ignore

    type MyRun() =
        inherit Command()
        override __.Names         = [|"run"|]
        override __.Summary       = ""
        override __.Syntax        = ""
        override __.Help          = ""
        override __.Process(args) = func args ( gatherOutput run args )

    type MyStepOver() =
        inherit Command()
        override __.Names         = [|"stepover"|]
        override __.Summary       = ""
        override __.Syntax        = ""
        override __.Help          = ""
        override __.Process(args) = func args ( gatherOutput stepOver () )

    type MyStepInto() =
        inherit Command()
        override __.Names         = [|"stepinto"|]
        override __.Summary       = ""
        override __.Syntax        = ""
        override __.Help          = ""
        override __.Process(args) = func args ( gatherOutput stepInto () )

    type MyStepOut() =
        inherit Command()
        override __.Names         = [|"stepout"|]
        override __.Summary       = ""
        override __.Syntax        = ""
        override __.Help          = ""
        override __.Process(args) = func args ( gatherOutput stepOut () )

    type MyContinue() =
        inherit Command()
        override __.Names         = [|"continue"|]
        override __.Summary       = ""
        override __.Syntax        = ""
        override __.Help          = ""
        override __.Process(args) = func args ( gatherOutput Continue () )

    type MyDisplay() =
        inherit Command()
        override __.Names         = [|"display"|]
        override __.Summary       = ""
        override __.Syntax        = ""
        override __.Help          = ""
        override __.Process(args) =

            let keywords = ["expressions";"backtrace";"source";"assembly";"output"]

            if cq.Count <> 0 then
                cq.ToArray()
                |> Array.map( fun x ->
                            match x with
                            | DisplayItems.EXP -> "expressions"
                            | DisplayItems.BT  -> "backtrace"
                            | DisplayItems.SRC -> "source"
                            | DisplayItems.ASM -> "assembly"
                            | DisplayItems.OP  -> "output" )
                |> Array.reduce( fun a b -> a + " " + b )
                |> fun s -> printfn "your display orders: %s" s

            if args = String.Empty then
                printfn "please choise following"
                keywords
                |> List.reduce( fun a b -> a + " " + b )
                |> fun s -> printfn "%s" s

            else
                cq.Clear()

                args.Split(' ')
                |> Array.iter( fun s ->
                    if keywords |> List.exists( fun keyword -> keyword = s.ToLower() ) then
                        match s.ToLower() with
                        | "expressions" -> cq.Enqueue( DisplayItems.EXP )
                        | "backtrace"   -> cq.Enqueue( DisplayItems.BT  )
                        | "source"      -> cq.Enqueue( DisplayItems.SRC )
                        | "assembly"    -> cq.Enqueue( DisplayItems.ASM )
                        | "output"      -> cq.Enqueue( DisplayItems.OP  )
                        | _ -> ()
                )

                if cq.Count <> 0 then
                    cq.ToArray()
                    |> Array.map( fun x ->
                                match x with
                                | DisplayItems.EXP -> "expressions"
                                | DisplayItems.BT  -> "backtrace"
                                | DisplayItems.SRC -> "source"
                                | DisplayItems.ASM -> "assembly"
                                | DisplayItems.OP  -> "output" )
                    |> Array.reduce( fun a b -> a + " " +  b )
                    |> fun s -> printfn "your display orders: %s" s

    [<Sealed; Command>]
    type MyCommand() =
        inherit MultiCommand()
        do base.AddCommand<MyRun>()
        do base.AddCommand<MyStepOver>()
        do base.AddCommand<MyStepInto>()
        do base.AddCommand<MyStepOut>()
        do base.AddCommand<MyContinue>()
        do base.AddCommand<MyDisplay>()
        override this.Names   = [|"foo"|]
        override this.Summary = ""
        override this.Syntax  = ""
        override this.Help    = ""
