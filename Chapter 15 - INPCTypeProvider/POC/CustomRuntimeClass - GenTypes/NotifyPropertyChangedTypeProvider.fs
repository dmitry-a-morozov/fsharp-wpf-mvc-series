namespace CustomRuntimeClass.INPCTypeProvider

open System
open System.Reflection
open System.IO
open System.Collections.Generic
open System.ComponentModel
open System.Windows
open System.Windows.Data

open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns
open Microsoft.FSharp.Quotations.ExprShape

open Samples.FSharp.ProvidedTypes

type PCEH = PropertyChangedEventHandler

[<TypeProvider>]
type public NotifyPropertyChangedTypeProvider(config : TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces()

    let nameSpace = this.GetType().Namespace
    let assembly = Assembly.GetExecutingAssembly()
    let tempAssembly = ProvidedAssembly(Path.ChangeExtension(Path.GetTempFileName(), ".dll"))
    let providerType = ProvidedTypeDefinition(assembly, nameSpace, "NotifyPropertyChanged", Some typeof<obj>, IsErased = false)

    let cache = Dictionary()

    do 
        tempAssembly.AddTypes <| [ providerType ]

        let parameters = [
            ProvidedStaticParameter("prototypesAssembly", typeof<string>)
        ]

        providerType.DefineStaticParameters(
            parameters, 
            instantiationFunction = (fun typeName args ->   
                let fileName = string args.[0]
                let key = typeName, fileName
                match cache.TryGetValue(key) with
                | false, _ ->
                    try
                        let v = this.GenerateTypes typeName fileName
                        cache.[key] <- Choice1Of2 v
                        v
                    with e ->
                        cache.[key] <- Choice2Of2 (System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture e)
                        reraise()
                | true, Choice1Of2 v -> v
                | true, Choice2Of2 e -> e.Throw(); failwith "unreachable"
            )
        )
        this.AddNamespace(nameSpace, [ providerType ])

    member internal __.GenerateTypes typeName fileName = 

        let prototypesAssembly = 
            match config.ReferencedAssemblies |> Array.tryFind (fun fullPath -> Path.GetFileNameWithoutExtension fullPath = fileName) with
            | Some assemblyFileName -> 
                assemblyFileName |> File.ReadAllBytes |> Assembly.Load
            | None -> 
                failwithf "Invalid prototype assembly name %s. Pick from the list of referenced assemblies." fileName
                
        let outerType = ProvidedTypeDefinition(assembly, nameSpace, typeName, Some typeof<obj>, IsErased = false)
        tempAssembly.AddTypes <| [ outerType ]

        outerType.AddMembersDelayed <| fun() ->
            [
                let proccesedTypes = Dictionary<Type, ProvidedTypeDefinition>()
                for prototype in prototypesAssembly.GetExportedTypes() do
                    if FSharpType.IsRecord prototype 
                    then 
                        let modelType = __.MapRecordToModelClass(prototype, proccesedTypes)
                        proccesedTypes.Add(prototype, modelType)
                        yield modelType
            ] 

        outerType

    member internal this.MapRecordToModelClass(prototype, processedTypes) = 

        let modelType = ProvidedTypeDefinition(prototype.Name, Some typeof<Model>, IsErased = false)
        tempAssembly.AddTypes [ modelType ]

        let mutableProperties = this.AddMutableProperties(prototype, modelType, processedTypes)
        this.AddDerivedProperties(prototype, modelType, mutableProperties)

        modelType

    member internal this.AddMutableProperties(prototype, modelType, processedTypes) = 
        let pceh = ProvidedField("propertyChangedEvent", typeof<PCEH>)
        modelType.AddMember pceh

        let proccesedProperties = Dictionary()
        
        modelType.AddMembers [
            for p in FSharpType.GetRecordFields prototype do

                let propertyType = 
                    let originalPropertyType = p.PropertyType
                    if originalPropertyType.IsGenericType 
                    then 
                        let genericTypeDef = originalPropertyType.GetGenericTypeDefinition()
                        let xs = 
                            originalPropertyType.GetGenericArguments()
                            |> Array.map (fun t -> 
                                match processedTypes.TryGetValue t with
                                | false, _ -> t
                                | true, t' -> upcast t'
                            )
                        ProvidedTypeBuilder.MakeGenericType(genericTypeDef, List.ofArray xs)
                    else
                        match processedTypes.TryGetValue(originalPropertyType) with
                        | false, _ -> originalPropertyType
                        | true, t -> upcast t
                        
                let backingField = ProvidedField(p.Name, propertyType)
                modelType.AddMember backingField

                let propName = p.Name
                let property = ProvidedProperty(p.Name, propertyType)
                property.GetterCode <- fun args -> Expr.FieldGet(args.[0], backingField)
                if p.CanWrite
                then 
                    property.SetterCode <- (fun args -> 
                        let this, value = args.[0], args.[1]
                        <@@
                            let newValue = %%Expr.Coerce(value, typeof<obj>)
                            let oldValue = %%Expr.Coerce(Expr.FieldGet(this, backingField), typeof<obj>)
                            if not(newValue.Equals(oldValue))
                            then
                                (%%Expr.FieldSet(this, backingField, value) : unit)
                                let notifyPropertyChanged = (%%Expr.FieldGet(this, pceh) : PCEH)
                                if notifyPropertyChanged <> null 
                                then
                                    notifyPropertyChanged.Invoke(%%Expr.Coerce(this, typeof<obj>), PropertyChangedEventArgs propName)   
                                    (%%Expr.Coerce(this, typeof<Model>) : Model).SetErrors(propName, [])                     
                        @@>)

                proccesedProperties.Add(p.Name, property)
                yield property                   
        ]

        modelType.AddInterfaceImplementation typeof<System.ComponentModel.INotifyPropertyChanged>
        let event = ProvidedEvent("PropertyChanged", typeof<PCEH>)
        event.AdderCode <- fun args ->
            let this, handler = args.[0], args.[1]
            Expr.FieldSet(this, pceh, <@@ Delegate.Combine((%%Expr.FieldGet(this, pceh) : PCEH), (%%handler : PCEH)) :?> PCEH @@>)
        event.RemoverCode <- fun args -> 
            let this, handler = args.[0], args.[1]
            Expr.FieldSet(this, pceh, <@@ Delegate.Remove((%%Expr.FieldGet(this, pceh) : PCEH), (%%handler : PCEH)) :?> PCEH @@>)

        modelType.AddMember event
                         
        let addMethod = event.AddMethod :?> ProvidedMethod
        addMethod.AddMethodAttrs MethodAttributes.Virtual

        let removeMethod = event.RemoveMethod :?> ProvidedMethod
        removeMethod.AddMethodAttrs MethodAttributes.Virtual

        modelType.DefineMethodOverride(addMethod, typeof<INotifyPropertyChanged>.GetMethod "add_PropertyChanged")
        modelType.DefineMethodOverride(removeMethod, typeof<INotifyPropertyChanged>.GetMethod "remove_PropertyChanged")

        proccesedProperties

    member internal this.AddDerivedProperties(prototype, modelType, processeProperties) = 

        let typeInitCode = ResizeArray()
        let ctorParams = ResizeArray()

        modelType.AddMembers [
            for p in prototype.GetProperties() do
                match p with
                | PropertyGetterWithReflectedDefinition (Lambda (model, Lambda(unitVar, propertyBody))) when not p.CanWrite ->
                    assert(unitVar.Type = typeof<unit>)

                    let propName, propType = p.Name, p.PropertyType                        
                    let derivedProperty = ProvidedProperty(propName, propType)
                    derivedProperty.GetterCode <- fun args ->
                        let replacement = args.[0]
                        this.RewriteTargetInstance(model, replacement, processeProperties, propertyBody)

                    let dp = ProvidedField(propName, typeof<DependencyProperty>)
                    dp.SetFieldAttributes(FieldAttributes.InitOnly ||| FieldAttributes.Static)
                    modelType.AddMember dp

                    typeInitCode.Add <| Expr.FieldSet(dp, <@@ DependencyProperty.Register(propName, propType, modelType) @@>)
                    let propertyDependecies = DerivedProperties.getPropertyDependencies model propertyBody |> Seq.toList
                    ctorParams.Add <| <@@ (%%Expr.FieldGet(dp) : DependencyProperty), propertyDependecies @@>

                    yield derivedProperty

                | _ -> ()
        ]

        if typeInitCode.Count > 0
        then
            let typeInit = ProvidedConstructor([], IsTypeInitializer = true)
            typeInit.InvokeCode <- fun _ ->
                typeInitCode |> Seq.reduce (fun x y -> Expr.Sequential(x, y))
            modelType.AddMember typeInit

        let ctor = ProvidedConstructor([], IsImplicitCtor = true)
        let baseCtor = typeof<Model>.GetConstructor(BindingFlags.NonPublic ||| BindingFlags.Instance, null, [| typeof<(DependencyProperty * string list)[]> |], null)
        ctor.BaseConstructorCall <- fun args -> baseCtor, [ Expr.NewArray(typeof<DependencyProperty * string list>, List.ofSeq ctorParams) ]
        modelType.AddMember ctor

    member internal this.RewriteTargetInstance(target, replacement, processeProperties, expr) = 
        let rec loop(target, replacement, expr) = 
            match expr with 
            | PropertyGet (Some(Var obj), prop, []) when obj = target -> 
                assert(processeProperties.ContainsKey prop.Name)
                Expr.PropertyGet(replacement, processeProperties.[prop.Name])
            | ShapeVar var -> Expr.Var(var)
            | ShapeLambda(var, body) -> Expr.Lambda(var, loop(target, replacement, body))  
            | ShapeCombination(shape, exprs) -> ExprShape.RebuildShapeCombination(shape, exprs |> List.map(fun e -> loop(target, replacement, e)))
        loop(target, replacement, expr)


[<assembly:TypeProviderAssembly>] 
do()