[<AutoOpen>]
module Mvc.Wpf.Validation

open System
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

type PropertySelector<'T, 'a> = Expr<('T -> 'a)>

let (|OneStepPropertySelector|) (expr : PropertySelector<'T, 'a>) = 
    match expr with 
    | Lambda(arg, PropertyGet( Some (Var selectOn), property, [])) -> 
        assert(arg.Name = selectOn.Name)
        property.Name, fun (this : 'T) -> property.GetValue(this, [||]) |> unbox<'a>
    | _ -> invalidArg "Property selector quotation" (string expr)

let setError<'M, 'a when 'M :> Model>(OneStepPropertySelector(propertyName, _) : PropertySelector<'M, 'a>) message (model : 'M) = 
    model.SetError(propertyName, message)

let clearError expr = setError expr null

let invalidIf (OneStepPropertySelector(propertyName, getValue) : PropertySelector<_, _>) predicate message (model : #Model) = 
    if model |> getValue |> predicate then model.SetError(propertyName, message)

let assertThat expr predicate = invalidIf expr (not << predicate)

let objectRequired expr = invalidIf expr ((=) null) "Required field."
let valueRequired expr = assertThat expr (fun (x : Nullable<_>) -> x.HasValue) "Required field."
let textRequired expr = invalidIf expr String.IsNullOrWhiteSpace "Required field."

let inline positive expr = assertThat expr positive  "Must be positive number."

