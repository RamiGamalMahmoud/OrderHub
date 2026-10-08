namespace OrderHub.UI.StartUpSteps.Steps;

public enum StartUpdStepsOrder
{
    ConnectionStep,
    InitializeApplicationContextStep,
    PrepareDirectories,
    DatabaseInitialization,
    Authentication,
    PrepareMassagingService,
    RunApiServer,
    WhatsAppStartup,
    CheckForUpdateStep,
}
