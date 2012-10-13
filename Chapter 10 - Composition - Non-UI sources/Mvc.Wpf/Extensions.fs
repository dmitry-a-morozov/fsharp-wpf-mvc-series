namespace Mvc.Wpf
 
open System
open LanguagePrimitives

[<AutoOpen>]
module Extensions = 

    let inline undefined<'T> = raise<'T> <| NotImplementedException()

    let inline positive x = GenericGreaterThan x GenericZero

    type IObservable<'T> with
        member first.Unify second = Observable.merge (Observable.map Choice1Of2 first) (Observable.map Choice2Of2 second)

[<RequireQualifiedAccess>]
module Observable =
    let mapTo value = Observable.map(fun _ -> value)

