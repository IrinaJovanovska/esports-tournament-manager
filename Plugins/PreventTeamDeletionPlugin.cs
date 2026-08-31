using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace TournamentPlugin.Plugins
{
    public class PreventTeamDeletionPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracing =
                (ITracingService)serviceProvider.GetService(
                    typeof(ITracingService));

            IPluginExecutionContext context =
                (IPluginExecutionContext)serviceProvider.GetService(
                    typeof(IPluginExecutionContext));

            tracing.Trace(
                "========== PreventTeamDeletionPlugin STARTED ==========");

            try
            {
                if (context.MessageName != "Delete")
                    return;

                if (!context.InputParameters.Contains("Target"))
                    return;

                EntityReference targetRef =
                    context.InputParameters["Target"] as EntityReference;

                if (targetRef == null)
                    return;

                if (targetRef.LogicalName != "sol_team")
                    return;

                tracing.Trace("Target Team ID: " + targetRef.Id);

                IOrganizationServiceFactory serviceFactory =
                    (IOrganizationServiceFactory)
                    serviceProvider.GetService(
                        typeof(IOrganizationServiceFactory));

                IOrganizationService service =
                    serviceFactory.CreateOrganizationService(
                        context.UserId);

                QueryExpression playerQuery =
                    new QueryExpression("sol_player");

                playerQuery.ColumnSet = new ColumnSet(false);

                playerQuery.Criteria.AddCondition(
                    "sol_team",
                    ConditionOperator.Equal,
                    targetRef.Id);

                playerQuery.TopCount = 1;

                EntityCollection players =
                    service.RetrieveMultiple(playerQuery);

                tracing.Trace(
                    "Players found: " + players.Entities.Count);

                if (players.Entities.Count > 0)
                {
                    tracing.Trace(
                        "PLAYER FOUND. BLOCKING TEAM DELETE.");

                    throw new InvalidPluginExecutionException(
                        "Cannot delete this Team because it has registered players. " +
                        "Please remove all players from this team before deleting."
                    );
                }

                tracing.Trace(
                    "No Players found. Team deletion allowed.");
            }
            catch (InvalidPluginExecutionException)
            {
                tracing.Trace(
                    "DELETE BLOCKED.");

                throw;
            }
            catch (Exception ex)
            {
                tracing.Trace(
                    "Unexpected error: " + ex.ToString());

                throw new InvalidPluginExecutionException(
                    "An unexpected error occurred: " + ex.Message,
                    ex);
            }
        }
    }
}