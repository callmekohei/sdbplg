// ===========================================================================
//  FILE    : sdbCommands.fsx
//  AUTHOR  : callmekohei <callmekohei at gmail.com>
//  License : MIT license
// ===========================================================================

(*
    The following code is written with reference to mono/sdb/src/Commands.
    see : https://github.com/mono/sdb/tree/master/src/Commands
    Thanks @alexrp
*)

namespace sdbCommands

open System
open System.IO

#r "/usr/local/lib/sdb/sdb.exe"
#r "/usr/local/lib/sdb/Mono.Debugging.dll"
#r "/usr/local/lib/sdb/Mono.Debugging.Soft.dll"
open Mono.Debugger.Client
open Mono.Debugging.Client


module SDBCommands =

    let localVariables () = async {
        try
            let f = Debugger.ActiveFrame

            if (f = null) then
                if (Debugger.State <> State.Exited) then
                    Log.Info("Backtrace for this thread is unavailable")
                else
                    Log.Error("No active stack frame")
            else


                let vals = f.GetLocalVariables()

                if (vals.Length = 0) then
                    Log.Info("No locals")
                else
                    for v in vals do
                        let strErr = Utilities.StringizeValue(v)

                        if (snd strErr) then
                            Log.Error("{0}<error>{1} {2} = {3}", Color.DarkRed, Color.Reset, v.Name, fst strErr)
                        else
                            Log.Info("{0}{1}{2} {3} = {4}", Color.DarkGreen, v.TypeName, Color.Reset, v.Name, fst strErr)

        with e -> Log.Info(e.Message)
    }


    let stack() = async {
        try
            let f = Debugger.ActiveFrame

            if (f = null) then
                if (Debugger.State <> State.Exited) then
                    Log.Info("Backtrace for this thread is unavailable")
                else
                    Log.Error("No active stack frame")
            else
                Log.Emphasis(Utilities.StringizeFrame(f, true))

        with e -> Log.Info(e.Message)
    }


    let backTrace() = async{

        try
            let p = Debugger.ActiveProcess
            let t = Debugger.ActiveThread

            if (p = null) then
                Log.Error("No active inferior process")
            elif t = null then
                Log.Error("No active thread")
            else
                let threads = p.GetThreads()

                for i in [0..(threads.Length - 1)] do
                    let t = threads.[i]
                    let str = Utilities.StringizeThread(t, false)

                    if (t = Debugger.ActiveThread) then
                        Log.Emphasis(str)
                    else
                        Log.Info(str)

                    let bt = t.Backtrace

                    if (bt.FrameCount <> 0) then
                        for j in [0..(bt.FrameCount - 1)] do
                            let f = bt.GetFrame(j);
                            let fstr = Utilities.StringizeFrame(f, true)

                            if (f = Debugger.ActiveFrame) then
                                Log.Emphasis(fstr)
                            else
                                Log.Info(fstr)
                    else
                        Log.Info("Backtrace for this thread is unavailable")

                    if (i < threads.Length - 1) then
                        Log.Info(String.Empty)

        with e -> Log.Info(e.Message)
    }




    let threadList() = async {

        try

            let p = Debugger.ActiveProcess
            if (p = null) then
                Log.Error("No active inferior process")
            else
                let t = Debugger.ActiveThread
                if t = null then
                    Log.Error("No active thread")
                else
                    let threads = p.GetThreads()

                    let mutable i = 0
                    // for (var i = 0; i < threads.Length; i++)
                    for i in [0..(threads.Length - 1)] do
                        let t = threads.[i]
                        let str = Utilities.StringizeThread(t, true);

                        if (t = Debugger.ActiveThread) then
                            Log.Emphasis(str)
                        else
                            Log.Info(str)

                        if (i < (threads.Length - 1)) then
                            Log.Info(String.Empty)

        with e -> Log.Info(e.Message)
    }


    let thread() =
        try

            let t = Debugger.ActiveThread

            if (t = null) then
                Log.Error("No active thread")
            else
                let str = Utilities.StringizeThread(t, true)

                if (t = Debugger.ActiveThread) then
                    Log.Emphasis(str)
                else
                    Log.Info(str)

        with e -> Log.Info(e.Message)


    let watches() = async{
        try
            for pair in Debugger.Watches do
                let f            = Debugger.ActiveFrame
                let prefix       = pair.Key.ToString()
                let variableName = pair.Value
                let typeName     = f.GetExpressionValue(pair.Value, Debugger.Options.EvaluationOptions).TypeName
                let value        = f.GetExpressionValue(pair.Value, Debugger.Options.EvaluationOptions).Value

                Log.Info("#{0} '{1}':{2}{3}{4} it = {5}", prefix,variableName, Color.DarkGreen, typeName, Color.Reset, value);

        with e -> Log.Info(e.Message)
    }


    let Assembly() = async {
        try
            let f = Debugger.ActiveFrame

            if (f = null) then
                Log.Error("No active stack frame")
            else
                let lower = -5
                let upper = 10

                let asm = f.Disassemble(lower, upper)

                for line in asm do
                    if not line.IsOutOfRange then
                        let str = String.Format("0x{0:X8}    {1}", line.Address, line.Code)
                        if (line.Address = f.Address) then
                            Log.Emphasis(str)
                        else
                            Log.Info(str)

        with e -> Log.Info(e.Message)
    }


    let Source (args:string) =

        let f = Debugger.ActiveFrame

        if (f = null) then
            Log.Error("No active stack frame")
        else

            let lower = 5
            let upper = 5

            let loc  = f.SourceLocation
            let file = loc.FileName
            let line = loc.Line

            if file <> null && line <> -1 then
                if not ( File.Exists(file) ) then
                    Log.Error("Source file '{0}' not found", file);
                else
                    try
                        use reader = new StreamReader (file)

                        let exec = Debugger.CurrentExecutable

                        if (exec <> null && File.GetLastWriteTime(file) > exec.LastWriteTime) then
                            Log.Notice("Source file '{0}' is newer than the debuggee executable", file)

                        let mutable cur:int = 0

                        while ( not reader.EndOfStream ) do
                            let str = reader.ReadLine()

                            let i = line - cur
                            let j = cur - line

                            if (i > 0 && i < lower + 2 || j >= 0 && j < upper) then

                                if (cur = line - 1) then
                                    Log.Info( String.Format("{0,8}: >> {1}" , cur + 1 , Color.Red + str + Color.Reset) )
                                else
                                    Log.Info( String.Format("{0,8}:    {1}", cur + 1, str) )

                            cur <- cur + 1
                    with e ->
                        Log.Error("Could not open source file '{0}'", file)
                        Log.Error( e.Message )

            else
                Log.Error("No source information available")


    let run (args:string) =

        if (Debugger.State <> State.Exited) then
            Log.Error("an inferior process is already being debugged")
            ()

        elif (args.Length = 0) && (Debugger.CurrentExecutable = null) then
            Log.Error("no program path given (and no previous program to re-run)")
            ()

        elif (args.Length = 0) && (Debugger.CurrentExecutable <> null) then

            try
                let file = new FileInfo(Debugger.CurrentExecutable.FullName)
                Debugger.Run(file)
            with e ->
                Log.Error("could not open file '{0}':", args)
                Log.Error( e.Message )

        elif not (File.Exists( args )) then
            Log.Error("program executable '{0}' does not exist", args)
            ()

        else
            try
                let file = new FileInfo(args)
                Debugger.Run(file)
            with e ->
                Log.Error("could not open file '{0}':", args)
                Log.Error( e.Message )


    let stepOver() =
        try
            if (Debugger.State = State.Suspended) then
                Debugger.StepOverLine()
            else
                Log.Error("No suspended inferior process")
        with e -> Log.Info(e.Message)

    let stepInto() =
        try
            if (Debugger.State = State.Suspended) then
                Debugger.StepIntoLine()
            else
                Log.Error("No suspended inferior process")
        with e -> Log.Info(e.Message)

    let stepOut() =
        try
            if (Debugger.State = State.Suspended) then
                Debugger.StepOutOfMethod()
            else
                Log.Error("No suspended inferior process")
        with e -> Log.Info(e.Message)

    let Continue() =
        try
            if (Debugger.State = State.Exited) then
                Log.Error("No inferior process")
            else
                Debugger.Continue()
        with e -> Log.Info(e.Message)
