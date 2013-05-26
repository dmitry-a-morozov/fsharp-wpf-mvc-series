module App

open CustomRuntimeClass.INPCTypeProvider
open SampleModelPrototypes

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open FSharp.Windows

let (?) (window : Window) name : 'T = name |> window.FindName|> unbox

type ViewModels = NotifyPropertyChanged<"SampleModelPrototypes">

[<STAThread>]
do
    //Create Window
    let window : Window = Application.LoadComponent(Uri("MainWindow.xaml", UriKind.Relative)) |> unbox
    let x : TextBox = window?X
    let y : TextBox = window?Y
    let operations : ComboBox = window?Operation
    let result : TextBlock = window?Result
    let calculate : Button = window?Calculate
    let clear : Button = window?Clear

    //Create models
    let model = ViewModels.Calculator()
    model.AvailableOperations <- typeof<Operations> |> Enum.GetValues |> unbox
    model.SelectedOperation <- Operations.Add

    //Data bindings
    Binding.FromExpression 
        <@ 
            x.Text <- string model.X 
            y.Text <- string model.Y
            result.Text <- string model.Result
            operations.ItemsSource <- model.AvailableOperations
            operations.SelectedItem <- model.SelectedOperation
        @>

    window.DataContext <- model

    //Event handlers
    calculate.Click.Add  <| fun _ ->
        match model.SelectedOperation with
        | Operations.Add -> model.Result <- model.X + model.Y
        | Operations.Subtract -> model.Result <- model.X - model.Y 
        | Operations.Multiply -> model.Result <- model.X * model.Y 
        | Operations.Divide ->
            if model.Y = 0 
            then
                model |> Validation.setError <@ fun m -> m.Y @> "Attempted to divide by zero."
            else
                model.Result <- model.X / model.Y 
        | _ -> ()


    clear.Click.Add <| fun _ ->
        model.X <- 0
        model.Y <- 0
        model.Result <- 0

   //Start
    window.ShowDialog() |> ignore