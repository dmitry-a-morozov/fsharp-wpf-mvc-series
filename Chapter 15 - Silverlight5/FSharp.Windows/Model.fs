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

    static let proxyFactory = ProxyGenerator()
    let errors = Dictionary()
    let propertyChangedEvent = Event<_,_>()
    let errorsChanged = Event<_,_>()

    let getErrorsOrEmpty propertyName = match errors.TryGetValue propertyName with | true, errors -> errors | false, _ -> []

    static let notifyPropertyChanged = {
        new StandardInterceptor() with
            member this.PostProceed invocation = 
                match invocation.Method, invocation.InvocationTarget with 
                    | PropertySetter propertyName, (:? Model as model) -> 
                        model.TriggerPropertyChanged propertyName
                        model.ClearErrors propertyName 
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
        member this.HasErrors = this.HasErrors
        member this.GetErrors propertyName = upcast getErrorsOrEmpty propertyName
        [<CLIEvent>]
        member this.ErrorsChanged = errorsChanged.Publish

    member internal this.TriggerErrorsChanged propertyName = 
        errorsChanged.Trigger(this, DataErrorsChangedEventArgs propertyName)

    member this.AddErrors(propertyName, [<ParamArray>] messages) = 
        errors.[propertyName] <- getErrorsOrEmpty propertyName @ List.ofArray messages 
        this.TriggerErrorsChanged propertyName
    member this.AddError(propertyName, message : string) = this.AddErrors(propertyName, message)
    member this.ClearErrors propertyName = 
        errors.Remove propertyName |> ignore
        this.TriggerErrorsChanged propertyName
    member this.ClearAllErrors() = errors.Keys |> Seq.toArray |> Array.iter this.ClearErrors
    member this.HasErrors = errors.Values |> Seq.collect id |> Seq.exists (not << String.IsNullOrEmpty)

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

