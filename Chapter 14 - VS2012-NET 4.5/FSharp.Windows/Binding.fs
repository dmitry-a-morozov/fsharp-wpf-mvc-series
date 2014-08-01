[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FSharp.Windows.Binding

open System
open System.Collections.Generic
open System.Reflection
open System.Diagnostics
open System.Windows
open System.Windows.Data 
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns
open Microsoft.FSharp.Quotations.ExprShape
open Unchecked

type DerivedPropertyAttribute = ReflectedDefinitionAttribute

type IEnumerable<'T> with
    member this.CurrentItem : 'T = undefined

type IValueConverter with 
    static member Create(convert : 'a -> 'b, convertBack : 'b -> 'a) =  {
        new IValueConverter with
            member this.Convert(value, _, _, _) = try value |> unbox |> convert |> box with _ -> DependencyProperty.UnsetValue
            member this.ConvertBack(value, _, _, _) = try value |> unbox |> convertBack |> box with _ -> DependencyProperty.UnsetValue
    }
    static member OneWay convert = IValueConverter.Create(convert, fun _ -> DependencyProperty.UnsetValue)

    member this.Apply _ = undefined

let coerce _ = undefined

module Patterns = 

    type PropertyInfo with
        member this.DependencyProperty : DependencyProperty = 
            this.DeclaringType
                .GetField(this.Name + "Property", BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.FlattenHierarchy)
                .GetValue(null, [||]) 
                |> unbox

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

        loop expr []
        |> Option.map (function 
            | [] -> ""
            | head :: _ as steps ->
                steps 
                |> Seq.pairwise 
                |> Seq.map (function 
                    | "/", x -> x 
                    | _, "/" -> "/" 
                    | x, y -> "." + y) 
                |> String.concat ""
                |> (+) head
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
        | Call(instance, method', [ propertyPath ]) -> 
            let instance = match instance with | Some( Value( value, _)) -> value | _ -> null
            Some((fun(value : obj) -> method'.Invoke(instance, [| value |])), propertyPath )
        | _ -> None    
         
    open DerivedProperties

    let (|DerivedProperty|_|) = function
        | PropertyGet
            ( 
                Some( PropertyPath path), 
                (PropertyGetterWithReflectedDefinition (Lambda (model, Lambda(unitVar, propertyBody))) as property), 
                []
            ) when not property.CanWrite ->
            assert(unitVar.Type = typeof<unit>)
            getMultiBindingForDerivedProperty(path, model, propertyBody, property.GetValue) |> Some
        | _ -> None

    let (|ExtensionDerivedProperty|_|) = function
        | Call
            ( 
                None, 
                (MethodWithReflectedDefinition (Lambda (model, Lambda(unitVar, methodBody))) as getMethod), 
                [ PropertyPath path ]
            ) when getMethod.Name.StartsWith(model.Type.Name + ".get_") -> 
            assert(unitVar.Type = typeof<unit>)
            let getter model = getMethod.Invoke(null, [| model |])
            getMultiBindingForDerivedProperty(path, model, methodBody, getter) |> Some
        | _ -> None
    
    let private getUnboxImpl = 
        let ref = Type.GetType("Microsoft.FSharp.Core.Operators, FSharp.Core").GetMethod("Unbox")
        fun t -> ref.MakeGenericMethod [| t |]

    let private getBoxImpl = 
        let ref = Type.GetType("Microsoft.FSharp.Core.Operators, FSharp.Core").GetMethod("Box")
        fun t -> ref.MakeGenericMethod [| t |]

    let rec (|SourceProperty|_|) = function 
        | PropertyGet( Some _, prop, []) -> Some prop 
        | PropertyGet( Some( PropertyGet( Some _, step1, [])), step2, []) when step2.IsNullableValue -> Some step1 
        | _ -> None

    let rec extractPropertyGetters propertyBody = 
        seq {
            match propertyBody with 
            | PropertyGet _ as getter -> yield getter
            | ShapeVar _ -> ()
            | ShapeLambda(_, body) -> yield! extractPropertyGetters body   
            | ShapeCombination(_, exprs) -> for subExpr in exprs do yield! extractPropertyGetters subExpr
        }

    let (|SinglePropertyExpression|_|) expr = 
        match expr |> extractPropertyGetters |> Seq.toList with
        | [ SourceProperty prop as getterToReplace ] ->
            let propertyValue = Var("value", typeof<obj>)
            let rec replacePropertyWithParam expr = 
                match expr with 
                | PropertyGet _ as getter when getter = getterToReplace -> 
                    Expr.Call(getUnboxImpl prop.PropertyType, [Expr.Var propertyValue])
                | ShapeVar var -> Expr.Var(var)
                | ShapeLambda(var, body) -> Expr.Lambda(var, replacePropertyWithParam body)  
                | ShapeCombination(shape, exprs) -> ExprShape.RebuildShapeCombination(shape, exprs |> List.map(fun e -> replacePropertyWithParam e))

            let converterBody = Expr.Call(getBoxImpl expr.Type, [ replacePropertyWithParam expr ])
            let converter : obj -> obj = 
                Expr.Lambda(propertyValue, converterBody)
                |> Microsoft.FSharp.Linq.RuntimeHelpers.LeafExpressionConverter.EvaluateQuotation 
                |> unbox

            Some (Binding(prop.Name, Mode = BindingMode.OneWay, Converter = IValueConverter.OneWay converter))
        | _ -> None

    let rec (|BindingExpression|) = function
        | DerivedProperty binding 
        | ExtensionDerivedProperty binding -> binding
        | PropertyPath path -> 
            upcast Binding(path)
        | Coerce( BindingExpression binding, _) 
        | SpecificCall <@ coerce @> (None, _, [ BindingExpression binding ]) 
        | Nullable( BindingExpression binding) -> 
            binding
        | StringFormat(format, BindingExpression(:? Binding as binding)) -> 
            binding.StringFormat <- format
            binding.ValidatesOnNotifyDataErrors <- false
            upcast binding

        //??? hard to say if can be generally useful. For erased types.
//        | Call((Some (Value (:? System.ComponentModel.ICustomTypeDescriptor as model, _))), get_Item, [ Value(:? string as propertyName, _)]) 
//            when get_Item.Name = "get_Item" && model.GetProperties().Find(propertyName, ignoreCase = false) <> null -> Some propertyName

        | Call(None, method', [ Value(:? IValueConverter as converter, _); BindingExpression(:? Binding as binding) ] ) when method'.Name = "IValueConverter.Apply" -> 
            binding.Converter <- converter
            upcast binding
        | Converter(convert, BindingExpression(:? Binding as binding)) -> 
            binding.Mode <- BindingMode.OneWay
            binding.ValidatesOnNotifyDataErrors <- false
            binding.Converter <- IValueConverter.OneWay convert
            upcast binding
        | SinglePropertyExpression binding -> 
            binding.ValidatesOnNotifyDataErrors <- false
            upcast binding
        | expr -> invalidArg "binding property path quotation" (string expr)

    let inline configureBinding(binding : #BindingBase, mode, updateSourceTrigger, fallbackValue, targetNullValue, validatesOnExceptions, validatesOnDataErrors) = 
        mode |> Option.iter (fun x -> (^a : (member set_Mode : BindingMode -> unit) (binding, x)))
        updateSourceTrigger |> Option.iter (fun x -> (^a : (member set_UpdateSourceTrigger : UpdateSourceTrigger -> unit) (binding, x)))
        fallbackValue |> Option.iter (fun x -> (^a : (member set_FallbackValue : obj -> unit) (binding, x)))
        targetNullValue |> Option.iter (fun x -> (^a : (member set_TargetNullValue : obj -> unit) (binding, x)))
        validatesOnExceptions |> Option.iter (fun x -> (^a : (member set_ValidatesOnExceptions : bool -> unit) (binding, x)))
        validatesOnDataErrors |> Option.iter (fun x -> (^a : (member set_ValidatesOnDataErrors : bool -> unit) (binding, x)))

    type Expr with
        member this.ToBindingExpr(?mode, ?updateSourceTrigger, ?fallbackValue, ?targetNullValue, ?validatesOnExceptions, ?validatesOnDataErrors) = 
            match this with
            | PropertySet(Target target, targetProperty, [], BindingExpression binding) ->

                match binding with
                | :? Binding as single -> 
                    configureBinding(single,  mode, updateSourceTrigger, fallbackValue, targetNullValue, validatesOnExceptions, validatesOnDataErrors)
                | :? MultiBinding as multi ->
                    configureBinding(multi,  mode, updateSourceTrigger, fallbackValue, targetNullValue, validatesOnExceptions, validatesOnDataErrors)
                | unexpected -> 
                    Debug.Fail(sprintf "Unexpected binding type %s" (unexpected.GetType().Name))

                target.SetBinding(targetProperty.DependencyProperty, binding)
            | _ -> invalidArg "expr" (string this) 

open Patterns

type Binding with
    static member FromExpression(expr, ?mode, ?updateSourceTrigger, ?fallbackValue, ?targetNullValue, ?validatesOnDataErrors, ?validatesOnExceptions) =
        let rec split = function 
            | Sequential(head, tail) -> head :: split tail
            | tail -> [ tail ]

        for e in split expr do
            let be = e.ToBindingExpr(?mode = mode, ?updateSourceTrigger = updateSourceTrigger, ?fallbackValue = fallbackValue, 
                                     ?targetNullValue = targetNullValue, ?validatesOnExceptions = validatesOnExceptions, ?validatesOnDataErrors = validatesOnDataErrors)
            assert not be.HasError
    
    static member UpdateSourceOnChange expr = Binding.FromExpression(expr, updateSourceTrigger = UpdateSourceTrigger.PropertyChanged)
    static member TwoWay expr = Binding.FromExpression(expr, BindingMode.TwoWay)
    static member OneWay expr = Binding.FromExpression(expr, BindingMode.OneWay)

open System.Windows.Controls
open System.Windows.Controls.Primitives

type Selector with
    member this.SetBindings(itemsSource : Expr<#seq<'Item>>, ?selectedItem : Expr<'Item>, ?displayMember : Expr<('Item -> _)>) = 

        let e = this.SetBinding(ItemsControl.ItemsSourceProperty, match itemsSource with BindingExpression binding -> binding)
        assert not e.HasError

        selectedItem |> Option.iter(fun(BindingExpression binding) -> 
            let e = this.SetBinding(DataGrid.SelectedItemProperty, binding)
            assert not e.HasError
            this.IsSynchronizedWithCurrentItem <- Nullable true
        )

        displayMember |> Option.iter(fun(PropertySelector(propertyName, _)) -> 
            this.DisplayMemberPath <- propertyName
        )
        
type DataGrid with
    member this.SetBindings(itemsSource : Expr<#seq<'Item>>, columnBindings : 'Item -> (#DataGridBoundColumn * Expr) list, ?selectedItem) = 

        this.SetBindings(itemsSource, ?selectedItem = selectedItem)

        for column, BindingExpression binding in columnBindings Unchecked.defaultof<'Item> do
            column.Binding <- binding
                
