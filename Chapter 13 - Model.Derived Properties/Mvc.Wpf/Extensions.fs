namespace Mvc.Wpf
 
open System
open LanguagePrimitives
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

[<AutoOpen>]
module Extensions = 

    let inline undefined<'T> = raise<'T> <| NotImplementedException()

    let inline positive x = GenericGreaterThan x GenericZero

    type PropertySelector<'T, 'a> = Expr<('T -> 'a)>

    let (|SingleStepPropertySelector|) (expr : PropertySelector<'T, 'a>) = 
        match expr with 
        | Lambda(arg, PropertyGet( Some (Var selectOn), property, [])) -> 
            assert(arg.Name = selectOn.Name)
            property.Name, fun(this : 'T) -> property.GetValue(this, [||]) |> unbox<'a>
        | _ -> invalidArg "Property selector quotation" (string expr)


[<RequireQualifiedAccess>]
module Observable =
    let mapTo value = Observable.map(fun _ -> value)
    let unify first second = Observable.merge (Observable.map Choice1Of2 first) (Observable.map Choice2Of2 second)

