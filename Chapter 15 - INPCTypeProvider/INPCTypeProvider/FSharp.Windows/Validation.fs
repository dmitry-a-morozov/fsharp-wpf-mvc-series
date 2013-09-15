[<RequireQualifiedAccess>]
module FSharp.Windows.Validation

open System
open System.ComponentModel

let hasErrors (model : #INotifyDataErrorInfo) = model.HasErrors

let inline addError( SingleStepPropertySelector(propertyName, _) : PropertySelector< ^Model, _>) message (model : #INotifyDataErrorInfo) = 
    (^Model : (member AddError : string * string -> unit) (model, propertyName, message))

let inline clearError(SingleStepPropertySelector(propertyName, _) : PropertySelector< ^Model, _>) message model = 
    (^Model : (member SetErrors : string * string list -> unit) (model, propertyName, []))

let inline invalidIf( SingleStepPropertySelector(propertyName, getValue : ^Model -> _)) predicate message model = 
    if model |> getValue |> predicate 
    then 
        (^Model : (member AddError : string * string -> unit) (model, propertyName, message))

let inline assertThat expr predicate = invalidIf expr (not << predicate)

let inline objectRequired expr = invalidIf expr ((=) null) "Required field."
let inline valueRequired expr = assertThat expr (fun(x : Nullable<_>) -> x.HasValue) "Required field."
let inline textRequired expr = invalidIf expr String.IsNullOrWhiteSpace "Required field."

let inline positive expr = assertThat expr (fun x -> x > LanguagePrimitives.GenericZero) "Must be positive number."

