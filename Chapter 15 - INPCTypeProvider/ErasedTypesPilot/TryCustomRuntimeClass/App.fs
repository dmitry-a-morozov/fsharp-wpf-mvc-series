﻿module MainApp

open CustomRuntimeClass.INPCTypeProvider
open System
open System.Windows
open System.Windows.Controls
open SampleModelPrototypes

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
    model.SelectedOperation <- model.AvailableOperations.[0]

    //Data bindings

    //Uncomment following line to see why erased types do not work with Data Binding DSL
    //Binding.FromExpression <@ x.Text <- string model.X @>

    x.SetBinding(TextBox.TextProperty, "X") |> ignore
    y.SetBinding(TextBox.TextProperty, "Y") |> ignore
    result.SetBinding(TextBlock.TextProperty, "Result") |> ignore
    operations.SetBinding(ComboBox.ItemsSourceProperty, "AvailableOperations") |> ignore
    operations.SetBinding(ComboBox.SelectedItemProperty, "SelectedOperation") |> ignore

    window.DataContext <- model

    //Event handlers
    calculate.Click.Add  <| fun _ ->
        if model.SelectedOperation = Operations.Add 
        then 
            model.Result <- model.X + model.Y
        else if model.SelectedOperation = Operations.Subtract 
        then 
            model.Result <- model.X - model.Y 
        else if model.SelectedOperation = Operations.Multiply
        then 
            model.Result <- model.X * model.Y 
        else if model.SelectedOperation = Operations.Divide
        then 
            if model.Y = 0 
            then
                //model.AddError("Y", "Attempted to divide by zero.")
                model |> Validation.setError <@ fun m -> m.X @> "Attempted to divide by zero."
            else
                model.Result <- model.X / model.Y 


    clear.Click.Add <| fun _ ->
        model.X <- 0
        model.Y <- 0
        model.Result <- 0

    window.ShowDialog() |> ignore