[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Mvc.Wpf.Binding

open System.Reflection
open System.Windows
open System.Windows.Data 
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

type PropertyInfo with
    member this.DependencyProperty = 
        let dpInfo = 
            this.DeclaringType.GetField(this.Name + "Property", BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.FlattenHierarchy)
        assert (dpInfo <> null)
        dpInfo.GetValue(null, [||]) |> unbox<DependencyProperty> 

type Expr with
    member this.ToBindingExpr() = 
        match this with
        | PropertySet
            (
                Some( FieldGet( Some( PropertyGet( Some (Value( view, _)), window, [])), control)),
                targetProperty, 
                [], 
                PropertyGet( Some( Value _), sourceProperty, [])
            ) ->
                let target : FrameworkElement = (view, [||]) |> window.GetValue |> control.GetValue |> unbox
                let binding = Binding(path = sourceProperty.Name, ValidatesOnDataErrors = true)
                let bindingExpr = target.SetBinding(targetProperty.DependencyProperty, binding)
                assert not bindingExpr.HasError

        | _ -> invalidArg "expr" (string this) 



