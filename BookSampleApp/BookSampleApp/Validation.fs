[<RequireQualifiedAccess>]
module Validation

open System
open System.ComponentModel
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

type PropertySelector<'T, 'a> = Expr<('T -> 'a)>

let (|SingleStepPropertySelector|) (expr : PropertySelector<'T, 'a>) = 
    match expr with 
    | Lambda(arg, PropertyGet( Some (Var selectOn), property, [])) -> 
        assert(arg.Name = selectOn.Name)
        property.Name, fun(this : 'T) -> property.GetValue(this, [||]) |> unbox<'a>
    | _ -> invalidArg "Property selector quotation" (string expr)

let hasErrors (model : #INotifyDataErrorInfo) = model.HasErrors

let inline addError( SingleStepPropertySelector(propertyName, _) : PropertySelector< ^Model, _>) message (model : #INotifyDataErrorInfo) = 
    let errors = message :: (propertyName |> model.GetErrors |> Seq.cast<string> |> List.ofSeq)
    (^Model : (member SetErrors : string * string list -> unit) (model, propertyName, errors))

let inline clearError(SingleStepPropertySelector(propertyName, _) : PropertySelector< ^Model, _>) model = 
    (^Model : (member SetErrors : string * string list -> unit) (model, propertyName, []))

let inline invalidIf (SingleStepPropertySelector(_, getValue : ^Model -> _) as property) predicate message model = 
    if model |> getValue |> predicate then model |> addError property message 

let inline assertThat expr predicate = invalidIf expr (not << predicate)

let inline objectRequired expr = invalidIf expr ((=) null) "Required field."
let inline valueRequired expr = assertThat expr (fun(x : Nullable<_>) -> x.HasValue) "Required field."
let inline textRequired expr = invalidIf expr String.IsNullOrWhiteSpace "Required field."

[<RequireQualifiedAccess>]
module Number = 
    open LanguagePrimitives

    let inline positive x = GenericGreaterThan x GenericZero

let inline positive expr = assertThat expr Number.positive "Must be positive number."

