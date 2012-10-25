[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Mvc.Wpf.Binding

open System
open System.Collections.Generic
open System.Reflection
open System.Windows
open System.Windows.Data 
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns
open Unchecked

type PropertyInfo with
    member this.DependencyProperty : DependencyProperty = 
        this.DeclaringType
            .GetField(this.Name + "Property", BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.FlattenHierarchy)
            .GetValue(null, [||]) 
            |> unbox

type IEnumerable<'T> with
    member this.CurrentItem : 'T = undefined

type IValueConverter with 
    static member Create(convert : 'a -> 'b, convertBack : 'b -> 'a) =  
        {
            new IValueConverter with
                member this.Convert(value, _, _, _) = try value |> unbox |> convert |> box with _ -> DependencyProperty.UnsetValue
                member this.ConvertBack(value, _, _, _) = try value |> unbox |> convertBack |> box with _ -> DependencyProperty.UnsetValue
        }
    static member OneWay convert = IValueConverter.Create(convert, fun _ -> DependencyProperty.UnsetValue)

    member this.Apply _ = undefined

module BindingPatterns = 

    let (|Target|_|) expr = 
        let rec loop = function
            | Some( Value(obj, viewType) ) -> obj
            | Some( FieldGet(tail, field) ) ->  field.GetValue(loop tail)
            | Some( PropertyGet(tail, prop, []) ) -> prop.GetValue(loop tail, [||])
            | _ -> null
        match loop expr with
        | :? FrameworkElement as target -> Some target
        | _ -> None

    let (|PropertyPath|_|) expr = 
        let rec loop e acc = 
            match e with
            | PropertyGet( Some tail, property, []) -> 
                loop tail (property.Name :: acc)
            | SpecificCall <@ Seq.empty.CurrentItem @> (None, _, [ tail ]) -> 
                loop tail ("/" :: acc)
            | Value _ | Var _ -> Some acc
            | _ -> None

        loop expr [] |> Option.map(fun xs -> 
            xs.Head + (
                xs
                |> Seq.pairwise
                |> Seq.map(function 
                    | "/", x -> x 
                    | _, "/" -> "/" 
                    | x, y -> "." + y) 
                |> String.concat ""
            )
        )

    let (|StringFormat|_|) = function
        | SpecificCall <@ String.Format : string * obj -> string @> (None, [], [ Value(:? string as format, _); Coerce( propertyPath, _) ]) ->
            Some(format, propertyPath)
        | _ -> None    

    let (|Nullable|_|) = function
        | NewObject( ctorInfo, [ propertyPath ] ) when ctorInfo.DeclaringType.GetGenericTypeDefinition() = typedefof<Nullable<_>> -> 
            Some propertyPath
        | _ -> None    

    let (|Converter|_|) = function
        | Call(None, method', [ propertyPath ]) -> 
            assert (method'.IsStatic && method'.GetParameters().Length = 1)
            Some((fun(value : obj) -> method'.Invoke(null, [| value |])), propertyPath )
        | _ -> None    
         
    let rec (|BindingExpression|) = function
        | PropertyPath path -> 
            Binding path
        | Coerce( BindingExpression binding, _) 
        | SpecificCall <@ string @> (None, _, [ BindingExpression binding ]) 
        | Nullable( BindingExpression binding) -> 
            binding
        | StringFormat(format, BindingExpression binding) -> 
            binding.StringFormat <- format
            binding
        | Converter(convert, BindingExpression binding) -> 
            binding.Mode <- BindingMode.OneWay
            binding.Converter <- IValueConverter.OneWay convert
            binding
        | Call(None, method', [ Value(:? IValueConverter as converter, _); BindingExpression binding ] ) when method'.Name = "IValueConverter.Apply" -> 
            binding.Converter <- converter
            binding
        | expr -> invalidArg "binding property path quotation" (string expr)

open BindingPatterns

type Expr with
    member this.ToBindingExpr(?mode, ?updateSourceTrigger, ?fallbackValue, ?targetNullValue, ?validatesOnDataErrors, ?validatesOnExceptions) = 
        match this with
        | PropertySet(Target target, targetProperty, [], BindingExpression binding) ->

            if mode.IsSome then binding.Mode <- mode.Value
            if updateSourceTrigger.IsSome then binding.UpdateSourceTrigger <- updateSourceTrigger.Value
            if fallbackValue.IsSome then binding.FallbackValue <- fallbackValue.Value
            if targetNullValue.IsSome then binding.TargetNullValue <- targetNullValue.Value
            binding.ValidatesOnDataErrors <- defaultArg validatesOnDataErrors true
            binding.ValidatesOnExceptions <- defaultArg validatesOnExceptions true

            target.SetBinding(targetProperty.DependencyProperty, binding)

        | _ -> invalidArg "expr" (string this) 

type Binding with
    static member FromExpression(expr, ?mode, ?updateSourceTrigger, ?fallbackValue, ?targetNullValue, ?validatesOnDataErrors, ?validatesOnExceptions) =
        let rec split = function 
            | Sequential(head, tail) -> head :: split tail
            | tail -> [ tail ]

        for e in split expr do
            let be = e.ToBindingExpr(?mode = mode, ?updateSourceTrigger = updateSourceTrigger, ?fallbackValue = fallbackValue, 
                                     ?targetNullValue = targetNullValue, ?validatesOnDataErrors = validatesOnDataErrors, ?validatesOnExceptions = validatesOnExceptions)
            assert not be.HasError
    
    static member UpdateSourceOnChange expr = Binding.FromExpression(expr, updateSourceTrigger = UpdateSourceTrigger.PropertyChanged)
    static member TwoWay expr = Binding.FromExpression(expr, BindingMode.TwoWay)
    static member OneWay expr = Binding.FromExpression(expr, BindingMode.OneWay)

open System.Windows.Controls
open System.Windows.Controls.Primitives

type Selector with
    member this.SetBindings(itemsSource : Expr<#seq<'Item>>, ?selectedItem : Expr<'Item>, ?displayMember : PropertySelector<'Item, _>) = 

        let e = this.SetBinding(ItemsControl.ItemsSourceProperty, match itemsSource with BindingExpression binding -> binding)
        assert not e.HasError

        selectedItem |> Option.iter(fun(BindingExpression binding) -> 
            let e = this.SetBinding(DataGrid.SelectedItemProperty, binding)
            assert not e.HasError
            this.IsSynchronizedWithCurrentItem <- Nullable true
        )

        displayMember |> Option.iter(fun(SingleStepPropertySelector(propertyName, _)) -> 
            this.DisplayMemberPath <- propertyName
        )
        
type DataGrid with
    member this.SetBindings(itemsSource : Expr<#seq<'Item>>, columnBindings : 'Item -> (#DataGridBoundColumn * Expr) list, ?selectedItem) = 

        this.SetBindings(itemsSource, ?selectedItem = selectedItem)

        for column, BindingExpression binding in columnBindings Unchecked.defaultof<'Item> do
            column.Binding <- binding

