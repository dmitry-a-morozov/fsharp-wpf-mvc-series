namespace FSharp.Windows

open System
open System.ComponentModel
open System.Collections.Generic
open System.Reflection
open System.Linq
open System.Windows
open System.Windows.Data
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns 
open Microsoft.FSharp.Quotations.DerivedPatterns
open Microsoft.FSharp.Quotations.ExprShape

open Castle.DynamicProxy

open Binding.Patterns

module ModelExtensions = 

    let (|PropertySetter|_|) (m : MethodInfo) =
        match m.Name.Split('_') with
        | [| "set"; propertyName |] -> assert m.IsSpecialName; Some propertyName
        | _ -> None

    let (|PropertyGetter|_|) (m : MethodInfo) =
        match m.Name.Split('_') with
        | [| "get"; propertyName |] -> assert m.IsSpecialName; Some propertyName
        | _ -> None

    let (|Abstract|_|) (m : MethodInfo) = if m.IsAbstract then Some() else None

open ModelExtensions

[<AbstractClass>]
type Model() = 

    let propertyChangedEvent = Event<_,_>()

    let errors = Dictionary()
    let errorsChanged = Event<_,_>()

    static let proxyFactory = ProxyGenerator()

    static let notifyPropertyChanged = {
        new StandardInterceptor() with
            member this.PostProceed invocation = 
                match invocation.Method, invocation.InvocationTarget with 
                    | PropertySetter propertyName, (:? Model as model) -> 
                        model.TriggerPropertyChanged propertyName
                        model.SetErrors(propertyName, Array.empty)
                    | _ -> ()
    }

    static member Create<'T when 'T :> Model and 'T : not struct>()  : 'T = 
        let interceptors : IInterceptor[] = [| notifyPropertyChanged; AbstractProperties() |]
        proxyFactory.CreateClassProxy interceptors    

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChangedEvent.Publish

    member internal this.TriggerPropertyChanged propertyName = 
        propertyChangedEvent.Trigger(this, PropertyChangedEventArgs propertyName)

    interface INotifyDataErrorInfo with
        member this.HasErrors = 
            errors.Values |> Seq.collect id |> Seq.exists (not << String.IsNullOrEmpty)
        member this.GetErrors propertyName = 
            if String.IsNullOrEmpty propertyName 
            then upcast errors.Values 
            else upcast (match errors.TryGetValue propertyName with | true, errors -> errors | false, _ -> Array.empty)
        [<CLIEvent>]
        member this.ErrorsChanged = errorsChanged.Publish

    member this.SetErrors(propertyName, [<ParamArray>] messages: string[]) = 
        errors.[propertyName] <- messages
        errorsChanged.Trigger(this, DataErrorsChangedEventArgs propertyName)

and AbstractProperties() =
    let data = Dictionary()

    interface IInterceptor with
        member this.Intercept invocation = 
            match invocation.Method with 
                | Abstract & PropertySetter propertyName -> 
                    data.[propertyName] <- invocation.Arguments.[0]

                | Abstract & PropertyGetter propertyName ->
                    match data.TryGetValue propertyName with 
                    | true, value -> invocation.ReturnValue <- value 
                    | false, _ -> 
                        let returnType = invocation.Method.ReturnType
                        if returnType.IsValueType then 
                            invocation.ReturnValue <- Activator.CreateInstance returnType

                | _ -> invocation.Proceed()

[<RequireQualifiedAccess>]
module Mvc = 

    let inline startDialog(view, controller) = 
        let model = (^Model : (static member Create : unit -> ^Model ) ())
        if Mvc<'Events, ^Model>(model, view, controller).StartDialog() then Some model else None

    let inline startWindow(view, controller) = 
        async {
            let model = (^Model : (static member Create : unit -> ^Model) ())
            let! isOk = Mvc<'Events, ^Model>(model, view, controller).StartWindow()
            return if isOk then Some model else None
        }
