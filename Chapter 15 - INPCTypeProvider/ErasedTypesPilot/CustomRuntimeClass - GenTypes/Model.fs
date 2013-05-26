namespace CustomRuntimeClass.INPCTypeProvider

open System
open System.ComponentModel
open System.Collections.Generic

type Model() as this = 

    let propertyChangedEvent = Event<_, _>()

    let errors = Dictionary()
    let errorsChangedEvent = Event<_,_>()
    let getErrorsOrEmpty propertyName = match errors.TryGetValue propertyName with | true, errors -> errors | false, _ -> []
    let triggerErrorsChanged propertyName = errorsChangedEvent.Trigger(this, DataErrorsChangedEventArgs propertyName)

    [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
    member this.TriggerPropertyChanged propertyName = 
        propertyChangedEvent.Trigger(this, PropertyChangedEventArgs propertyName)

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChangedEvent.Publish

    interface INotifyDataErrorInfo with
        member this.HasErrors = this.HasErrors
        member this.GetErrors propertyName = upcast getErrorsOrEmpty propertyName
        [<CLIEvent>]
        member this.ErrorsChanged = errorsChangedEvent.Publish

    member this.AddErrors(propertyName, [<ParamArray>] messages) = 
        errors.[propertyName] <- getErrorsOrEmpty propertyName @ List.ofArray messages 
        triggerErrorsChanged propertyName
    member this.AddError(propertyName, message : string) = this.AddErrors(propertyName, message)
    member this.ClearErrors propertyName = 
        errors.Remove propertyName |> ignore
        triggerErrorsChanged propertyName
    member this.ClearAllErrors() = errors.Keys |> Seq.toArray |> Array.iter this.ClearErrors
    member this.HasErrors = errors.Values |> Seq.collect id |> Seq.exists (not << String.IsNullOrEmpty)



