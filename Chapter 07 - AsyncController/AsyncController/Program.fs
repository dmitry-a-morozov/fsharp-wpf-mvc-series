
open System
open System.Windows
open System.Diagnostics
open FSharp.Windows
open FSharp.Windows.Sample

[<STAThread>] 
do 
    let model = Model.Create()
    let view = SampleView()
    let controller = SampleController()
    let app = Application()

    let mvc = Mvc(model, view, controller)

    //Using DispatcherUnhandledException
    app.DispatcherUnhandledException.Add <| fun args ->
        let why = args.Exception
        Debug.Fail("DispatcherUnhandledException handler", string why.Message)
        args.Handled <- true

    // Using PreserveStackTraceWrapper wrapper
//    mvc.OnError <- fun _ exn ->
//        let wrapperExn = match exn with | PreserveStackTraceWrapper _  -> exn | inner -> PreserveStackTraceWrapper inner
//        raise wrapperExn
//
//    app.DispatcherUnhandledException.Add <| fun args ->
//        let why = args.Exception.Unwrap()
//        Debug.Fail("DispatcherUnhandledException handler", string why.Message)
//        args.Handled <- true

    mvc.Start() |> ignore
    app.Run view.Window |> ignore

(*
    kabooom button to generate exception
    AsyncInitController
        as base class for async init
        has static factory to create from static member constraints

    Exception handling strategy
        - defualt one rethrow thru undocumented InternalPreserveStackTrace
        - event handler invocation always wrapper in try-with. Optimization can be done 
        - inherit from Mvc to provide custom application-wide strategy
        - or local override thru object expression
*)