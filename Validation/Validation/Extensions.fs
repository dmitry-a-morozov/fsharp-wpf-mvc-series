namespace Mvc.Wpf
 
open System
open LanguagePrimitives

[<AutoOpen>]
module Extensions = 

    let inline undefined<'T> = raise<'T> <| NotImplementedException()

    //let isNull x = x = null 
    let isNotNull x = x <> null

    let inline positive x = GenericGreaterThan x GenericZero

[<RequireQualifiedAccess>]
module Observable =
    let mapTo value = Observable.map(fun _ -> value)

