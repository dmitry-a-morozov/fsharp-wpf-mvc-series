
open System
open System.Windows
open System.Windows.Controls
open System.ComponentModel

type Model() =
    let mutable text = ""
    let propertyChangedEvent = Event<_,_>()

    member this.Text 
        with get() = text 
        and set value = 
            text <- value
            propertyChangedEvent.Trigger(this, PropertyChangedEventArgs "Text")

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChangedEvent.Publish

[<STAThread>] 
do
    let model = Model()
    let textBox = TextBox(DataContext = model)
    textBox.SetBinding(TextBox.TextProperty, "Text") |> ignore
    textBox.TextChanged |> Observable.add(fun _ ->
        printfn "Begin event handler. TextBox.Text value: %s. Reverting ..." model.Text
        model.Text <- String(model.Text.ToCharArray() |> Array.rev)
        printfn "End event handler."
    )
    model.Text <- "Hello"
    stdin.ReadLine() |> ignore