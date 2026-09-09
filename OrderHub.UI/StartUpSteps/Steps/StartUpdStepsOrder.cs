namespace OrderHub.UI.StartUpSteps.Steps;

public enum StartUpdStepsOrder
{
    InitializeApplicationContextStep,
    WhatsAppStartup,
    PrepareDirectories,
    DatabaseInitialization,
    Authentication,
    PrepareMassagingService,
    RunApiServer,
}
