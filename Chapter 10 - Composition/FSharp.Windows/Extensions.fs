namespace FSharp.Windows
 
open System
open LanguagePrimitives

[<AutoOpen>]
module Extensions = 

    let inline undefined<'T> = raise<'T> <| NotImplementedException()

    let inline positive x = GenericGreaterThan x GenericZero

[<RequireQualifiedAccess>]
module Observable =
    let mapTo value = Observable.map(fun _ -> value)
    let unify first second = Observable.merge (Observable.map Choice1Of2 first) (Observable.map Choice2Of2 second)

