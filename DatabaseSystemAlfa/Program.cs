﻿using DatabaseSystemAlfa.Libraries.Configuration;
using DatabaseSystemAlfa.Libraries.Tools.Console.Message;
using DatabaseSystemAlfa.Libraries.Tools.Console.Message.Extensions;
using DatabaseSystemAlfa.Libraries.Tools.Console.Prompt;
using DatabaseSystemAlfa.Services;
using DatabaseSystemAlfa.Services.Operations;
using DatabaseSystemAlfa.Services.Operations.Global;
using DatabaseSystemAlfa.Services.Operations.Menu;
using Spectre.Console;

namespace DatabaseSystemAlfa;

public abstract class Program
{
    private static AppSettings? _appSettings;
    private const string ConfigFileName = "app-settings.json";

    public static void Main()
    {
        SetupEventHandlers();
        Configurator.ConfigFileName = ConfigFileName;

        try
        {
            _appSettings = new AppSettings(Configurator.InitBuilder().Build());
            MessageTemplate.Info("Configuration file was loaded successfully").Display();
        }
        catch (Exception e)
        {
            MessageTemplate.Error(e.Message).Display();

            _appSettings = new AppSettings();
            Configurator.SerializeToJson(_appSettings, true);
            MessageTemplate.Warning("Config file need to be setup manually").Display();

            MessageTemplate.Italic("The configuration template was auto-generated in the executable file's root folder")
                .PrependNewLine().Display();
        }

        var menuOperations = new Dictionary<string, IOperation>
        {
            { "Connect to database", new ConnectToDatabaseOperation(_appSettings, AnsiConsole.Status(), 3) },
            {
                "Setup configuration", new SetupConfigurationOperation(_appSettings, new OperationEvents(
                    PromptTemplate<string>.HighlightAsk,
                    PromptTemplate<string>.OptionalAndSecret,
                    PromptTemplate<int>.HighlightAsk
                ))
            },
            { "Save configuration", new SaveConfigurationOperation(_appSettings) },
            { "Exit", new ExitOperation() }
        };

        HandleOperations("Select menu operation", menuOperations, instance => !instance.ConnectionIsOpen());
    }

    private static void SetupEventHandlers()
    {
        MessageBase.OnDisplayRequestedEvent += (_, msg) => AnsiConsole.MarkupLine(msg);
        PromptBase<string>.OnAskRequestedEvent += AnsiConsole.Ask<string>;
        PromptBase<int>.OnAskRequestedEvent += AnsiConsole.Ask<int>;
        PromptBase<string>.OnPromptRequestedEvent += AnsiConsole.Prompt;
    }

    private static void HandleOperations(string description, Dictionary<string, IOperation> operations,
        Predicate<DatabaseSingleton> condition)
    {
        do
        {
            var selectedOperation = PromptTemplate<string>.Selection(
                MessageTemplate.Regular($"{description}:").PrependNewLine().ToString(),
                operations.Keys);

            if (!operations.TryGetValue(selectedOperation, out var operation)) continue;
            AnsiConsole.Clear();

            var result = operation.Execute();
            result.Message.Display();
            result.AdditionalMsg?.Display();

            if (operation is ExitOperation) break;
        } while (condition(DatabaseSingleton.Instance));
    }
}