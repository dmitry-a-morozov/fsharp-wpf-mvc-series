namespace CustomRuntimeClass.INPCTypeProvider

open System
open System.ComponentModel
open System.Collections.Generic
open System.Windows

type Model() as this = 
    inherit DependencyObject()

    let errors = Dictionary()
    let errorsChangedEvent = Event<_,_>()
    let triggerErrorsChanged propertyName = errorsChangedEvent.Trigger(this, DataErrorsChangedEventArgs propertyName)

    interface INotifyDataErrorInfo with
        member this.HasErrors = 
            errors.Values |> Seq.collect id |> Seq.exists (not << String.IsNullOrEmpty)
        member this.GetErrors propertyName = 
            upcast (match errors.TryGetValue propertyName with | true, errors -> errors | false, _ -> Seq.empty)
        [<CLIEvent>]
        member this.ErrorsChanged = errorsChangedEvent.Publish

    member this.SetErrors(propertyName, messages) = 
        errors.[propertyName] <- messages
        triggerErrorsChanged propertyName



