namespace global

open System
open System.ComponentModel
open System.Collections.Generic

[<AbstractClass>]
type Model() as this = 
    
    let propertyChanged = Event<_, _>()

    let errors = Dictionary()
    let errorsChanged = Event<_,_>()
    let triggerErrorsChanged propertyName = errorsChanged.Trigger(this, DataErrorsChangedEventArgs propertyName)
    let getErrorsOrEmpty propertyName = match errors.TryGetValue propertyName with | true, errors -> errors | false, _ -> []

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChanged.Publish
    member this.NotifyPropertyChanged propertyName = 
        propertyChanged.Trigger(this,PropertyChangedEventArgs propertyName)
        this.SetErrors(propertyName, List.empty)

    interface INotifyDataErrorInfo with
        member this.HasErrors = 
            errors.Values |> Seq.collect id |> Seq.exists (not << String.IsNullOrEmpty)
        member this.GetErrors propertyName = 
            if String.IsNullOrEmpty propertyName 
            then upcast errors.Values 
            else upcast getErrorsOrEmpty propertyName
        [<CLIEvent>]
        member this.ErrorsChanged = errorsChanged.Publish

    member this.SetErrors(propertyName, messages) = 
        errors.[propertyName] <- messages
        triggerErrorsChanged propertyName

    