namespace OrderHub.UI.StartUpSteps.Steps;

public enum StartUpdStepsOrder
{
    InitializeApplicationContextStep,
    PrepareDirectories,
    DatabaseInitialization,
    Authentication,
    PrepareMassagingService,
    RunApiServer,
    WhatsAppStartup,
    CheckForUpdateStep,
}
