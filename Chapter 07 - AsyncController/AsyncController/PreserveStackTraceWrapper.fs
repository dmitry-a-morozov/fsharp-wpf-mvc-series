[<AutoOpen>]
module FSharp.Windows.Exceptions

exception PreserveStackTraceWrapper of exn

type System.Exception with
    member this.Unwrap() = 
        match this with
        | PreserveStackTraceWrapper inner -> inner.Unwrap()
        | exn -> exn



