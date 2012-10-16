[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Mvc.Wpf.Binding

open System.Reflection
open System.Windows
open System.Windows.Data 
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns

type PropertyInfo with
    member this.DependencyProperty = 
        let dpInfo = 
            this.DeclaringType.GetField(this.Name + "Property", BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.FlattenHierarchy)
        assert (dpInfo <> null)
        dpInfo.GetValue(null, [||]) |> unbox<DependencyProperty> 

let rec (|PropertyPath|_|) = function 
    | PropertyGet( Some( Value _), sourceProperty, []) -> Some sourceProperty.Name
    | Coerce( PropertyPath path, _) 
    | SpecificCall <@ string @> (None, _, [ PropertyPath path ]) -> Some path
    | _ -> None

type Expr with
    member this.ToBindingExpr(?mode, ?updateSourceTrigger, ?fallbackValue, ?targetNullValue, ?validatesOnDataErrors) = 
        match this with
        | PropertySet
            (
                Some( FieldGet( Some( PropertyGet( Some (Value( view, _)), window, [])), control)),
                targetProperty, 
                [], 
                PropertyPath path
            ) ->
                let target : FrameworkElement = (view, [||]) |> window.GetValue |> control.GetValue |> unbox

                let binding = Binding(path, ValidatesOnDataErrors = defaultArg validatesOnDataErrors true) 
                if mode.IsSome then binding.Mode <- mode.Value
                if updateSourceTrigger.IsSome then binding.UpdateSourceTrigger <- updateSourceTrigger.Value
                if fallbackValue.IsSome then binding.FallbackValue <- fallbackValue.Value
                if targetNullValue.IsSome then binding.TargetNullValue <- targetNullValue.Value

                target.SetBinding(targetProperty.DependencyProperty, binding)
        | _ -> invalidArg "expr" (string this) 

type Binding with
    static member FromExpression(expr : Expr, ?mode, ?updateSourceTrigger, ?fallbackValue, ?targetNullValue, ?validatesOnDataErrors) =
        let rec split = function 
            | Sequential(head, tail) -> head :: split tail
            | tail -> [ tail ]

        for e in split expr do
            let be = e.ToBindingExpr(?mode = mode, ?updateSourceTrigger = updateSourceTrigger, ?fallbackValue = fallbackValue, ?targetNullValue = targetNullValue, ?validatesOnDataErrors = validatesOnDataErrors)
            assert not be.HasError
    
    static member TwoWay(expr : Expr) = Binding.FromExpression(expr, BindingMode.TwoWay)
    static member OneWay(expr : Expr) = Binding.FromExpression(expr, BindingMode.OneWay)
    static member OneWayToSource(expr : Expr) = Binding.FromExpression(expr, BindingMode.OneWayToSource)
    static member UpdateSourceOnChange(expr : Expr) = Binding.FromExpression(expr, updateSourceTrigger = UpdateSourceTrigger.PropertyChanged)

