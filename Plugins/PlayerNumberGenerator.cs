using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace PlayerNumberPlugin
{
    public class PlayerNumberGenerator : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // =====================================================
            // TRACING SERVICE
            // =====================================================

            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(
                    typeof(ITracingService));

            tracingService.Trace(
                "=====================================================");

            tracingService.Trace(
                "PlayerNumberGenerator STARTED.");

            // =====================================================
            // PLUGIN CONTEXT
            // =====================================================

            IPluginExecutionContext context =
                (IPluginExecutionContext)serviceProvider.GetService(
                    typeof(IPluginExecutionContext));

            tracingService.Trace(
                "MessageName: {0}",
                context.MessageName);

            tracingService.Trace(
                "PrimaryEntityName: {0}",
                context.PrimaryEntityName);

            tracingService.Trace(
                "Stage: {0}",
                context.Stage);

            tracingService.Trace(
                "Mode: {0}",
                context.Mode);

            // =====================================================
            // ORGANIZATION SERVICE
            // =====================================================

            IOrganizationServiceFactory serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(
                    typeof(IOrganizationServiceFactory));

            IOrganizationService service =
                serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                tracingService.Trace(
                    "Entered TRY block.");

                // =====================================================
                // ONLY CREATE
                // =====================================================

                if (!context.MessageName.Equals(
                    "Create",
                    StringComparison.OrdinalIgnoreCase))
                {
                    tracingService.Trace(
                        "RETURN: Message is not Create.");

                    return;
                }

                tracingService.Trace(
                    "Message is Create.");

                // =====================================================
                // CHECK TARGET
                // =====================================================

                if (!context.InputParameters.Contains("Target") ||
                    !(context.InputParameters["Target"] is Entity))
                {
                    tracingService.Trace(
                        "RETURN: Target is missing or is not Entity.");

                    return;
                }

                Entity playerEntity =
                    (Entity)context.InputParameters["Target"];

                // =====================================================
                // MAKE SURE THIS IS sol_player
                // =====================================================

                if (playerEntity.LogicalName != "sol_player")
                {
                    tracingService.Trace(
                        "RETURN: Target is not sol_player.");

                    return;
                }

                tracingService.Trace(
                    "Target is sol_player.");

                // =====================================================
                // GET PLAYER NAME
                // =====================================================
                // YOUR FIELD:
                // sol_playernumber = Player Name

                string playerName =
                    playerEntity.GetAttributeValue<string>(
                        "sol_playernumber");

                tracingService.Trace(
                    "Player Name: {0}",
                    playerName ?? "NULL");

                if (string.IsNullOrWhiteSpace(playerName))
                {
                    throw new InvalidPluginExecutionException(
                        "Player Name is required to generate Player Number.");
                }

                // =====================================================
                // SPLIT NAME
                // =====================================================

                string[] nameParts =
                    playerName.Split(
                        new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries);

                if (nameParts.Length == 0)
                {
                    throw new InvalidPluginExecutionException(
                        "Player Name is invalid.");
                }

                // =====================================================
                // GET INITIALS
                // =====================================================

                string initials =
                    nameParts[0]
                        .Substring(0, 1)
                        .ToUpper();

                if (nameParts.Length > 1)
                {
                    initials +=
                        nameParts[nameParts.Length - 1]
                            .Substring(0, 1)
                            .ToUpper();
                }

                tracingService.Trace(
                    "Initials: {0}",
                    initials);

                // =====================================================
                // CURRENT YEAR
                // =====================================================

                int currentYear =
                    DateTime.UtcNow.Year;

                tracingService.Trace(
                    "Current Year: {0}",
                    currentYear);

                // =====================================================
                // COUNT PLAYERS CREATED THIS YEAR
                // =====================================================

                QueryExpression query =
                    new QueryExpression("sol_player");

                query.ColumnSet =
                    new ColumnSet(false);

                query.Criteria.AddCondition(
                    "createdon",
                    ConditionOperator.ThisYear);

                EntityCollection existingPlayers =
                    service.RetrieveMultiple(query);

                tracingService.Trace(
                    "Existing players this year: {0}",
                    existingPlayers.Entities.Count);

                // =====================================================
                // GENERATE INCREMENTAL NUMBER
                // =====================================================

                int incrementalNumber =
                    existingPlayers.Entities.Count + 1;

                tracingService.Trace(
                    "Incremental Number: {0}",
                    incrementalNumber);

                // =====================================================
                // GENERATE PLAYER NUMBER
                // =====================================================

                string newPlayerNumber =
                    $"P{incrementalNumber:D5}-{initials}|{currentYear}";

                tracingService.Trace(
                    "Generated Player Number: {0}",
                    newPlayerNumber);

                // =====================================================
                // SET PLAYER NUMBER
                // =====================================================
                // YOUR FIELD:
                // sol_playername = Player Number

                playerEntity["sol_playername"] =
                    newPlayerNumber;

                tracingService.Trace(
                    "Player Number successfully assigned.");

                // =====================================================
                // FINISHED
                // =====================================================

                tracingService.Trace(
                    "PlayerNumberGenerator FINISHED SUCCESSFULLY.");

                tracingService.Trace(
                    "=====================================================");
            }
            catch (Exception ex)
            {
                tracingService.Trace(
                    "=====================================================");

                tracingService.Trace(
                    "PlayerNumberGenerator EXCEPTION.");

                tracingService.Trace(
                    "Exception Message: {0}",
                    ex.Message);

                tracingService.Trace(
                    "Exception Details: {0}",
                    ex.ToString());

                tracingService.Trace(
                    "=====================================================");

                throw new InvalidPluginExecutionException(
                    "Error generating Player Number: " +
                    ex.Message,
                    ex);
            }
        }
    }
}