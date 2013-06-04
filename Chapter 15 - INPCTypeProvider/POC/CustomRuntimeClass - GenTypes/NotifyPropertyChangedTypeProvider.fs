namespace CustomRuntimeClass.INPCTypeProvider

open System
open System.Reflection
open System.IO
open System.Collections.Generic

open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations

open Samples.FSharp.ProvidedTypes

type PCEH = System.ComponentModel.PropertyChangedEventHandler

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

    static member internal SetValue<'T when 'T : equality>(self, backingField, name, value) =
        <@@
            let oldValue = %%Expr.FieldGet(self, backingField) : 'T
            if (%%value : 'T) <> oldValue
            then
                (%%Expr.FieldSet(self, backingField, value) : unit)
        @@>

    member internal __.MapRecordToModelClass(prototype : Type, processedTypes) = 

        let modelType = ProvidedTypeDefinition(prototype.Name, Some typeof<obj>, IsErased = false)
        tempAssembly.AddTypes <| [ modelType ]

        let prototypeName = prototype.AssemblyQualifiedName
        modelType.AddMember <| ProvidedConstructor([], IsImplicitCtor = true)

        let handler = ProvidedField("handler", typeof<PCEH>)
        modelType.AddMember handler

        modelType.AddMembersDelayed <| fun () -> 
            [
                for p in prototype.GetProperties() do

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

                    property.SetterCode <- fun args -> 
                        <@@
                            let newValue = %%Expr.Coerce(args.[1], typeof<obj>)
                            let oldValue = %%Expr.Coerce(Expr.FieldGet(args.[0], backingField), typeof<obj>)
                            if not(newValue.Equals(oldValue))
                            then
                                (%%Expr.FieldSet(args.[0], backingField, args.[1]) : unit)
                                let h = (%%Expr.FieldGet(args.[0], handler) : PCEH)
                                if h <> null 
                                then
                                    h.Invoke(null, ComponentModel.PropertyChangedEventArgs(propName))                        
                        @@>

                    yield property                   
            ]

        modelType.AddInterfaceImplementation typeof<System.ComponentModel.INotifyPropertyChanged>
        let evt = ProvidedEvent("PropertyChanged", typeof<System.ComponentModel.PropertyChangedEventHandler>)
        modelType.AddMember evt
        evt.AdderCode <- 
            fun [this; value] ->
                Expr.FieldSet(this, handler, <@@ System.Delegate.Combine((%%Expr.FieldGet(this, handler) : PCEH), (%%value : PCEH)) :?> PCEH @@>)
        evt.RemoverCode <- 
            fun [this; value] -> 
                Expr.FieldSet(this, handler, <@@ System.Delegate.Remove((%%Expr.FieldGet(this, handler) : PCEH), (%%value : PCEH)) :?> PCEH @@>)
        let addDecl = typeof<System.ComponentModel.INotifyPropertyChanged>.GetMethod "add_PropertyChanged"
        let removeDecl = typeof<System.ComponentModel.INotifyPropertyChanged>.GetMethod "remove_PropertyChanged"
                         
        let addMethod = evt.AddMethod :?> ProvidedMethod
        addMethod.SetMethodAttrs (addMethod.Attributes ||| MethodAttributes.Virtual)

        let removeMethod = evt.RemoveMethod :?> ProvidedMethod
        removeMethod.SetMethodAttrs (removeMethod.Attributes ||| MethodAttributes.Virtual)

        modelType.DefineMethodOverride(addMethod, addDecl)
        modelType.DefineMethodOverride(removeMethod, removeDecl)

        modelType

[<assembly:TypeProviderAssembly>] 
do()